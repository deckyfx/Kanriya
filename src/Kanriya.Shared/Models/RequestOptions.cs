namespace Kanriya.Shared.Models;

/// <summary>
/// General request options for service calls
/// Allows extending functionality without changing method signatures
/// </summary>
public class RequestOptions
{
    /// <summary>
    /// Skip sending emails (for development/testing)
    /// Used to prevent email bombing during development
    /// </summary>
    public bool SkipEmail { get; set; } = false;
    
    /// <summary>
    /// Language code for localized responses (e.g., "en", "ja", "zh")
    /// If null, uses system default language
    /// </summary>
    public string? Lang { get; set; }
    
    /// <summary>
    /// Create default RequestOptions
    /// </summary>
    public static RequestOptions Default => new RequestOptions();
    
    /// <summary>
    /// Create RequestOptions for development/testing
    /// </summary>
    public static RequestOptions Development => new RequestOptions 
    { 
        SkipEmail = true
    };
    
    /// <summary>
    /// Create RequestOptions with specific language
    /// </summary>
    public static RequestOptions WithLanguage(string language) => new RequestOptions 
    { 
        Lang = language 
    };
}