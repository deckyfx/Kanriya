using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;
using Kanriya.Server.Types;
using Kanriya.Server.Types.Inputs;
using Kanriya.Server.Types.Outputs;
using HotChocolate.AspNetCore;
using HotChocolate.Authorization;

namespace Kanriya.Server.Mutations;

/// <summary>
/// GraphQL mutations for authentication operations
/// </summary>
[ExtendObjectType(typeof(RootMutation))]
public class AuthMutations
{
    /// <summary>
    /// Sign up a new user
    /// Creates a pending user that requires email verification
    /// </summary>
    public async Task<AuthOutput> SignUp(
        SignUpInput input,
        [Service] IUserService userService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.SignUpAsync(
            input.Email,
            input.Password,
            cancellationToken);
        
        return new AuthOutput
        {
            Success = result.Success,
            Message = result.Message,
            // In development, return the verification token for testing
            // In production, this should be sent via email
            #if DEBUG
            VerificationToken = result.VerificationToken
            #endif
        };
    }
    
    /// <summary>
    /// Verify email address with token
    /// Moves user from pending to active
    /// Note: Does not return JWT token - user must sign in after activation
    /// </summary>
    public async Task<AuthOutput> VerifyEmail(
        string verificationToken,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.VerifyEmailAsync(verificationToken, cancellationToken);
        
        // Don't generate token here - user must sign in after email verification
        
        return new AuthOutput
        {
            Success = result.Success,
            Message = result.Success 
                ? "Email verified successfully. Please sign in to get your access token."
                : result.Message,
            User = result.User,
            Token = null // No token on verification, only on signin
        };
    }
    
    /// <summary>
    /// Sign in an existing user
    /// Returns JWT token on success
    /// </summary>
    public async Task<AuthOutput> SignIn(
        SignInInput input,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.SignInAsync(
            input.Email,
            input.Password,
            input.BrandId,
            cancellationToken);
        
        return new AuthOutput
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User,
            Token = result.Token
        };
    }
    
    /// <summary>
    /// Resend verification email
    /// Generates new token for pending users
    /// </summary>
    public async Task<AuthOutput> ResendVerification(
        string email,
        [Service] IUserService userService,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.ResendVerificationAsync(email, cancellationToken);
        
        return new AuthOutput
        {
            Success = result.Success,
            Message = result.Message,
            #if DEBUG
            VerificationToken = result.NewToken
            #endif
        };
    }
    
    /// <summary>
    /// Change user password (requires authentication)
    /// </summary>
    [Authorize]
    public async Task<AuthOutput> ChangePassword(
        string currentPassword,
        string newPassword,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new AuthOutput
            {
                Success = false,
                Message = "User not authenticated"
            };
        }
        
        var result = await userService.ChangePasswordAsync(
            currentUser.User.Id,
            currentPassword,
            newPassword,
            cancellationToken);
        
        return new AuthOutput
        {
            Success = result.Success,
            Message = result.Message
        };
    }
    
    /// <summary>
    /// Update user profile (requires authentication)
    /// </summary>
    [Authorize]
    public async Task<AuthOutput> UpdateProfile(
        string? fullName,
        string? profilePictureUrl,
        [Service] IUserService userService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new AuthOutput
            {
                Success = false,
                Message = "User not authenticated"
            };
        }
        
        var user = await userService.UpdateProfileAsync(
            currentUser.User.Id,
            fullName,
            profilePictureUrl,
            cancellationToken);
        
        return new AuthOutput
        {
            Success = user != null,
            Message = user != null ? "Profile updated successfully" : "Failed to update profile",
            User = user
        };
    }
}