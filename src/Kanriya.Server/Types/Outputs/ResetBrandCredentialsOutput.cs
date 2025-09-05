namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Output for reset brand credentials mutation
/// </summary>
public class ResetBrandCredentialsOutput
{
    /// <summary>
    /// Whether the reset was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// New API key (only shown once)
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// New API password (only shown once)
    /// </summary>
    public string? ApiPassword { get; set; }
}