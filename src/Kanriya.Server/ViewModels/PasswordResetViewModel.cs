using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.ViewModels;

/// <summary>
/// ViewModel for password reset page
/// </summary>
public class PasswordResetViewModel
{
    /// <summary>
    /// The reset token from the URL
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// The new password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Confirm the new password
    /// </summary>
    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the reset was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message to display to the user
    /// </summary>
    public string? Message { get; set; }
}