namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Response output for revoking outlet access
/// </summary>
public class RevokeOutletAccessOutput
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message providing details about the operation result
    /// </summary>
    public string? Message { get; set; }
}