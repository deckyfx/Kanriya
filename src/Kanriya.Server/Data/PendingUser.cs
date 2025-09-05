using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

/// <summary>
/// Represents a user pending email verification
/// After verification, data is moved to users table and deleted from here
/// </summary>
public class PendingUser
{
    /// <summary>
    /// Unique identifier for the pending user
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
    /// User's first name
    /// </summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// When the pending user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Email verification token
    /// </summary>
    [Required]
    public string VerificationToken { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Token expiry time (24 hours from creation)
    /// </summary>
    public DateTime TokenExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}