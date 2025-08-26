using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data.BrandSchema;

/// <summary>
/// User role entity for brand schema
/// </summary>
public class BrandUserRole
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User ID reference
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Role name (e.g., "BrandOwner")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the role was assigned
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the role was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property to user
    /// </summary>
    public BrandUser User { get; set; } = null!;
}

/// <summary>
/// Brand roles
/// </summary>
public static class BrandRoles
{
    /// <summary>
    /// Brand owner - full control
    /// </summary>
    public const string BrandOwner = "BrandOwner";
    
    /// <summary>
    /// Brand operator - can manage brand info and settings
    /// </summary>
    public const string BrandOperator = "BrandOperator";
    
    // Future roles can be added here
    // public const string BrandAdmin = "BrandAdmin";
    // public const string BrandMember = "BrandMember";
    
    /// <summary>
    /// All available brand roles
    /// </summary>
    public static readonly string[] AllRoles = [BrandOwner, BrandOperator];
    
    /// <summary>
    /// Check if a role is valid
    /// </summary>
    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}