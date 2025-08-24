using Kanriya.Server.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Types;
using HotChocolate.AspNetCore;
using HotChocolate.Authorization;

namespace Kanriya.Server.Queries;

/// <summary>
/// GraphQL queries for user operations
/// </summary>
[ExtendObjectType(typeof(RootQuery))]
public class UserQueries
{
    /// <summary>
    /// Get the current authenticated user
    /// </summary>
    [Authorize]
    [GraphQLDescription("Get the current authenticated user")]
    public async Task<User?> GetMe(
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
            return null;
        
        return await userService.GetByIdAsync(currentUser.User.Id, cancellationToken);
    }
    
    /// <summary>
    /// Get a user by ID (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<User?> GetUserById(
        string id,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        return await userService.GetByIdAsync(id, cancellationToken);
    }
    
    /// <summary>
    /// Get a user by email (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<User?> GetUserByEmail(
        string email,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        return await userService.GetByEmailAsync(email, cancellationToken);
    }
    
    /// <summary>
    /// Get all users with pagination (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IEnumerable<User>> GetUsers(
        [Service] IUserService userService,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return await userService.GetAllUsersAsync(skip, take, cancellationToken);
    }
    
    /// <summary>
    /// Get pending users awaiting verification (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IEnumerable<PendingUser>> GetPendingUsers(
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        return await userService.GetPendingUsersAsync(cancellationToken);
    }
    
    /// <summary>
    /// Check if an email is available for registration
    /// </summary>
    public async Task<bool> IsEmailAvailable(
        string email,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailAsync(email, cancellationToken);
        return user == null;
    }
}