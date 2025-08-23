using System.ComponentModel.DataAnnotations;

namespace GQLServer.Types.Inputs;

/// <summary>
/// Input type for user sign up mutation
/// </summary>
public class SignUpInput
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password (will be hashed)
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}