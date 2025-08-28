namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Response output for brand info update operations
/// </summary>
public class UpdateBrandInfoOutput
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message providing details about the operation result
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// The updated key
    /// </summary>
    public string? Key { get; set; }
    
    /// <summary>
    /// The updated value
    /// </summary>
    public string? Value { get; set; }
}