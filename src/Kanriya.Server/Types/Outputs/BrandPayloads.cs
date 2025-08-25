using Kanriya.Server.Data;

namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Payload for create brand mutation
/// </summary>
public class CreateBrandPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Brand? Brand { get; set; }
    public string? ApiSecret { get; set; }
    public string? ApiPassword { get; set; }
}

/// <summary>
/// Payload for delete brand mutation
/// </summary>
public class DeleteBrandPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// Update brand removed - not part of simplified architecture
// Brand status change removed - not part of simplified architecture
// Add user to brand removed - not part of simplified architecture
// Remove user from brand removed - not part of simplified architecture
// Switch brand context removed - not part of simplified architecture