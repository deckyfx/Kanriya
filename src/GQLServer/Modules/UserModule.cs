using GQLServer.Constants;
using GQLServer.Data;
using GQLServer.Services;
using GQLServer.Types;
using GQLServer.Types.Inputs;
using GQLServer.Types.Outputs;
using GQLServer.Queries;
using GQLServer.Mutations;
using GQLServer.Subscriptions;
using HotChocolate.AspNetCore;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using HotChocolate.Execution;

namespace GQLServer.Modules;

/// <summary>
/// GraphQL module for User/Authentication domain
/// Contains all queries, mutations, and subscriptions related to Users and Authentication
/// </summary>
[ExtendObjectType(typeof(RootQuery))]
public class UserQueries
{
    /// <summary>
    /// Get the current authenticated user
    /// </summary>
    [Authorize]
    [GraphQLName("me")]
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
    [GraphQLName("userById")]
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
    [GraphQLName("userByEmail")]
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
    [GraphQLName("users")]
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
    [GraphQLName("pendingUsers")]
    public async Task<IEnumerable<PendingUser>> GetPendingUsers(
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        return await userService.GetPendingUsersAsync(cancellationToken);
    }
    
    /// <summary>
    /// Check if an email is available for registration
    /// </summary>
    [GraphQLName("isEmailAvailable")]
    public async Task<bool> IsEmailAvailable(
        string email,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailAsync(email, cancellationToken);
        return user == null;
    }
}

[ExtendObjectType(typeof(RootMutation))]
public class UserAuthMutations
{
    /// <summary>
    /// Sign up a new user
    /// </summary>
    [GraphQLName("signUp")]
    public async Task<AuthPayload> SignUp(
        SignUpInput input,
        [Service] IUserService userService,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.SignUpAsync(input.Email, input.Password, cancellationToken);
        
        // Note: PendingUser creation event could be published here if we had access to the PendingUser
        // For now, we rely on the service to handle this internally
        
        return new AuthPayload
        {
            Success = result.Success,
            Message = result.Message,
            VerificationToken = result.VerificationToken
        };
    }
    
    /// <summary>
    /// Verify email address with token
    /// </summary>
    [GraphQLName("verifyEmail")]
    public async Task<AuthPayload> VerifyEmail(
        string verificationToken,
        [Service] IUserService userService,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.VerifyEmailAsync(verificationToken, cancellationToken);
        
        string? token = null;
        if (result.Success && result.User != null)
        {
            // Generate JWT token for the verified user
            token = userService.GenerateJwtToken(result.User);
            
            // Publish user created event
            var userEvt = new SubscriptionEvent<User>
            {
                Event = EventType.Created,
                Document = result.User,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await sender.SendAsync("UserChanges", userEvt, cancellationToken);
            
            // Note: PendingUser deletion event is handled by the service
        }
        
        return new AuthPayload
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User,
            Token = token
        };
    }
    
    /// <summary>
    /// Sign in an existing user
    /// </summary>
    [GraphQLName("signIn")]
    public async Task<AuthPayload> SignIn(
        SignInInput input,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.SignInAsync(input.Email, input.Password, cancellationToken);
        
        return new AuthPayload
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User,
            Token = result.Token
        };
    }
    
    /// <summary>
    /// Resend verification email
    /// </summary>
    [GraphQLName("resendVerification")]
    public async Task<AuthPayload> ResendVerification(
        string email,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.ResendVerificationAsync(email, cancellationToken);
        
        return new AuthPayload
        {
            Success = result.Success,
            Message = result.Message,
            VerificationToken = result.NewToken  // Note: field is NewToken, not VerificationToken
        };
    }
    
    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [Authorize]
    [GraphQLName("changePassword")]
    public async Task<AuthPayload> ChangePassword(
        string currentPassword,
        string newPassword,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new AuthPayload
            {
                Success = false,
                Message = "User not authenticated"
            };
        }
        
        var result = await userService.ChangePasswordAsync(
            currentUser.User.Id, 
            currentPassword, 
            newPassword, 
            cancellationToken);
        
        return new AuthPayload
        {
            Success = result.Success,
            Message = result.Message
            // Note: ChangePasswordAsync doesn't return User or Token
        };
    }
    
    /// <summary>
    /// Update user profile
    /// </summary>
    [Authorize]
    [GraphQLName("updateProfile")]
    public async Task<AuthPayload> UpdateProfile(
        string? fullName,
        string? profilePictureUrl,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new AuthPayload
            {
                Success = false,
                Message = "User not authenticated"
            };
        }
        
        var oldUser = await userService.GetByIdAsync(currentUser.User.Id, cancellationToken);
        var updatedUser = await userService.UpdateProfileAsync(
            currentUser.User.Id,
            fullName,
            profilePictureUrl,
            cancellationToken);
        
        if (updatedUser != null && oldUser != null)
        {
            // Publish user updated event
            var evt = new SubscriptionEvent<User>
            {
                Event = EventType.Updated,
                Document = updatedUser,
                Time = DateTime.UtcNow,
                Previous = oldUser
            };
            await sender.SendAsync("UserChanges", evt, cancellationToken);
        }
        
        return new AuthPayload
        {
            Success = updatedUser != null,
            Message = updatedUser != null ? "Profile updated successfully" : "Failed to update profile",
            User = updatedUser
        };
    }
}

[ExtendObjectType(typeof(RootMutation))]
public class UserManagementMutations
{
    /// <summary>
    /// Grant a role to a user (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    [GraphQLName("grantRole")]
    public async Task<UserManagementResult> GrantRole(
        string userId,
        string role,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!UserRoles.IsValidRole(role))
        {
            return new UserManagementResult
            {
                Success = false,
                Message = $"Invalid role: {role}. Valid roles are: {string.Join(", ", UserRoles.AllRoles)}"
            };
        }
        
        var result = await userService.GrantRoleAsync(userId, role, currentUser?.User?.Id, cancellationToken);
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
    [GraphQLName("revokeRole")]
    public async Task<UserManagementResult> RevokeRole(
        string userId,
        string role,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        if (!UserRoles.IsValidRole(role))
        {
            return new UserManagementResult
            {
                Success = false,
                Message = $"Invalid role: {role}. Valid roles are: {string.Join(", ", UserRoles.AllRoles)}"
            };
        }
        
        var result = await userService.RevokeRoleAsync(userId, role, cancellationToken);
        return new UserManagementResult
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User
        };
    }
    
    /// <summary>
    /// Delete a user (SuperAdmin can delete any, users can delete themselves)
    /// </summary>
    [Authorize]
    [GraphQLName("deleteUser")]
    public async Task<UserManagementResult> DeleteUser(
        string userId,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        // Check if user is deleting themselves or is SuperAdmin
        bool isSelfDelete = currentUser?.User?.Id == userId;
        bool isSuperAdmin = currentUser?.User?.UserRoles?.Any(r => r.Role == UserRoles.SuperAdmin) ?? false;
        
        if (!isSelfDelete && !isSuperAdmin)
        {
            return new UserManagementResult
            {
                Success = false,
                Message = "You can only delete your own account unless you are a SuperAdmin"
            };
        }
        
        var user = await userService.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return new UserManagementResult
            {
                Success = false,
                Message = "User not found"
            };
        }
        
        var success = await userService.DeleteUserAsync(userId, cancellationToken);
        
        if (success)
        {
            // Publish user deleted event
            var evt = new SubscriptionEvent<User>
            {
                Event = EventType.Deleted,
                Document = null,
                Time = DateTime.UtcNow,
                Previous = user
            };
            await sender.SendAsync("UserChanges", evt, cancellationToken);
        }
        
        return new UserManagementResult
        {
            Success = success,
            Message = success ? "User deleted successfully" : "Failed to delete user"
        };
    }
    
    /// <summary>
    /// Force verify a pending user (SuperAdmin only)
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    [GraphQLName("forceVerifyUser")]
    public async Task<UserManagementResult> ForceVerifyUser(
        string pendingUserId,
        [Service] IUserService userService,
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.ForceVerifyUserAsync(pendingUserId, cancellationToken);
        
        if (result.Success && result.User != null)
        {
            // Publish user created event
            var userEvt = new SubscriptionEvent<User>
            {
                Event = EventType.Created,
                Document = result.User,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await sender.SendAsync("UserChanges", userEvt, cancellationToken);
        }
        
        return new UserManagementResult
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User
        };
    }
}

[ExtendObjectType(typeof(RootSubscription))]
public class UserSubscriptions
{
    /// <summary>
    /// Subscribe to all user changes (created, updated, deleted)
    /// </summary>
    [Subscribe]
    [Topic("UserChanges")]
    [GraphQLName("onUserChanged")]
    public SubscriptionEvent<User> OnUserChanged([EventMessage] SubscriptionEvent<User> userEvent) => userEvent;
    
    /// <summary>
    /// Subscribe to pending user changes
    /// </summary>
    [Subscribe]
    [Topic("PendingUserChanges")]
    [GraphQLName("onPendingUserChanged")]
    public SubscriptionEvent<PendingUser> OnPendingUserChanged([EventMessage] SubscriptionEvent<PendingUser> pendingUserEvent) => pendingUserEvent;
}

/// <summary>
/// Result type for user management operations
/// </summary>
public class UserManagementResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
}