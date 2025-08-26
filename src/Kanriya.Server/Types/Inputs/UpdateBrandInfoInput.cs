namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for updating brand info (key-value pairs in brand_xxx.infoes table)
/// </summary>
public class UpdateBrandInfoInput
{
    /// <summary>
    /// The key to update in the brand's infoes table
    /// </summary>
    public required string Key { get; set; }
    
    /// <summary>
    /// The value to set for this key
    /// </summary>
    public required string Value { get; set; }
}