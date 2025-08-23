using GQLServer.Data;

namespace GQLServer.Types.Outputs;

/// <summary>
/// Standard authentication response payload
/// </summary>
public class AuthPayload
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The authenticated user (null if failed)
    /// </summary>
    public User? User { get; set; }
    
    /// <summary>
    /// JWT token for authentication (only for sign in)
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// Verification token (only for sign up, dev mode only)
    /// </summary>
    public string? VerificationToken { get; set; }
}