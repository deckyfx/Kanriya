namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for granting a user access to an outlet
/// </summary>
public class GrantOutletAccessInput
{
    /// <summary>
    /// ID of the user to grant access to
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// ID of the outlet to grant access to
    /// </summary>
    public required string OutletId { get; set; }
}