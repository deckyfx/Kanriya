namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for updating an outlet's information
/// </summary>
public class UpdateOutletInput
{
    /// <summary>
    /// ID of the outlet to update (required)
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Updated code for the outlet (optional)
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Updated display name of the outlet (optional)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Updated physical address of the outlet (optional)
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Whether the outlet is active (optional)
    /// </summary>
    public bool? IsActive { get; set; }
}