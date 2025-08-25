using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

/// <summary>
/// Represents a verified user in the system
/// Users are created after email verification from pending_users
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User's email address (unique)
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Hashed password (never store plain text)
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// User's full name
    /// </summary>
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// When the user account was created (verified)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }
    
    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Password reset token (null when not resetting)
    /// </summary>
    public string? PasswordResetToken { get; set; }
    
    /// <summary>
    /// When the password reset token expires
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    /// <summary>
    /// Navigation property for user roles
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}