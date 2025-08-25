using System.Security.Cryptography;
using System.Text;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Service for generating API credentials for brand users
/// </summary>
public interface IApiCredentialService
{
    string GenerateApiSecret();
    string GenerateApiPassword();
    string HashApiPassword(string apiPassword);
    bool VerifyApiPassword(string apiPassword, string hashedPassword);
}

public class ApiCredentialService : IApiCredentialService
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string PasswordChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
    
    /// <summary>
    /// Generate a 16-character API secret (used as username)
    /// </summary>
    public string GenerateApiSecret()
    {
        return GenerateRandomString(16, AllowedChars);
    }
    
    /// <summary>
    /// Generate a 32-character API password
    /// </summary>
    public string GenerateApiPassword()
    {
        return GenerateRandomString(32, PasswordChars);
    }
    
    /// <summary>
    /// Hash the API password using BCrypt
    /// </summary>
    public string HashApiPassword(string apiPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(apiPassword);
    }
    
    /// <summary>
    /// Verify an API password against its hash
    /// </summary>
    public bool VerifyApiPassword(string apiPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(apiPassword, hashedPassword);
    }
    
    private string GenerateRandomString(int length, string allowedChars)
    {
        var result = new StringBuilder(length);
        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length * 4]; // Get extra bytes to avoid bias
            rng.GetBytes(buffer);
            
            for (int i = 0; i < length; i++)
            {
                // Use multiple bytes to reduce bias
                var randomValue = BitConverter.ToUInt32(buffer, i * 4);
                result.Append(allowedChars[(int)(randomValue % allowedChars.Length)]);
            }
        }
        return result.ToString();
    }
}