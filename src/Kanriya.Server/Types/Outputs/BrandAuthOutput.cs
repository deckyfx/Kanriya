using Kanriya.Server.Data;

namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Output for brand authentication response
/// </summary>
public class BrandAuthOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    
    /// <summary>
    /// JWT token with brand context
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// Token type (always "BRAND" for this auth)
    /// </summary>
    public string? TokenType { get; set; } = "BRAND";
    
    /// <summary>
    /// Brand information
    /// </summary>
    public Brand? Brand { get; set; }
    
    /// <summary>
    /// User's roles in the brand
    /// </summary>
    public string[]? Roles { get; set; }
    
    /// <summary>
    /// API Secret for future reference (returned only on first creation)
    /// </summary>
    public string? ApiSecret { get; set; }
    
    /// <summary>
    /// API Password (returned only on first creation, never stored in plain text)
    /// </summary>
    public string? ApiPassword { get; set; }
}