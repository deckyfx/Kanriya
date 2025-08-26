using Kanriya.Tests.Helpers;
using Kanriya.Tests.Actions;
using Spectre.Console;

namespace Kanriya.Tests.Suites;

/// <summary>
/// Authentication Test Suite with improved reporting
/// </summary>
public static class AuthTestSuite
{
    private static int _positivePass = 0;
    private static int _positiveFail = 0;
    private static int _negativePass = 0;
    private static int _negativeFail = 0;

    public static async Task<(int passed, int failed)> RunAsync(
        UserTestHelper userHelper,
        DatabaseHelper dbHelper,
        GraphQLClient client)
    {
        // Reset counters
        _positivePass = 0;
        _positiveFail = 0;
        _negativePass = 0;
        _negativeFail = 0;

        AnsiConsole.Write(new Panel(new FigletText("Auth Suite")
            .Color(Color.Aqua))
            .Header("[bold cyan]Authentication Test Suite - Enhanced Reporting[/]")
            .Border(BoxBorder.Double));
        AnsiConsole.WriteLine();

        // Run all stages - passing test users between stages
        var (validUser, validToken, dupUser, dupToken) = await RunSignUpStage(userHelper, dbHelper);
        var (verifiedValidUser, verifiedDupUser) = await RunEmailVerificationStage(userHelper, dbHelper, validUser, validToken, dupUser, dupToken);
        await RunSignInStage(userHelper, dbHelper, verifiedValidUser, verifiedDupUser);
        await RunPasswordManagementStage(userHelper, dbHelper, client, verifiedValidUser);
        await RunAccountDeletionStage(userHelper, dbHelper, client, verifiedValidUser, verifiedDupUser);

        // Show summary
        TestReporter.ShowSummary(_positivePass, _positiveFail, _negativePass, _negativeFail);

        var totalPassed = _positivePass + _negativePass;
        var totalFailed = _positiveFail + _negativeFail;
        return (totalPassed, totalFailed);
    }

    private static async Task<(TestUser? validUser, string? validToken, TestUser? dupUser, string? dupToken)> RunSignUpStage(UserTestHelper userHelper, DatabaseHelper dbHelper)
    {
        TestReporter.StartStage("Stage 1: Sign Up");

        // Scenario 1.1: Invalid email (negative test - should reject)
        TestReporter.StartScenario("1.1", "Invalid email format", 
            "Expecting: System should reject malformed email");
        
        var invalidEmailUser = new TestUser
        {
            Email = "not-an-email",
            Password = "ValidPassword123!"
        };
        var (success1, _) = await userHelper.SignUpAsync(invalidEmailUser);
        TestReporter.ReportNegativeTest("Sign up with 'not-an-email'", !success1, "Invalid email format");
        if (!success1) _negativePass++; else _negativeFail++;

        // Scenario 1.2: Weak password (negative test - should reject)
        TestReporter.StartScenario("1.2", "Weak password",
            "Expecting: System should reject password 'weak'");
        
        var weakPasswordUser = new TestUser
        {
            Email = $"test_{Guid.NewGuid():N}@test.com",
            Password = "weak"
        };
        var (success2, _) = await userHelper.SignUpAsync(weakPasswordUser);
        TestReporter.ReportNegativeTest("Sign up with password 'weak'", !success2, "Password too weak");
        if (!success2) _negativePass++; else _negativeFail++;

        // Scenario 1.3: Valid credentials (positive test - should succeed)
        TestReporter.StartScenario("1.3", "Valid credentials",
            "Expecting: System should accept valid email and strong password");
        
        var validUser = TestUser.Generate("signup_test");
        var (success3, token) = await userHelper.SignUpAsync(validUser);
        TestReporter.ReportPositiveTest("Sign up with valid credentials", success3, 
            success3 ? $"User created: {validUser.Email}" : "Failed to create user");
        if (success3) _positivePass++; else _positiveFail++;

        if (success3)
        {
            // Verify in database (emails are stored in lowercase)
            var inPending = await dbHelper.UserIsPendingAsync(validUser.Email.ToLower());
            TestReporter.ReportPositiveTest("User added to pending_users", inPending);
            if (inPending) _positivePass++; else _positiveFail++;
            
            // Store token for next stage
            validUser.VerificationToken = token;
        }

        // Scenario 1.4: Duplicate email (negative test - should reject)
        TestReporter.StartScenario("1.4", "Duplicate email",
            "Expecting: System should reject duplicate email registration");
        
        var user1 = TestUser.Generate("dup_test");
        var (firstSignUp, token1) = await userHelper.SignUpAsync(user1);
        if (firstSignUp)
        {
            TestReporter.ReportInfo($"First registration: {user1.Email}");
            
            var (duplicateSignUp, _) = await userHelper.SignUpAsync(user1);
            TestReporter.ReportNegativeTest("Duplicate registration attempt", !duplicateSignUp, 
                "Email already registered");
            if (!duplicateSignUp) _negativePass++; else _negativeFail++;
            
            // Store token for next stage
            user1.VerificationToken = token1;
        }
        
        // Return users for next stage
        return (success3 ? validUser : null, success3 ? token : null, 
                firstSignUp ? user1 : null, firstSignUp ? token1 : null);
    }

    private static async Task<(TestUser? verifiedValidUser, TestUser? verifiedDupUser)> RunEmailVerificationStage(
        UserTestHelper userHelper, 
        DatabaseHelper dbHelper,
        TestUser? validUser,
        string? validToken,
        TestUser? dupUser,
        string? dupToken)
    {
        TestReporter.StartStage("Stage 2: Email Verification");

        // Use the valid user from Stage 1
        if (validUser == null || string.IsNullOrEmpty(validToken))
        {
            TestReporter.ReportInfo("No valid user from Stage 1, skipping verification tests");
            return (null, null);
        }

        TestReporter.ReportInfo($"Using test user from Stage 1: {validUser.Email}");
        var testUser = validUser;
        var verificationToken = validToken;

        // Scenario 2.1: Invalid token (negative test)
        TestReporter.StartScenario("2.1", "Invalid verification token",
            "Expecting: System should reject invalid token");
        
        var invalidVerify = await userHelper.VerifyEmailAsync("invalid-token-12345");
        TestReporter.ReportNegativeTest("Verify with invalid token", !invalidVerify, "Invalid token");
        if (!invalidVerify) _negativePass++; else _negativeFail++;

        // Scenario 2.2: Resend verification (positive test)
        TestReporter.StartScenario("2.2", "Resend verification email",
            "Expecting: System should resend verification email");
        
        var (resendSuccess, newToken) = await userHelper.ResendVerificationAsync(testUser.Email);
        TestReporter.ReportPositiveTest("Resend verification email", resendSuccess,
            resendSuccess ? "New token received" : "Failed to resend");
        if (resendSuccess) _positivePass++; else _positiveFail++;
        
        if (resendSuccess && !string.IsNullOrEmpty(newToken))
            verificationToken = newToken;

        // Scenario 2.3: Valid token (positive test)
        TestReporter.StartScenario("2.3", "Valid verification token",
            "Expecting: System should verify email and activate user");
        
        var verifySuccess = await userHelper.VerifyEmailAsync(verificationToken);
        TestReporter.ReportPositiveTest("Verify with valid token", verifySuccess);
        if (verifySuccess) _positivePass++; else _positiveFail++;

        if (verifySuccess)
        {
            var isActive = await dbHelper.UserIsActiveAsync(testUser.Email.ToLower());
            TestReporter.ReportPositiveTest("User moved to active users", isActive);
            if (isActive) _positivePass++; else _positiveFail++;

            var notPending = !await dbHelper.UserIsPendingAsync(testUser.Email.ToLower());
            TestReporter.ReportPositiveTest("User removed from pending", notPending);
            if (notPending) _positivePass++; else _positiveFail++;
        }

        // Scenario 2.4: Re-verify (negative test)
        TestReporter.StartScenario("2.4", "Re-verify already verified email",
            "Expecting: System should reject re-verification");
        
        var reverify = await userHelper.VerifyEmailAsync(verificationToken);
        TestReporter.ReportNegativeTest("Re-verify already verified email", !reverify, 
            "Already verified");
        if (!reverify) _negativePass++; else _negativeFail++;

        // Test duplicate user verification if available
        TestUser? verifiedDupUser = null;
        if (dupUser != null && !string.IsNullOrEmpty(dupToken))
        {
            TestReporter.StartScenario("2.5", "Verify duplicate user",
                "Expecting: Duplicate user should also be verifiable");
            
            var dupVerifySuccess = await userHelper.VerifyEmailAsync(dupToken);
            TestReporter.ReportPositiveTest($"Verify duplicate user {dupUser.Email}", dupVerifySuccess);
            if (dupVerifySuccess) 
            {
                _positivePass++;
                verifiedDupUser = dupUser;
            }
            else 
            {
                _positiveFail++;
            }
        }
        
        // Return verified users for next stages
        return (testUser, verifiedDupUser);
    }

    private static async Task RunSignInStage(
        UserTestHelper userHelper, 
        DatabaseHelper dbHelper,
        TestUser? verifiedValidUser,
        TestUser? verifiedDupUser)
    {
        TestReporter.StartStage("Stage 3: Sign In");

        // Use verified user from previous stages
        if (verifiedValidUser == null)
        {
            TestReporter.ReportInfo("No verified user from previous stages, skipping sign-in tests");
            return;
        }

        TestReporter.ReportInfo($"Using verified user from previous stages: {verifiedValidUser.Email}");
        var testUser = verifiedValidUser;
        userHelper._client.SetAuthToken(null); // Clear for testing

        // Scenario 3.1: Wrong password (negative test)
        TestReporter.StartScenario("3.1", "Wrong password",
            "Expecting: System should reject incorrect password");
        
        var wrongPassUser = new TestUser
        {
            Email = testUser.Email,
            Password = "WrongPassword123!"
        };
        var (wrongPassSuccess, _) = await userHelper.SignInAsync(wrongPassUser);
        TestReporter.ReportNegativeTest("Sign in with wrong password", !wrongPassSuccess,
            "Invalid credentials");
        if (!wrongPassSuccess) _negativePass++; else _negativeFail++;

        // Scenario 3.2: Non-existent email (negative test)
        TestReporter.StartScenario("3.2", "Non-existent email",
            "Expecting: System should reject non-existent email");
        
        var nonExistentUser = new TestUser
        {
            Email = "nonexistent@test.com",
            Password = "Password123!"
        };
        var (nonExistentSuccess, _) = await userHelper.SignInAsync(nonExistentUser);
        TestReporter.ReportNegativeTest("Sign in with non-existent email", !nonExistentSuccess,
            "User not found");
        if (!nonExistentSuccess) _negativePass++; else _negativeFail++;

        // Scenario 3.3: Valid credentials (positive test)
        TestReporter.StartScenario("3.3", "Valid credentials",
            "Expecting: System should authenticate user and return JWT token");
        
        var (signInSuccess, token) = await userHelper.SignInAsync(testUser);
        TestReporter.ReportPositiveTest("Sign in with correct credentials", signInSuccess,
            signInSuccess ? "JWT token received" : "Failed to sign in");
        if (signInSuccess) _positivePass++; else _positiveFail++;

        if (signInSuccess && !string.IsNullOrEmpty(token))
        {
            userHelper._client.SetAuthToken(token);
            var canAccessProfile = await userHelper.GetMeAsync();
            TestReporter.ReportPositiveTest("Access protected endpoint with token", canAccessProfile);
            if (canAccessProfile) _positivePass++; else _positiveFail++;
        }
    }

    private static async Task RunPasswordManagementStage(
        UserTestHelper userHelper, 
        DatabaseHelper dbHelper,
        GraphQLClient client,
        TestUser? verifiedUser)
    {
        TestReporter.StartStage("Stage 4: Password Management");

        // Use verified user from previous stages
        if (verifiedUser == null)
        {
            TestReporter.ReportInfo("No verified user from previous stages, skipping password tests");
            return;
        }

        TestReporter.ReportInfo($"Using verified user: {verifiedUser.Email}");
        var testUser = verifiedUser;
        
        // Sign in first to get auth token
        var (signInSuccess, token) = await userHelper.SignInAsync(testUser);
        if (!signInSuccess || string.IsNullOrEmpty(token))
        {
            TestReporter.ReportInfo("Failed to sign in for password tests");
            return;
        }
        client.SetAuthToken(token);

        // Scenario 4.1: Wrong current password for change (negative test)
        TestReporter.StartScenario("4.1", "Change password with wrong current password",
            "Expecting: System should reject incorrect current password");
        
        var wrongCurrentSuccess = await userHelper.ChangePasswordAsync("WrongPassword123!", "NewPassword456!");
        TestReporter.ReportNegativeTest("Change with wrong current password", !wrongCurrentSuccess,
            "Current password incorrect");
        if (!wrongCurrentSuccess) _negativePass++; else _negativeFail++;

        // Scenario 4.2: Request password reset (positive test)
        TestReporter.StartScenario("4.2", "Request password reset",
            "Expecting: System should send reset token for valid email");
        
        var (resetRequestSuccess, resetToken) = await userHelper.RequestPasswordResetAsync(testUser.Email);
        TestReporter.ReportPositiveTest("Request password reset", resetRequestSuccess,
            resetRequestSuccess ? "Reset token received" : "Failed to request reset");
        if (resetRequestSuccess) _positivePass++; else _positiveFail++;

        // Scenario 4.3: Reset with invalid token (negative test)
        TestReporter.StartScenario("4.3", "Reset password with invalid token",
            "Expecting: System should reject invalid reset token");
        
        var invalidResetSuccess = await userHelper.ResetPasswordAsync("invalid-token-12345", "NewPassword789!");
        TestReporter.ReportNegativeTest("Reset with invalid token", !invalidResetSuccess,
            "Invalid or expired token");
        if (!invalidResetSuccess) _negativePass++; else _negativeFail++;

        // Scenario 4.4: Reset with valid token (positive test)
        TestReporter.StartScenario("4.4", "Reset password with valid token",
            "Expecting: System should accept valid reset token and new password");
        
        var resetNewPassword = "ResetPassword789!";
        if (resetRequestSuccess && !string.IsNullOrEmpty(resetToken))
        {
            var resetSuccess = await userHelper.ResetPasswordAsync(resetToken, resetNewPassword);
            TestReporter.ReportPositiveTest("Reset password with valid token", resetSuccess);
            if (resetSuccess) 
            {
                _positivePass++;
                testUser.Password = resetNewPassword; // Update password for next scenario
            }
            else 
            {
                _positiveFail++;
            }
        }
        else
        {
            TestReporter.ReportInfo("Skipping reset test - no valid token");
        }

        // Scenario 4.5: Sign in with reset password (positive test)
        TestReporter.StartScenario("4.5", "Sign in with reset password",
            "Expecting: User should be able to sign in with the new password from reset");
        
        client.SetAuthToken(null); // Clear any existing token
        var (resetSignInSuccess, resetSignInToken) = await userHelper.SignInAsync(testUser);
        TestReporter.ReportPositiveTest("Sign in with reset password", resetSignInSuccess);
        if (resetSignInSuccess) 
        {
            _positivePass++;
            client.SetAuthToken(resetSignInToken);
        }
        else 
        {
            _positiveFail++;
            // Try to re-authenticate with original password if reset failed
            TestReporter.ReportInfo("Attempting to sign in with original password");
            testUser.Password = verifiedUser.Password; // Restore original password
            var (reAuthSuccess, reAuthToken) = await userHelper.SignInAsync(testUser);
            if (reAuthSuccess && !string.IsNullOrEmpty(reAuthToken))
            {
                client.SetAuthToken(reAuthToken);
            }
        }

        // Scenario 4.6: Change to weak password (negative test)
        TestReporter.StartScenario("4.6", "Change to weak password",
            "Expecting: System should reject weak new password (requires authentication)");
        
        var weakNewSuccess = await userHelper.ChangePasswordAsync(testUser.Password, "weak");
        TestReporter.ReportNegativeTest("Change to weak password 'weak'", !weakNewSuccess,
            "New password too weak");
        if (!weakNewSuccess) _negativePass++; else _negativeFail++;

        // Scenario 4.7: Valid password change (positive test)
        TestReporter.StartScenario("4.7", "Valid password change",
            "Expecting: System should accept strong new password (requires authentication)");
        
        var newPassword = "ChangedPassword123!";
        var changeSuccess = await userHelper.ChangePasswordAsync(testUser.Password, newPassword);
        TestReporter.ReportPositiveTest("Change to strong password", changeSuccess);
        if (changeSuccess) _positivePass++; else _positiveFail++;

        if (changeSuccess)
        {
            client.SetAuthToken(null);
            testUser.Password = newPassword;
            var (newSignInSuccess, newToken) = await userHelper.SignInAsync(testUser);
            TestReporter.ReportPositiveTest("Sign in with changed password", newSignInSuccess);
            if (newSignInSuccess) 
            {
                _positivePass++;
                client.SetAuthToken(newToken);
            }
            else 
            {
                _positiveFail++;
            }
        }
    }

    private static async Task RunAccountDeletionStage(
        UserTestHelper userHelper,
        DatabaseHelper dbHelper,
        GraphQLClient client,
        TestUser? verifiedValidUser,
        TestUser? verifiedDupUser)
    {
        TestReporter.StartStage("Stage 5: Account Deletion");

        // Use verified user from previous stages
        if (verifiedValidUser == null)
        {
            TestReporter.ReportInfo("No verified user from previous stages, skipping deletion tests");
            return;
        }

        TestReporter.ReportInfo($"Using verified user: {verifiedValidUser.Email}");
        var testUser = verifiedValidUser;
        
        // Sign in first to get auth token
        var (signInSuccess, token) = await userHelper.SignInAsync(testUser);
        if (!signInSuccess || string.IsNullOrEmpty(token))
        {
            TestReporter.ReportInfo("Failed to sign in for deletion tests");
            return;
        }
        client.SetAuthToken(token);

        // Scenario 5.1: Wrong password (negative test)
        TestReporter.StartScenario("5.1", "Delete with wrong password",
            "Expecting: System should reject deletion with wrong password");
        
        var wrongPasswordSuccess = await userHelper.DeleteAccountAsync("WrongPassword123!");
        TestReporter.ReportNegativeTest("Delete with wrong password", !wrongPasswordSuccess,
            "Invalid password");
        if (!wrongPasswordSuccess) _negativePass++; else _negativeFail++;

        // Scenario 5.2: Valid deletion (positive test)
        TestReporter.StartScenario("5.2", "Delete with correct password",
            "Expecting: System should delete account and prevent future login");
        
        var deleteSuccess = await userHelper.DeleteAccountAsync(testUser.Password);
        TestReporter.ReportPositiveTest("Delete account", deleteSuccess);
        if (deleteSuccess) _positivePass++; else _positiveFail++;

        if (deleteSuccess)
        {
            var notInDb = !await dbHelper.UserIsActiveAsync(testUser.Email.ToLower());
            TestReporter.ReportPositiveTest("User removed from database", notInDb);
            if (notInDb) _positivePass++; else _positiveFail++;

            client.SetAuthToken(null);
            var (cantSignIn, _) = await userHelper.SignInAsync(testUser);
            TestReporter.ReportPositiveTest("Cannot sign in after deletion", !cantSignIn,
                !cantSignIn ? "Login correctly blocked" : "ERROR: Can still sign in!");
            if (!cantSignIn) _positivePass++; else _positiveFail++;
        }
        
        // Cleanup duplicate user if it exists
        if (verifiedDupUser != null)
        {
            TestReporter.ReportInfo($"Cleaning up duplicate user: {verifiedDupUser.Email}");
            await AuthActions.CleanupUser(userHelper, verifiedDupUser);
        }
    }
}