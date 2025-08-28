namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for deleting an outlet
/// </summary>
public class DeleteOutletInput
{
    /// <summary>
    /// ID of the outlet to delete (required)
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Optional reason for deletion (for audit logs)
    /// </summary>
    public string? Reason { get; set; }
}