namespace GQLServer.Types.Inputs;

// REGISTER INPUT - Used for user registration
// ============================================
// Input type for the register mutation

public class RegisterInput
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; } // Optional: Admin can set role during registration
}