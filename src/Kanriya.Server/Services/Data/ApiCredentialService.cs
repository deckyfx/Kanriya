using Kanriya.Shared.Utils;

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
    /// <summary>
    /// Generate a 16-character API secret (used as username)
    /// </summary>
    public string GenerateApiSecret()
    {
        return StringUtils.GenerateApiKey();
    }
    
    /// <summary>
    /// Generate a 32-character API password
    /// </summary>
    public string GenerateApiPassword()
    {
        return StringUtils.GenerateSecurePassword(32);
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
}