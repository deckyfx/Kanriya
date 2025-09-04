using HotChocolate;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// GraphQL input type for request options
/// </summary>
[GraphQLDescription("Optional request configuration for service calls")]
public class RequestOptionsInput
{
    /// <summary>
    /// Skip sending emails (for development/testing)
    /// </summary>
    [GraphQLDescription("Skip sending emails (for development/testing). Prevents email bombing during development.")]
    public bool? SkipEmail { get; set; }
    
    /// <summary>
    /// Language code for localized responses
    /// </summary>
    [GraphQLDescription("Language code for localized responses (e.g., 'en', 'ja', 'zh'). If not specified, uses system default.")]
    public string? Lang { get; set; }
    
    /// <summary>
    /// Convert to shared RequestOptions model
    /// </summary>
    public Kanriya.Shared.Models.RequestOptions ToRequestOptions()
    {
        return new Kanriya.Shared.Models.RequestOptions
        {
            SkipEmail = SkipEmail ?? false,
            Lang = Lang
        };
    }
    
    /// <summary>
    /// Create from shared RequestOptions model
    /// </summary>
    public static RequestOptionsInput FromRequestOptions(Kanriya.Shared.Models.RequestOptions options)
    {
        return new RequestOptionsInput
        {
            SkipEmail = options.SkipEmail,
            Lang = options.Lang
        };
    }
}