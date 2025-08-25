using Kanriya.Server.Constants;
using Kanriya.Server.Data;
using System.Linq;

namespace Kanriya.Server.Types;

/// <summary>
/// Represents the current authenticated user in the GraphQL context
/// </summary>
public class CurrentUser
{
    /// <summary>
    /// The authenticated user
    /// </summary>
    public User? User { get; set; }
    
    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated => User != null;
    
    /// <summary>
    /// Whether the user has a specific role
    /// </summary>
    public bool HasRole(string role) => 
        User?.UserRoles?.Any(ur => ur.Role == role) ?? false;
    
    /// <summary>
    /// Whether the user is a super admin
    /// </summary>
    public bool IsSuperAdmin => HasRole(UserRoles.SuperAdmin);
    
    /// <summary>
    /// Whether the user is a brand owner
    /// </summary>
    public bool IsBrandOwner => HasRole(UserRoles.BrandOwner);
    
    /// <summary>
    /// Whether the user is a brand operator
    /// </summary>
    public bool IsBrandOperator => HasRole(UserRoles.BrandOperator);
    
    /// <summary>
    /// Get all roles for the current user
    /// </summary>
    public string[] GetRoles() => 
        User?.UserRoles?.Select(ur => ur.Role).ToArray() ?? Array.Empty<string>();
}