namespace GQLServer.Types.Outputs;

// USER TYPE - Represents a user in the system
// ============================================
// This is what we return when querying user information

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Default role
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Note: We never expose the password hash in GraphQL
    // The password hash is stored separately and never returned
}