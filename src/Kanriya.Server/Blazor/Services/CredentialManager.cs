using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Kanriya.Server.Blazor.Services
{
    public class StoredCredential
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = ""; // "principal" or "brand"
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = ""; // For principal
        public string BrandName { get; set; } = ""; // For brand
        public string BrandId { get; set; } = ""; // For brand
        public string Token { get; set; } = "";
        public string? ApiKey { get; set; } // For brand
        public string? ApiPassword { get; set; } // For brand
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; } // Currently active credential
    }

    public interface ICredentialManager
    {
        Task<List<StoredCredential>> GetAllCredentialsAsync();
        Task<StoredCredential?> GetActiveCredentialAsync();
        Task AddCredentialAsync(StoredCredential credential);
        Task RemoveCredentialAsync(string credentialId);
        Task SwitchCredentialAsync(string credentialId);
        Task ClearAllCredentialsAsync();
        Task UpdateCredentialAsync(StoredCredential credential);
        Task<bool> HasStoredCredentialsAsync();
        event Action? OnCredentialChanged;
    }

    public class CredentialManager : ICredentialManager
    {
        private readonly IJSRuntime _jsRuntime;
        private const string STORAGE_KEY = "kanriya_credentials";
        private const string ACTIVE_KEY = "kanriya_active_credential";
        private List<StoredCredential>? _cachedCredentials;
        
        public event Action? OnCredentialChanged;

        public CredentialManager(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<StoredCredential>> GetAllCredentialsAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", STORAGE_KEY);
                if (string.IsNullOrEmpty(json))
                {
                    _cachedCredentials = new List<StoredCredential>();
                    return _cachedCredentials;
                }

                _cachedCredentials = JsonSerializer.Deserialize<List<StoredCredential>>(json) ?? new List<StoredCredential>();
                
                // Ensure active credential is marked
                var activeId = await GetActiveCredentialIdAsync();
                foreach (var cred in _cachedCredentials)
                {
                    cred.IsActive = cred.Id == activeId;
                }
                
                return _cachedCredentials;
            }
            catch
            {
                _cachedCredentials = new List<StoredCredential>();
                return _cachedCredentials;
            }
        }

        public async Task<StoredCredential?> GetActiveCredentialAsync()
        {
            var credentials = await GetAllCredentialsAsync();
            var activeId = await GetActiveCredentialIdAsync();
            
            if (string.IsNullOrEmpty(activeId))
                return credentials.FirstOrDefault();
                
            return credentials.FirstOrDefault(c => c.Id == activeId);
        }

        private async Task<string?> GetActiveCredentialIdAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ACTIVE_KEY);
            }
            catch
            {
                return null;
            }
        }

        public async Task AddCredentialAsync(StoredCredential credential)
        {
            var credentials = await GetAllCredentialsAsync();
            
            // Check if credential already exists
            // For principal: check by token (if not empty)
            // For brand: check by BrandId + ApiKey combination
            StoredCredential? existing = null;
            
            if (credential.Type == "principal" && !string.IsNullOrEmpty(credential.Token))
            {
                existing = credentials.FirstOrDefault(c => c.Type == "principal" && c.Token == credential.Token);
            }
            else if (credential.Type == "brand" && !string.IsNullOrEmpty(credential.BrandId) && !string.IsNullOrEmpty(credential.ApiKey))
            {
                existing = credentials.FirstOrDefault(c => 
                    c.Type == "brand" && 
                    c.BrandId == credential.BrandId && 
                    c.ApiKey == credential.ApiKey);
            }
            
            if (existing != null)
            {
                // Update existing credential
                existing.DisplayName = credential.DisplayName;
                existing.LastUsedAt = DateTime.UtcNow;
                existing.ApiPassword = credential.ApiPassword; // Update password if changed
            }
            else
            {
                // Add new credential
                credentials.Add(credential);
            }
            
            await SaveCredentialsAsync(credentials);
            
            // If this is the only credential or explicitly set as active
            if (credentials.Count == 1 || credential.IsActive)
            {
                await SwitchCredentialAsync(credential.Id);
            }
            
            OnCredentialChanged?.Invoke();
        }

        public async Task RemoveCredentialAsync(string credentialId)
        {
            var credentials = await GetAllCredentialsAsync();
            var toRemove = credentials.FirstOrDefault(c => c.Id == credentialId);
            
            if (toRemove != null)
            {
                credentials.Remove(toRemove);
                await SaveCredentialsAsync(credentials);
                
                // If removed credential was active, switch to first available
                if (toRemove.IsActive && credentials.Any())
                {
                    await SwitchCredentialAsync(credentials.First().Id);
                }
                else if (!credentials.Any())
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ACTIVE_KEY);
                }
                
                OnCredentialChanged?.Invoke();
            }
        }

        public async Task SwitchCredentialAsync(string credentialId)
        {
            var credentials = await GetAllCredentialsAsync();
            StoredCredential? activeCredential = null;
            
            foreach (var cred in credentials)
            {
                cred.IsActive = cred.Id == credentialId;
                if (cred.IsActive)
                {
                    cred.LastUsedAt = DateTime.UtcNow;
                    activeCredential = cred;
                }
            }
            
            await SaveCredentialsAsync(credentials);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ACTIVE_KEY, credentialId);
            
            // IMPORTANT: Update the auth token with the new credential's token
            if (activeCredential != null && !string.IsNullOrEmpty(activeCredential.Token))
            {
                // Update the authentication token used by SimpleAuthStateProvider
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", activeCredential.Token);
            }
            
            OnCredentialChanged?.Invoke();
        }

        public async Task ClearAllCredentialsAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ACTIVE_KEY);
            _cachedCredentials = null;
            OnCredentialChanged?.Invoke();
        }

        public async Task UpdateCredentialAsync(StoredCredential credential)
        {
            var credentials = await GetAllCredentialsAsync();
            var existing = credentials.FirstOrDefault(c => c.Id == credential.Id);
            
            if (existing != null)
            {
                var index = credentials.IndexOf(existing);
                credentials[index] = credential;
                await SaveCredentialsAsync(credentials);
                OnCredentialChanged?.Invoke();
            }
        }

        public async Task<bool> HasStoredCredentialsAsync()
        {
            var credentials = await GetAllCredentialsAsync();
            return credentials.Any();
        }

        private async Task SaveCredentialsAsync(List<StoredCredential> credentials)
        {
            var json = JsonSerializer.Serialize(credentials);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
            _cachedCredentials = credentials;
        }
    }
}