using Kanriya.Tests.Helpers;
using Spectre.Console;

namespace Kanriya.Tests.Actions;

/// <summary>
/// Reusable authentication actions for testing
/// Each action is atomic and can be used by any test suite
/// </summary>
public static class AuthActions
{
    /// <summary>
    /// Action: Sign up with valid credentials
    /// </summary>
    public static async Task<(bool success, TestUser user, string? verificationToken)> SignUpValidUser(
        UserTestHelper userHelper, 
        string prefix = "test")
    {
        var user = TestUser.Generate(prefix);
        var (success, token) = await userHelper.SignUpAsync(user);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ User signed up: {user.Email}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Sign up failed for: {user.Email}[/]");
        }
        
        return (success, user, token);
    }

    /// <summary>
    /// Action: Sign up with invalid email
    /// </summary>
    public static async Task<bool> SignUpInvalidEmail(UserTestHelper userHelper, string invalidEmail)
    {
        var user = new TestUser
        {
            Email = invalidEmail,
            Password = "ValidPassword123!"
        };
        
        var (success, _) = await userHelper.SignUpAsync(user);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Sign up with weak password
    /// </summary>
    public static async Task<bool> SignUpWeakPassword(UserTestHelper userHelper, string weakPassword)
    {
        var user = new TestUser
        {
            Email = $"test_{Guid.NewGuid():N}@test.com",
            Password = weakPassword
        };
        
        var (success, _) = await userHelper.SignUpAsync(user);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Verify email with token
    /// </summary>
    public static async Task<bool> VerifyEmail(UserTestHelper userHelper, string verificationToken)
    {
        var success = await userHelper.VerifyEmailAsync(verificationToken);
        
        if (success)
        {
            AnsiConsole.MarkupLine("[green dim]  ✓ Email verified[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red dim]  ✗ Email verification failed[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Resend verification email
    /// </summary>
    public static async Task<(bool success, string? newToken)> ResendVerification(
        UserTestHelper userHelper, 
        string email)
    {
        var (success, token) = await userHelper.ResendVerificationAsync(email);
        
        if (success)
        {
            AnsiConsole.MarkupLine("[green dim]  ✓ Verification email resent[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red dim]  ✗ Resend verification failed[/]");
        }
        
        return (success, token);
    }

    /// <summary>
    /// Action: Sign in with valid credentials
    /// </summary>
    public static async Task<(bool success, string? token)> SignInValid(
        UserTestHelper userHelper, 
        TestUser user)
    {
        var (success, token) = await userHelper.SignInAsync(user);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green dim]  ✓ Signed in: {user.Email}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red dim]  ✗ Sign in failed: {user.Email}[/]");
        }
        
        return (success, token);
    }

    /// <summary>
    /// Action: Sign in with wrong password
    /// </summary>
    public static async Task<bool> SignInWrongPassword(
        UserTestHelper userHelper, 
        string email, 
        string wrongPassword)
    {
        var user = new TestUser
        {
            Email = email,
            Password = wrongPassword
        };
        
        var (success, _) = await userHelper.SignInAsync(user);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Get user profile (requires auth)
    /// </summary>
    public static async Task<bool> GetUserProfile(UserTestHelper userHelper)
    {
        var success = await userHelper.GetMeAsync();
        
        if (success)
        {
            AnsiConsole.MarkupLine("[green dim]  ✓ Profile retrieved[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red dim]  ✗ Failed to get profile[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Change password with valid inputs
    /// </summary>
    public static async Task<bool> ChangePasswordValid(
        UserTestHelper userHelper,
        string currentPassword,
        string newPassword)
    {
        var success = await userHelper.ChangePasswordAsync(currentPassword, newPassword);
        
        if (success)
        {
            AnsiConsole.MarkupLine("[green dim]  ✓ Password changed[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red dim]  ✗ Password change failed[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Change password with wrong current password
    /// </summary>
    public static async Task<bool> ChangePasswordWrongCurrent(
        UserTestHelper userHelper,
        string wrongPassword,
        string newPassword)
    {
        var success = await userHelper.ChangePasswordAsync(wrongPassword, newPassword);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Delete account with correct password
    /// </summary>
    public static async Task<bool> DeleteAccountValid(
        UserTestHelper userHelper,
        string password)
    {
        var success = await userHelper.DeleteAccountAsync(password);
        
        if (success)
        {
            AnsiConsole.MarkupLine("[green dim]  ✓ Account deleted[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red dim]  ✗ Account deletion failed[/]");
        }
        
        return success;
    }

    /// <summary>
    /// Action: Delete account with wrong password
    /// </summary>
    public static async Task<bool> DeleteAccountWrongPassword(
        UserTestHelper userHelper,
        string wrongPassword)
    {
        var success = await userHelper.DeleteAccountAsync(wrongPassword);
        return !success; // Success means it was correctly rejected
    }

    /// <summary>
    /// Action: Create fully verified user ready for testing
    /// Returns user with valid token set in client
    /// </summary>
    public static async Task<TestUser?> CreateVerifiedUser(
        UserTestHelper userHelper,
        string prefix = "test")
    {
        // Sign up
        var (signUpSuccess, user, verificationToken) = await SignUpValidUser(userHelper, prefix);
        if (!signUpSuccess || string.IsNullOrEmpty(verificationToken))
        {
            AnsiConsole.MarkupLine("[red]Failed to create user[/]");
            return null;
        }

        // Verify email
        var verifySuccess = await VerifyEmail(userHelper, verificationToken);
        if (!verifySuccess)
        {
            AnsiConsole.MarkupLine("[red]Failed to verify user[/]");
            return null;
        }

        // Sign in
        var (signInSuccess, token) = await SignInValid(userHelper, user);
        if (!signInSuccess || string.IsNullOrEmpty(token))
        {
            AnsiConsole.MarkupLine("[red]Failed to sign in user[/]");
            return null;
        }

        user.Token = token;
        userHelper._client.SetAuthToken(token);
        
        return user;
    }

    /// <summary>
    /// Action: Cleanup user (delete if exists)
    /// Used for cleaning up verified test users at the end of test stages
    /// </summary>
    public static async Task CleanupUser(UserTestHelper userHelper, TestUser user)
    {
        try
        {
            // Try to sign in first
            var (signInSuccess, token) = await userHelper.SignInAsync(user);
            if (signInSuccess && !string.IsNullOrEmpty(token))
            {
                userHelper._client.SetAuthToken(token);
                await userHelper.DeleteAccountAsync(user.Password);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            userHelper._client.SetAuthToken(null);
        }
    }
}