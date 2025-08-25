using Kanriya.Tests.Helpers;
using Spectre.Console;

namespace Kanriya.Tests.Tests;

/// <summary>
/// Comprehensive user authorization test scenario
/// </summary>
public static class UserAuthorizationTest
{
    public static async Task<(int passed, int failed)> RunAsync(
        UserTestHelper userHelper, 
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        int passed = 0;
        int failed = 0;
        
        AnsiConsole.Write(new Rule("[cyan]User Authorization Tests[/]"));
        AnsiConsole.WriteLine();
        
        var user = TestUser.Generate("auth_test");
        
        // Test 1: Sign Up
        AnsiConsole.MarkupLine("[yellow]📝 Step 1: Sign Up New User...[/]");
        var (signUpSuccess, verificationToken) = await userHelper.SignUpAsync(user);
        if (signUpSuccess)
        {
            passed++;
            
            // Verify user is in pending_users table
            if (await dbHelper.UserIsPendingAsync(user.Email))
                passed++;
            else
                failed++;
        }
        else
        {
            failed++;
        }
        
        // Test 2: Resend Verification
        AnsiConsole.MarkupLine("[yellow]📧 Step 2: Resend Verification Email...[/]");
        var (resendSuccess, newToken) = await userHelper.ResendVerificationAsync(user.Email);
        if (resendSuccess)
        {
            passed++;
            if (!string.IsNullOrEmpty(newToken))
            {
                user.VerificationToken = newToken; // Use the new token
                AnsiConsole.MarkupLine($"[green dim]  New verification token received[/]");
            }
        }
        else
        {
            failed++;
        }
        
        // Test 3: Verify Email via HTTP endpoint
        AnsiConsole.MarkupLine("[yellow]✉️ Step 3: Verify Email via HTTP Endpoint...[/]");
        if (!string.IsNullOrEmpty(user.VerificationToken))
        {
            var verifySuccess = await userHelper.VerifyEmailAsync(user.VerificationToken);
            if (verifySuccess)
            {
                passed++;
                
                // Verify user moved to users table
                if (await dbHelper.UserIsActiveAsync(user.Email))
                    passed++;
                else
                    failed++;
                    
                // Verify user removed from pending_users
                if (!await dbHelper.UserIsPendingAsync(user.Email))
                {
                    AnsiConsole.MarkupLine($"[green dim]  ✓ User removed from pending_users[/]");
                    passed++;
                }
                else
                {
                    failed++;
                }
            }
            else
            {
                failed++;
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]  ❌ No verification token available[/]");
            failed++;
        }
        
        // Test 4: Request Password Reset (TODO: Implement endpoint first)
        AnsiConsole.MarkupLine("[yellow]🔑 Step 4: Request Password Reset...[/]");
        AnsiConsole.MarkupLine("[dim]  ⏳ TODO: Implement requestPasswordReset endpoint[/]");
        
        // Test 5: Sign In
        AnsiConsole.MarkupLine("[yellow]🔐 Step 5: Sign In...[/]");
        var (signInSuccess, token) = await userHelper.SignInAsync(user);
        if (signInSuccess && !string.IsNullOrEmpty(token))
        {
            passed++;
            client.SetAuthToken(token);
        }
        else
        {
            failed++;
        }
        
        // Test 6: Get Profile (requires auth)
        AnsiConsole.MarkupLine("[yellow]👤 Step 6: Get User Profile...[/]");
        var getProfileSuccess = await userHelper.GetMeAsync();
        if (getProfileSuccess)
            passed++;
        else
            failed++;
        
        // Test 7: Change Password
        AnsiConsole.MarkupLine("[yellow]🔐 Step 7: Change Password...[/]");
        var newPassword = "NewPassword456!";
        var changePasswordSuccess = await userHelper.ChangePasswordAsync(user.Password, newPassword);
        if (changePasswordSuccess)
        {
            passed++;
            user.Password = newPassword; // Update for later use
        }
        else
        {
            failed++;
        }
        
        // Test 8: Logout (TODO: Implement logout mutation)
        AnsiConsole.MarkupLine("[yellow]🚪 Step 8: Logout...[/]");
        AnsiConsole.MarkupLine("[dim]  ⏳ TODO: Implement logout mutation[/]");
        client.SetAuthToken(null); // Clear token for now
        
        // Test 9: Sign In with New Password
        AnsiConsole.MarkupLine("[yellow]🔐 Step 9: Sign In with New Password...[/]");
        var (signInNewSuccess, newAuthToken) = await userHelper.SignInAsync(user);
        if (signInNewSuccess && !string.IsNullOrEmpty(newAuthToken))
        {
            passed++;
            client.SetAuthToken(newAuthToken);
        }
        else
        {
            failed++;
        }
        
        // Test 10: Delete Account (cleanup)
        AnsiConsole.MarkupLine("[yellow]🗑️ Step 10: Delete Account...[/]");
        var deleteSuccess = await userHelper.DeleteAccountAsync(user.Password);
        if (deleteSuccess)
        {
            passed++;
            
            // Verify user removed from database
            if (!await dbHelper.UserIsActiveAsync(user.Email))
            {
                passed++;
                AnsiConsole.MarkupLine($"[green dim]  ✓ User removed from database[/]");
            }
            else
            {
                failed++;
            }
        }
        else
        {
            failed++;
        }
        
        client.SetAuthToken(null);
        AnsiConsole.WriteLine();
        
        return (passed, failed);
    }
}