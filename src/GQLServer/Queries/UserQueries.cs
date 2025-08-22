using HotChocolate;
using HotChocolate.Authorization;
using System.Security.Claims;
using GQLServer.Services;
using GQLServer.Types.Outputs;

namespace GQLServer.Queries;

// USER QUERIES - Protected queries with authentication and authorization
// ======================================================================
// These queries demonstrate:
// - Public queries (no authentication required)
// - Authenticated queries (any logged-in user)
// - Role-based queries (specific roles required)
// - Policy-based queries (complex authorization rules)

[ExtendObjectType("Query")]
public class UserQueries
{
    // PUBLIC QUERY - No authentication required
    // Anyone can check if the server is running
    public string GetServerStatus() => "Server is running!";
    
    // AUTHENTICATED QUERY - Requires any authenticated user
    // Usage (must be logged in):
    // query {
    //   me {
    //     id
    //     username
    //     email
    //     role
    //   }
    // }
    [Authorize] // Requires authentication but any role
    public User? GetMe(
        [Service] AuthService authService,
        ClaimsPrincipal claimsPrincipal)
    {
        // Get the user ID from JWT claims
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user token");
        }
        
        return authService.GetUserById(userId);
    }
    
    // ADMIN-ONLY QUERY - Requires Admin role
    // Usage (must be logged in as Admin):
    // query {
    //   allUsers {
    //     id
    //     username
    //     email
    //     role
    //     createdAt
    //   }
    // }
    [Authorize(Roles = new[] { "Admin" })]
    public List<User> GetAllUsers([Service] AuthService authService)
    {
        return authService.GetAllUsers();
    }
    
    // MODERATOR OR ADMIN QUERY - Requires Moderator or Admin role
    // Usage (must be Moderator or Admin):
    // query {
    //   userById(id: 1) {
    //     id
    //     username
    //     email
    //     role
    //   }
    // }
    [Authorize(Roles = new[] { "Admin", "Moderator" })]
    public User? GetUserById(
        int id,
        [Service] AuthService authService)
    {
        return authService.GetUserById(id);
    }
    
    // POLICY-BASED QUERY - Uses custom policy
    // This uses the "ModeratorOrAbove" policy defined in Program.cs
    // Usage (must be Moderator or Admin):
    // query {
    //   systemStats {
    //     totalUsers
    //     totalAdmins
    //     totalModerators
    //   }
    // }
    [Authorize(Policy = "ModeratorOrAbove")]
    public SystemStats GetSystemStats([Service] AuthService authService)
    {
        var allUsers = authService.GetAllUsers();
        
        return new SystemStats
        {
            TotalUsers = allUsers.Count,
            TotalAdmins = allUsers.Count(u => u.Role == "Admin"),
            TotalModerators = allUsers.Count(u => u.Role == "Moderator")
        };
    }
    
    // MIXED AUTHORIZATION - Different fields have different requirements
    // The User type itself can have authorized fields
    // query {
    //   publicUserInfo(username: "admin") {
    //     username     # Public
    //     email        # Requires authentication
    //     role         # Requires Moderator or Admin
    //   }
    // }
    public PublicUserInfo? GetPublicUserInfo(
        string username,
        [Service] AuthService authService)
    {
        var users = authService.GetAllUsers();
        var user = users.FirstOrDefault(u => u.Username == username);
        
        if (user == null)
            return null;
        
        return new PublicUserInfo
        {
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}

// Helper type for system statistics
public class SystemStats
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalModerators { get; set; }
}

// Type with field-level authorization
public class PublicUserInfo
{
    // Public field - anyone can see
    public string Username { get; set; } = string.Empty;
    
    // Protected field - requires authentication
    [Authorize]
    public string Email { get; set; } = string.Empty;
    
    // Admin/Moderator only field
    [Authorize(Roles = new[] { "Admin", "Moderator" })]
    public string Role { get; set; } = string.Empty;
    
    // Public field
    public DateTime CreatedAt { get; set; }
}