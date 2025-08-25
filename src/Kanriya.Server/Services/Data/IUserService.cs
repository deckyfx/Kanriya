using Kanriya.Server.Data;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Service interface for user authentication and management operations
/// </summary>
public interface IUserService
{
    // ==================== AUTHENTICATION ====================
    
    /// <summary>
    /// Register a new user (creates pending user)
    /// </summary>
    Task<(bool Success, string Message, string? VerificationToken)> SignUpAsync(
        string email, 
        string password,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verify email and activate user account
    /// </summary>
    Task<(bool Success, string Message, User? User)> VerifyEmailAsync(
        string verificationToken,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sign in an existing user (principal or brand)
    /// </summary>
    Task<(bool Success, string Message, User? User, string? Token, string? TokenType)> SignInAsync(
        string emailOrApiSecret,
        string password,
        string? brandId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resend verification email
    /// </summary>
    Task<(bool Success, string Message, string? NewToken)> ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default);
    
    // ==================== USER MANAGEMENT ====================
    
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update user profile
    /// </summary>
    Task<User?> UpdateProfileAsync(
        string userId,
        string? fullName = null,
        string? profilePictureUrl = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Change user password (requires current password)
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Request password reset (sends reset token via email)
    /// </summary>
    Task<(bool Success, string Message, string? ResetToken)> RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reset password using reset token (for forgotten passwords)
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all users (admin only)
    /// </summary>
    Task<IEnumerable<User>> GetAllUsersAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get pending users (admin only)
    /// </summary>
    Task<IEnumerable<PendingUser>> GetPendingUsersAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant a role to a user
    /// </summary>
    Task<(bool Success, string Message, User? User)> GrantRoleAsync(
        string userId,
        string role,
        string? grantedBy = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke a role from a user
    /// </summary>
    Task<(bool Success, string Message, User? User)> RevokeRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete user account
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Force verify a pending user
    /// </summary>
    Task<(bool Success, string Message, User? User)> ForceVerifyUserAsync(
        string pendingUserId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deactivate user account (soft delete)
    /// </summary>
    Task<bool> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up expired pending users
    /// </summary>
    Task<int> CleanupExpiredPendingUsersAsync(CancellationToken cancellationToken = default);
    
    // ==================== TOKEN MANAGEMENT ====================
    
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    string GenerateJwtToken(User user);
    
    /// <summary>
    /// Validate password strength
    /// </summary>
    bool IsPasswordValid(string password);
    
    /// <summary>
    /// Hash password
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify password hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}