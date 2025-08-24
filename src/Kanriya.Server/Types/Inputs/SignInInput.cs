using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input type for user sign in mutation
/// </summary>
public class SignInInput
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}