using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

/// <summary>
/// Represents a role assigned to a user
/// One user can have multiple roles
/// </summary>
public class UserRole
{
    /// <summary>
    /// Unique identifier for the user role assignment
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Foreign key to User
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The role name (from UserRoles constants)
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// When this role was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who assigned this role (user ID of the admin)
    /// </summary>
    public string? AssignedBy { get; set; }
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User User { get; set; } = null!;
}