using System.ComponentModel.DataAnnotations;

namespace GQLServer.Data;

/// <summary>
/// Represents a greeting log entry in the database
/// </summary>
public class GreetLog
{
    /// <summary>
    /// Unique identifier for the greet log entry
    /// Using string to support various ID formats (GUID, custom format, etc.)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the greeting was logged
    /// Stored in UTC to ensure consistency across timezones
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The content of the greeting message
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;
}