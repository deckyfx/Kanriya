using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data.BrandSchema;

/// <summary>
/// Outlet entity representing a physical or logical business location within a brand
/// </summary>
public class Outlet
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Unique outlet code within the brand (e.g., "OUTLET001", "MALL-A")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the outlet (e.g., "Geprek Bensu - Mall A")
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Physical address of the outlet
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Whether the outlet is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the outlet was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the outlet was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property for user-outlet permissions
    /// </summary>
    public ICollection<UserOutlet> UserOutlets { get; set; } = new List<UserOutlet>();
    
    // TODO: Navigation property for outlet employees (future implementation)
    // public ICollection<OutletEmployee> OutletEmployees { get; set; } = new List<OutletEmployee>();
}