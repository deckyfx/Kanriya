using Kanriya.Server.Constants;
using Kanriya.Server.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;
using Kanriya.Server.Types;
using HotChocolate.Authorization;

namespace Kanriya.Server.Mutations;

/// <summary>
/// GraphQL mutations for user management operations (SuperAdmin only)
/// </summary>
[ExtendObjectType(typeof(RootMutation))]
public class UserManagementMutations
{
    /// <summary>
    /// Grant a role to a user (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<UserManagementResult> GrantRole(
        string userId,
        string role,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Validate role
        if (!UserRoles.IsValidRole(role))
        {
            return new UserManagementResult
            {
                Success = false,
                Message = $"Invalid role: {role}. Valid roles are: {string.Join(", ", UserRoles.AllRoles)}"
            };
        }
        
        var result = await userService.GrantRoleAsync(userId, role, currentUser.User?.Id, cancellationToken);
        
        return new UserManagementResult
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User
        };
    }
    
    /// <summary>
    /// Revoke a role from a user (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<UserManagementResult> RevokeRole(
        string userId,
        string role,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.RevokeRoleAsync(userId, role, cancellationToken);
        
        return new UserManagementResult
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User
        };
    }
    
    /// <summary>
    /// Delete a user account (SuperAdmin only, or user deleting their own account)
    /// </summary>
    [Authorize]
    public async Task<UserManagementResult> DeleteUser(
        string userId,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Check if user is deleting their own account or is a SuperAdmin
        if (currentUser.User?.Id != userId && !currentUser.IsSuperAdmin)
        {
            return new UserManagementResult
            {
                Success = false,
                Message = "You can only delete your own account unless you're a SuperAdmin"
            };
        }
        
        var result = await userService.DeleteUserAsync(userId, cancellationToken);
        
        return new UserManagementResult
        {
            Success = result,
            Message = result ? "User deleted successfully" : "Failed to delete user"
        };
    }
    
    /// <summary>
    /// Force verify a user's email (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<UserManagementResult> ForceVerifyUser(
        string pendingUserId,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.ForceVerifyUserAsync(pendingUserId, cancellationToken);
        
        return new UserManagementResult
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User
        };
    }
}

/// <summary>
/// Result type for user management operations
/// </summary>
public class UserManagementResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The affected user (if applicable)
    /// </summary>
    public User? User { get; set; }
}