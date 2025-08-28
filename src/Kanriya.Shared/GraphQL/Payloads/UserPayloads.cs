namespace Kanriya.Shared.GraphQL.Payloads;

using Kanriya.Shared.GraphQL.Types;

public class SignUpOutput
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class SignInOutput
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class ResendVerificationEmailOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class VerifyEmailOutput
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class ForgotPasswordOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ResetPasswordOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ChangePasswordOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class UpdateProfileOutput
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class DeleteAccountOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}