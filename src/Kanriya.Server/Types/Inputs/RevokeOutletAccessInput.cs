namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for revoking outlet access from a user
/// </summary>
public class RevokeOutletAccessInput
{
    /// <summary>
    /// ID of the user to revoke access from (required)
    /// </summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>
    /// ID of the outlet to revoke access to (required)
    /// </summary>
    public string OutletId { get; set; } = null!;
}