namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Represents a key-value pair from the brand's infoes table
/// </summary>
public class BrandInfo
{
    /// <summary>
    /// The key
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// The value
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// When this info was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this info was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}