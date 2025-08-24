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
    /// Business Owner - Owns one or more businesses
    /// </summary>
    public const string BusinessOwner = "BusinessOwner";
    
    /// <summary>
    /// Business Operator - Operates on behalf of a business
    /// </summary>
    public const string BusinessOperator = "BusinessOperator";
    
    /// <summary>
    /// Get all available roles
    /// </summary>
    public static readonly string[] AllRoles = new[]
    {
        SuperAdmin,
        User,
        BusinessOwner,
        BusinessOperator
    };
    
    /// <summary>
    /// Check if a role is valid
    /// </summary>
    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}