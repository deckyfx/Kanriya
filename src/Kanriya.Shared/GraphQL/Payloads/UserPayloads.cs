namespace Kanriya.Shared.GraphQL.Payloads;

using Kanriya.Shared.GraphQL.Types;

public class SignUpPayload
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class SignInPayload
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class ResendVerificationEmailPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class VerifyEmailPayload
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class ForgotPasswordPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ResetPasswordPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ChangePasswordPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class UpdateProfilePayload
{
    public bool Success { get; set; }
    public UserType? User { get; set; }
    public string? Message { get; set; }
}

public class DeleteAccountPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}