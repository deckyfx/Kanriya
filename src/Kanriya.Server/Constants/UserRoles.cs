namespace Kanriya.Server.Constants;

/// <summary>
/// Defines all available user roles in the system
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Super Administrator - Has complete system access
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";
    
    /// <summary>
    /// Regular User - Basic authenticated user
    /// </summary>
    public const string User = "User";
    
    /// <summary>
    /// Brand Owner - Owns one or more brands
    /// </summary>
    public const string BrandOwner = "BrandOwner";
    
    /// <summary>
    /// Brand Operator - Operates on behalf of a brand
    /// </summary>
    public const string BrandOperator = "BrandOperator";
    
    /// <summary>
    /// Get all available roles
    /// </summary>
    public static readonly string[] AllRoles = new[]
    {
        SuperAdmin,
        User,
        BrandOwner,
        BrandOperator
    };
    
    /// <summary>
    /// Check if a role is valid
    /// </summary>
    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}