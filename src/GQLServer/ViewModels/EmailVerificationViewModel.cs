namespace GQLServer.ViewModels;

/// <summary>
/// View model for email verification page
/// </summary>
public class EmailVerificationViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? UserEmail { get; set; }
}