using Kanriya.Server.Data;

namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Payload for UpdateBrandName mutation
/// </summary>
public class UpdateBrandNamePayload
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
    /// The updated brand (if successful)
    /// </summary>
    public Brand? Brand { get; set; }
}