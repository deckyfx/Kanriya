using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kanriya.Server.Blazor.Services;

/// <summary>
/// Simplified authentication state provider that reads JWT from localStorage
/// </summary>
public class SimpleAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SimpleAuthStateProvider> _logger;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    
    public SimpleAuthStateProvider(IJSRuntime js, ILogger<SimpleAuthStateProvider> logger)
    {
        _js = js;
        _logger = logger;
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Try to get token from localStorage
            string? token = null;
            try
            {
                token = await _js.InvokeAsync<string?>("localStorage.getItem", "authToken");
            }
            catch (InvalidOperationException)
            {
                // JS interop not available during pre-rendering
                return new AuthenticationState(_currentUser);
            }
            
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            
            // Validate and parse the token
            var user = ValidateToken(token);
            if (user != null)
            {
                _currentUser = user;
            }
            
            return new AuthenticationState(_currentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auth state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
    
    public async Task MarkUserAsAuthenticated(string token)
    {
        try
        {
            // Store token in localStorage
            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", token);
            
            // Validate and set current user
            var user = ValidateToken(token);
            if (user != null)
            {
                _currentUser = user;
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as authenticated");
        }
    }
    
    public async Task MarkUserAsLoggedOut()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }
    
    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Check expiry
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return null;
            }
            
            // Create ClaimsPrincipal from token claims
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}