namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Response output for updating a user's outlet access list
/// </summary>
public class UpdateUserOutletsOutput
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
    /// Number of outlets the user now has access to
    /// </summary>
    public int OutletCount { get; set; }
}