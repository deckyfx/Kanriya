using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Types.Inputs;

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
    
    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}