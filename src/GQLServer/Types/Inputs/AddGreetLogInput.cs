using System.ComponentModel.DataAnnotations;

namespace GQLServer.Types.Inputs;

/// <summary>
/// Input type for adding a new greet log entry
/// Used in the addGreetLog mutation
/// </summary>
public class AddGreetLogInput
{
    /// <summary>
    /// The content of the greeting message
    /// Must be between 1 and 500 characters
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [MinLength(1, ErrorMessage = "Content must be at least 1 character")]
    [MaxLength(500, ErrorMessage = "Content cannot exceed 500 characters")]
    public string Content { get; set; } = string.Empty;
}