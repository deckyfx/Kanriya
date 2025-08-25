namespace Kanriya.Shared.GraphQL.Inputs;

public class SignUpInput
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class SignInInput
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ResendVerificationEmailInput
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailInput
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ForgotPasswordInput
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordInput
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordInput
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileInput
{
    public string? Name { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class DeleteMyAccountInput
{
    public string Password { get; set; } = string.Empty;
}