namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for creating a new outlet
/// </summary>
public class CreateOutletInput
{
    /// <summary>
    /// Unique code for the outlet within the brand (e.g., "OUTLET001", "MALL-A")
    /// </summary>
    public required string Code { get; set; }
    
    /// <summary>
    /// Display name of the outlet (e.g., "Geprek Bensu - Mall A")
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Physical address of the outlet (optional)
    /// </summary>
    public string? Address { get; set; }
}