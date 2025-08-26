using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

/// <summary>
/// Represents a brand in the multi-tenant system
/// Stores only essential information and PostgreSQL credentials
/// All brand details are stored in the brand's own schema
/// </summary>
public class Brand
{
    /// <summary>
    /// Unique identifier for the brand (UUID)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name of the brand
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the principal user who owns this brand
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// PostgreSQL schema name (e.g., "brand_acme")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SchemaName { get; set; } = string.Empty;
    
    /// <summary>
    /// PostgreSQL database user that has full access to this schema
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DatabaseUser { get; set; } = string.Empty;
    
    /// <summary>
    /// Encrypted PostgreSQL password for the database user
    /// </summary>
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the brand is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the brand was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the brand was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property to the owner user
    /// </summary>
    public User? Owner { get; set; }
}

/// <summary>
/// Brand status enumeration
/// </summary>
public enum BrandStatus
{
    Active,
    Suspended,
    Deleted
}