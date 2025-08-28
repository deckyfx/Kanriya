using Kanriya.Server.Constants;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using System.Linq;

namespace Kanriya.Server.Types;

/// <summary>
/// Represents the current authenticated user in the GraphQL context
/// Can be either a principal user or a brand-context user
/// </summary>
public class CurrentUser
{
    /// <summary>
    /// The authenticated principal user (when using email/password authentication)
    /// </summary>
    public User? User { get; set; }
    
    /// <summary>
    /// The authenticated brand user (when using brand API credentials)
    /// </summary>
    public BrandUser? BrandUser { get; set; }
    
    /// <summary>
    /// The brand ID when in brand context
    /// </summary>
    public string? BrandId { get; set; }
    
    /// <summary>
    /// The brand schema name when in brand context
    /// </summary>
    public string? BrandSchema { get; set; }
    
    /// <summary>
    /// List of outlet IDs the user has access to (in brand context)
    /// Empty for BrandOwner (who has access to all outlets)
    /// </summary>
    public List<string> OutletIds { get; set; } = new List<string>();
    
    /// <summary>
    /// Whether the user has access to all outlets (BrandOwner role)
    /// </summary>
    public bool HasAllOutletAccess { get; set; }
    
    /// <summary>
    /// Whether the user is authenticated (either principal or brand context)
    /// </summary>
    public bool IsAuthenticated => User != null || BrandUser != null;
    
    /// <summary>
    /// Whether the user is in brand context (using brand API credentials)
    /// </summary>
    public bool IsBrandContext => BrandUser != null && !string.IsNullOrEmpty(BrandId);
    
    /// <summary>
    /// Whether the user is in principal context (using email/password)
    /// </summary>
    public bool IsPrincipalContext => User != null && !IsBrandContext;
    
    /// <summary>
    /// Whether the user has a specific role (for principal users)
    /// </summary>
    public bool HasRole(string role) => 
        User?.UserRoles?.Any(ur => ur.Role == role) ?? false;
    
    /// <summary>
    /// Whether the brand user has a specific role (for brand-context users)
    /// </summary>
    public bool HasBrandRole(string role) => 
        BrandUser?.Roles?.Any(ur => ur.Role == role) ?? false;
    
    /// <summary>
    /// Whether the user is a super admin
    /// </summary>
    public bool IsSuperAdmin => HasRole(UserRoles.SuperAdmin);
    
    /// <summary>
    /// Whether the user is a brand owner (principal context)
    /// </summary>
    public bool IsBrandOwner => HasRole(UserRoles.BrandOwner);
    
    /// <summary>
    /// Whether the user is a brand operator (principal context)
    /// </summary>
    public bool IsBrandOperator => HasRole(UserRoles.BrandOperator);
    
    /// <summary>
    /// Get all roles for the current user
    /// </summary>
    public string[] GetRoles() => 
        User?.UserRoles?.Select(ur => ur.Role).ToArray() ?? Array.Empty<string>();
    
    /// <summary>
    /// Get all brand roles for the current brand user
    /// </summary>
    public string[] GetBrandRoles() => 
        BrandUser?.Roles?.Select(ur => ur.Role).ToArray() ?? Array.Empty<string>();
    
    /// <summary>
    /// Check if the user has access to a specific outlet
    /// </summary>
    public bool HasOutletAccess(string outletId)
    {
        if (!IsBrandContext)
            return false;
            
        // BrandOwner has access to all outlets
        if (HasAllOutletAccess || HasBrandRole(BrandRoles.BrandOwner))
            return true;
            
        // Check if outlet ID is in user's outlet list
        return OutletIds.Contains(outletId);
    }
}