namespace GQLServer.Types.Inputs;

// LOGIN INPUT - Used for user authentication
// ===========================================
// Input type for the login mutation

public class LoginInput
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}