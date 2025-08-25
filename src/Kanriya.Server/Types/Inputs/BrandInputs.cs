namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for creating a new brand
/// </summary>
public class CreateBrandInput
{
    [GraphQLDescription("Display name of the brand")]
    public required string Name { get; set; }
}

// Update brand removed - not part of simplified architecture
// Add user to brand removed - not part of simplified architecture
// Remove user from brand removed - not part of simplified architecture