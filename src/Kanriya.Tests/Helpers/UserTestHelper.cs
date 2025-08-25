using System.Text.Json;
using Spectre.Console;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// Helper for user-related test operations
/// </summary>
public class UserTestHelper
{
    public readonly GraphQLClient _client;
    private readonly string _baseUrl;
    
    public UserTestHelper(GraphQLClient client, string baseUrl)
    {
        _client = client;
        _baseUrl = baseUrl;
    }
    
    /// <summary>
    /// Sign up a new user
    /// </summary>
    public async Task<(bool success, string? verificationToken)> SignUpAsync(TestUser user)
    {
        var mutation = @"
            mutation SignUp($input: SignUpInput!) {
                signUp(input: $input) {
                    success
                    message
                    verificationToken
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new
        {
            input = new
            {
                email = user.Email,
                password = user.Password
            }
        });
        
        var success = GraphQLClient.GetSuccess(result, "data", "signUp");
        var token = GraphQLClient.GetString(result, "data", "signUp", "verificationToken");
        
        if (success)
        {
            user.VerificationToken = token;
            LogSuccess($"User created: {user.Email}");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "signUp");
            LogError($"Sign up failed: {message}");
        }
        
        return (success, token);
    }
    
    /// <summary>
    /// Resend verification email
    /// </summary>
    public async Task<(bool success, string? newToken)> ResendVerificationAsync(string email)
    {
        var mutation = @"
            mutation ResendVerification($email: String!) {
                resendVerification(email: $email) {
                    success
                    message
                    verificationToken
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new { email });
        
        var success = GraphQLClient.GetSuccess(result, "data", "resendVerification");
        var token = GraphQLClient.GetString(result, "data", "resendVerification", "verificationToken");
        
        if (success)
        {
            LogSuccess($"Verification email resent for: {email}");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "resendVerification");
            LogError($"Resend verification failed: {message}");
        }
        
        return (success, token);
    }
    
    /// <summary>
    /// Verify email using HTTP endpoint
    /// </summary>
    public async Task<bool> VerifyEmailAsync(string verificationToken)
    {
        var verifyUrl = $"{_baseUrl}/verify-email?token={verificationToken}";
        var response = await _client.GetAsync(verifyUrl);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var success = content.Contains("successfully verified") || 
                         content.Contains("Email verified");
            
            if (success)
            {
                LogSuccess("Email verified via HTTP endpoint");
            }
            else
            {
                LogError("Email verification failed");
            }
            
            return success;
        }
        
        LogError($"HTTP verification failed: {response.StatusCode}");
        return false;
    }
    
    /// <summary>
    /// Sign in a user
    /// </summary>
    public async Task<(bool success, string? token)> SignInAsync(TestUser user, string? brandId = null)
    {
        var mutation = @"
            mutation SignIn($input: SignInInput!) {
                signIn(input: $input) {
                    success
                    message
                    token
                    user {
                        id
                        email
                    }
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new
        {
            input = new
            {
                email = user.Email,
                password = user.Password,
                brandId = brandId
            }
        });
        
        var success = GraphQLClient.GetSuccess(result, "data", "signIn");
        var token = GraphQLClient.GetString(result, "data", "signIn", "token");
        
        if (success)
        {
            user.Token = token;
            user.Id = GraphQLClient.GetString(result, "data", "signIn", "user", "id");
            LogSuccess("Sign in successful");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "signIn");
            LogError($"Sign in failed: {message}");
        }
        
        return (success, token);
    }
    
    /// <summary>
    /// Get current user profile
    /// </summary>
    public async Task<bool> GetMeAsync()
    {
        var query = @"
            query GetMe {
                me {
                    id
                    email
                    fullName
                }
            }";
            
        var result = await _client.ExecuteAsync(query);
        
        if (result != null && 
            result.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("me", out var me) &&
            me.ValueKind != JsonValueKind.Null)
        {
            var email = GraphQLClient.GetString(result, "data", "me", "email");
            LogSuccess($"Profile retrieved: {email}");
            return true;
        }
        
        LogError("Failed to get user profile");
        return false;
    }
    
    /// <summary>
    /// Change password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var mutation = @"
            mutation ChangePassword($currentPassword: String!, $newPassword: String!) {
                changePassword(
                    currentPassword: $currentPassword,
                    newPassword: $newPassword
                ) {
                    success
                    message
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new
        {
            currentPassword,
            newPassword
        });
        
        var success = GraphQLClient.GetSuccess(result, "data", "changePassword");
        
        if (success)
        {
            LogSuccess("Password changed successfully");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "changePassword");
            LogError($"Password change failed: {message}");
        }
        
        return success;
    }
    
    /// <summary>
    /// Delete account
    /// </summary>
    public async Task<bool> DeleteAccountAsync(string password)
    {
        var mutation = @"
            mutation DeleteMyAccount($input: DeleteMyAccountInput!) {
                deleteMyAccount(input: $input) {
                    success
                    message
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new
        {
            input = new { password }
        });
        
        var success = GraphQLClient.GetSuccess(result, "data", "deleteMyAccount");
        
        if (success)
        {
            LogSuccess("Account deleted successfully");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "deleteMyAccount");
            LogError($"Account deletion failed: {message}");
        }
        
        return success;
    }
    
    /// <summary>
    /// Request a password reset
    /// </summary>
    public async Task<(bool success, string? resetToken)> RequestPasswordResetAsync(string email)
    {
        var mutation = @"
            mutation RequestPasswordReset($email: String!) {
                requestPasswordReset(email: $email) {
                    success
                    message
                    token
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new { email });
        
        var success = GraphQLClient.GetSuccess(result, "data", "requestPasswordReset");
        var resetToken = GraphQLClient.GetString(result, "data", "requestPasswordReset", "token");
        
        if (success)
        {
            LogSuccess($"Password reset requested for {email}");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "requestPasswordReset");
            LogError($"Password reset request failed: {message}");
        }
        
        return (success, resetToken);
    }
    
    /// <summary>
    /// Reset password using token
    /// </summary>
    public async Task<bool> ResetPasswordAsync(string resetToken, string newPassword)
    {
        var mutation = @"
            mutation ResetPassword($resetToken: String!, $newPassword: String!) {
                resetPassword(resetToken: $resetToken, newPassword: $newPassword) {
                    success
                    message
                }
            }";
            
        var result = await _client.ExecuteAsync(mutation, new { resetToken, newPassword });
        
        var success = GraphQLClient.GetSuccess(result, "data", "resetPassword");
        
        if (success)
        {
            LogSuccess("Password reset successfully");
        }
        else
        {
            var message = GraphQLClient.GetMessage(result, "data", "resetPassword");
            LogError($"Password reset failed: {message}");
        }
        
        return success;
    }
    
    /// <summary>
    /// Create a user and sign them in (helper for other tests)
    /// </summary>
    public async Task<TestUser?> CreateAndSignInUserAsync(string prefix = "test")
    {
        var user = TestUser.Generate(prefix);
        
        // Sign up
        var (signUpSuccess, verificationToken) = await SignUpAsync(user);
        if (!signUpSuccess || string.IsNullOrEmpty(verificationToken))
            return null;
        
        // Verify email
        var verifySuccess = await VerifyEmailAsync(verificationToken);
        if (!verifySuccess)
            return null;
        
        // Sign in
        var (signInSuccess, token) = await SignInAsync(user);
        if (!signInSuccess || string.IsNullOrEmpty(token))
            return null;
        
        _client.SetAuthToken(token);
        return user;
    }
    
    private void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]  ✅ {message}[/]");
    }
    
    private void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]  ❌ {message}[/]");
    }
}