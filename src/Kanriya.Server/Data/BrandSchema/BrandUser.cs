using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data.BrandSchema;

/// <summary>
/// User entity for brand schema
/// Uses API credentials instead of email/password
/// </summary>
public class BrandUser
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// API Secret - 16 character randomized string
    /// Used as username for API authentication
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string ApiSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// API Password - 32 character randomized string (hashed)
    /// Used as password for API authentication
    /// </summary>
    [Required]
    public string ApiPasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Brand schema name this user belongs to
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BrandSchema { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the user
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the user was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Navigation property for user roles
    /// </summary>
    public ICollection<BrandUserRole> Roles { get; set; } = new List<BrandUserRole>();
}