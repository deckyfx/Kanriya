using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data.BrandSchema;

/// <summary>
/// User-Outlet permission mapping entity
/// Determines which outlets a brand user can access
/// </summary>
public class UserOutlet
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Foreign key to BrandUser
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Foreign key to Outlet
    /// </summary>
    [Required]
    public string OutletId { get; set; } = string.Empty;
    
    /// <summary>
    /// When the permission was granted
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property to BrandUser
    /// </summary>
    public BrandUser User { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to Outlet
    /// </summary>
    public Outlet Outlet { get; set; } = null!;
}