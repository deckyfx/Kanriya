namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for brand authentication using API credentials
/// </summary>
public class BrandSignInInput
{
    [GraphQLDescription("16-character API Secret (username)")]
    public required string ApiSecret { get; set; }
    
    [GraphQLDescription("32-character API Password")]
    public required string ApiPassword { get; set; }
    
    [GraphQLDescription("Brand ID to authenticate into")]
    public required string BrandId { get; set; }
}