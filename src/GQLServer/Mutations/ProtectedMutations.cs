using HotChocolate;
using HotChocolate.Authorization;
using System.Security.Claims;

namespace GQLServer.Mutations;

// PROTECTED MUTATIONS - Examples of mutations with different authorization levels
// ================================================================================
// These mutations demonstrate:
// - User-level operations (any authenticated user)
// - Moderator-level operations
// - Admin-level operations
// - Policy-based operations

[ExtendObjectType("Mutation")]
public class ProtectedMutations
{
    // USER-LEVEL MUTATION - Any authenticated user can update their profile
    // Usage (must be logged in):
    // mutation {
    //   updateMyProfile(newEmail: "newemail@example.com") {
    //     success
    //     message
    //   }
    // }
    [Authorize]
    public UpdateResult UpdateMyProfile(
        string newEmail,
        ClaimsPrincipal claimsPrincipal)
    {
        var username = claimsPrincipal.Identity?.Name;
        
        // In a real app, update the user's email in the database
        return new UpdateResult
        {
            Success = true,
            Message = $"Profile updated for user {username}. Email changed to {newEmail}"
        };
    }
    
    // MODERATOR-LEVEL MUTATION - Moderators can moderate content
    // Usage (must be Moderator or Admin):
    // mutation {
    //   moderateContent(contentId: 123, action: "approve") {
    //     success
    //     message
    //   }
    // }
    [Authorize(Roles = new[] { "Moderator", "Admin" })]
    public UpdateResult ModerateContent(
        int contentId,
        string action,
        ClaimsPrincipal claimsPrincipal)
    {
        var moderator = claimsPrincipal.Identity?.Name;
        var role = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;
        
        return new UpdateResult
        {
            Success = true,
            Message = $"{role} {moderator} {action}d content #{contentId}"
        };
    }
    
    // ADMIN-ONLY MUTATION - Only admins can delete users
    // Usage (must be Admin):
    // mutation {
    //   deleteUser(userId: 123) {
    //     success
    //     message
    //   }
    // }
    [Authorize(Roles = new[] { "Admin" })]
    public UpdateResult DeleteUser(
        int userId,
        ClaimsPrincipal claimsPrincipal)
    {
        var admin = claimsPrincipal.Identity?.Name;
        
        // In a real app, delete the user from database
        return new UpdateResult
        {
            Success = true,
            Message = $"Admin {admin} deleted user #{userId}"
        };
    }
    
    // POLICY-BASED MUTATION - Using the "ModeratorOrAbove" policy
    // Usage (must be Moderator or Admin):
    // mutation {
    //   banUser(userId: 123, reason: "Spam", duration: 7) {
    //     success
    //     message
    //   }
    // }
    [Authorize(Policy = "ModeratorOrAbove")]
    public UpdateResult BanUser(
        int userId,
        string reason,
        int duration,
        ClaimsPrincipal claimsPrincipal)
    {
        var moderator = claimsPrincipal.Identity?.Name;
        var role = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;
        
        return new UpdateResult
        {
            Success = true,
            Message = $"{role} {moderator} banned user #{userId} for {duration} days. Reason: {reason}"
        };
    }
    
    // SELF-SERVICE MUTATION - Users can only modify their own data
    // Usage (must be logged in):
    // mutation {
    //   changeMyPassword(oldPassword: "current", newPassword: "newpass") {
    //     success
    //     message
    //   }
    // }
    [Authorize]
    public UpdateResult ChangeMyPassword(
        string oldPassword,
        string newPassword,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = claimsPrincipal.Identity?.Name;
        
        // In a real app:
        // 1. Verify the old password
        // 2. Hash the new password
        // 3. Update in database
        
        return new UpdateResult
        {
            Success = true,
            Message = $"Password changed successfully for user {username} (ID: {userId})"
        };
    }
    
    // COMPLEX AUTHORIZATION - Multiple checks
    // Only admins can grant roles, but they can't grant Admin role to themselves
    // Usage (must be Admin):
    // mutation {
    //   grantRole(userId: 2, newRole: "Moderator") {
    //     success
    //     message
    //   }
    // }
    [Authorize(Roles = new[] { "Admin" })]
    public UpdateResult GrantRole(
        int userId,
        string newRole,
        ClaimsPrincipal claimsPrincipal)
    {
        var adminId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var adminName = claimsPrincipal.Identity?.Name;
        
        // Prevent self-promotion to Admin
        if (int.Parse(adminId!) == userId && newRole == "Admin")
        {
            throw new GraphQLException("Admins cannot grant Admin role to themselves");
        }
        
        // Validate role
        var validRoles = new[] { "User", "Moderator", "Admin" };
        if (!validRoles.Contains(newRole))
        {
            throw new GraphQLException($"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
        }
        
        return new UpdateResult
        {
            Success = true,
            Message = $"Admin {adminName} granted {newRole} role to user #{userId}"
        };
    }
}

// Helper type for mutation results
public class UpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}