namespace GQLServer.Types.Outputs;

// AUTH PAYLOAD - Returned after successful authentication
// ========================================================
// Contains the JWT token and user information

public class AuthPayload
{
    public string Token { get; set; } = string.Empty;
    public User User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}