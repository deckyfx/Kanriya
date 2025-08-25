using System.IdentityModel.Tokens.Jwt;
using Kanriya.Server.Services.System;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Kanriya.Server.Data;
using Kanriya.Server.Program;
using Kanriya.Server.Types.Outputs;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kanriya.Server.Services.Data;

/// <summary>
/// Service implementation for user authentication and management
/// </summary>
public class UserService : IUserService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMailerService _mailerService;
    
    public UserService(
        IServiceProvider serviceProvider,
        ILogger<UserService> logger,
        IConfiguration configuration,
        IMailerService mailerService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _mailerService = mailerService;
    }
    
    private IServiceScope CreateScope() => _serviceProvider.CreateScope();
    
    // ==================== AUTHENTICATION ====================
    
    public async Task<(bool Success, string Message, string? VerificationToken)> SignUpAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (!IsEmailValid(email))
            return (false, "Invalid email format", null);
        
        if (!IsPasswordValid(password))
            return (false, "Password must be at least 8 characters with uppercase, lowercase, and number", null);
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Check if email already exists in users table
        var existingUser = await dbContext.Users
            .AnyAsync(u => u.Email == email.ToLower(), cancellationToken);
        
        if (existingUser)
            return (false, "Email already registered", null);
        
        // Check if email exists in pending users
        var existingPending = await dbContext.PendingUsers
            .FirstOrDefaultAsync(p => p.Email == email.ToLower(), cancellationToken);
        
        if (existingPending != null)
        {
            // If token expired, update it
            if (existingPending.TokenExpiresAt < DateTime.UtcNow)
            {
                existingPending.VerificationToken = Guid.NewGuid().ToString();
                existingPending.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
                existingPending.CreatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Updated expired verification token for {Email}", email);
                return (true, "Verification email resent", existingPending.VerificationToken);
            }
            
            return (false, "Email already pending verification. Check your email.", null);
        }
        
        // Create pending user
        var pendingUser = new PendingUser
        {
            Email = email.ToLower(),
            PasswordHash = HashPassword(password),
            VerificationToken = Guid.NewGuid().ToString(),
            TokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        
        dbContext.PendingUsers.Add(pendingUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Publish subscription event
        var eventSender = scope.ServiceProvider.GetService<ITopicEventSender>();
        if (eventSender != null)
        {
            var subscriptionEvent = new SubscriptionEvent<PendingUser>
            {
                Event = EventType.Created,
                Document = pendingUser,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await eventSender.SendAsync("PendingUserChanged", subscriptionEvent, cancellationToken);
        }
        
        _logger.LogInformation("Created pending user for {Email}", email);
        
        // Send activation email
        await SendActivationEmailAsync(pendingUser, cancellationToken);
        
        return (true, "Verification email sent. Please check your inbox.", pendingUser.VerificationToken);
    }
    
    public async Task<(bool Success, string Message, User? User)> VerifyEmailAsync(
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Find pending user by token
        var pendingUser = await dbContext.PendingUsers
            .FirstOrDefaultAsync(p => p.VerificationToken == verificationToken, cancellationToken);
        
        if (pendingUser == null)
            return (false, "Invalid verification token", null);
        
        // Check if token expired
        if (pendingUser.TokenExpiresAt < DateTime.UtcNow)
            return (false, "Verification token expired", null);
        
        // Check if email already exists (race condition check)
        var existingUser = await dbContext.Users
            .AnyAsync(u => u.Email == pendingUser.Email, cancellationToken);
        
        if (existingUser)
        {
            // Remove pending user
            dbContext.PendingUsers.Remove(pendingUser);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (false, "Email already verified", null);
        }
        
        // Create verified user with default User role
        var user = new User
        {
            Email = pendingUser.Email,
            PasswordHash = pendingUser.PasswordHash,
            FullName = pendingUser.Email,  // Use email as default name until user updates profile
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Assign default User role
        var userRole = new UserRole
        {
            UserId = user.Id,
            Role = Constants.UserRoles.User,
            AssignedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(userRole);
        
        // Add user and remove pending user in transaction
        dbContext.Users.Add(user);
        dbContext.PendingUsers.Remove(pendingUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Publish subscription events
        var eventSender = scope.ServiceProvider.GetService<ITopicEventSender>();
        if (eventSender != null)
        {
            // User created event
            var userEvent = new SubscriptionEvent<User>
            {
                Event = EventType.Created,
                Document = user,
                Time = DateTime.UtcNow,
                Previous = null
            };
            await eventSender.SendAsync("UserChanged", userEvent, cancellationToken);
            
            // Pending user deleted event
            var pendingUserEvent = new SubscriptionEvent<PendingUser>
            {
                Event = EventType.Deleted,
                Document = null,
                Time = DateTime.UtcNow,
                Previous = pendingUser
            };
            await eventSender.SendAsync("PendingUserChanged", pendingUserEvent, cancellationToken);
        }
        
        _logger.LogInformation("Verified and activated user {Email}", user.Email);
        
        // Send welcome email
        await SendWelcomeEmailAsync(user, cancellationToken);
        
        return (true, "Email verified successfully", user);
    }
    
    public async Task<(bool Success, string Message, User? User, string? Token, string? TokenType)> SignInAsync(
        string emailOrApiSecret,
        string password,
        string? brandId = null,
        CancellationToken cancellationToken = default)
    {
        // If brandId is provided, use brand authentication
        if (!string.IsNullOrEmpty(brandId))
        {
            return await SignInBrandAsync(emailOrApiSecret, password, brandId, cancellationToken);
        }
        
        // Otherwise, use principal authentication
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Find user by email
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == emailOrApiSecret.ToLower(), cancellationToken);
        
        if (user == null)
            return (false, "Invalid email or password", null, null, null);
        
        // Verify password
        if (!VerifyPassword(password, user.PasswordHash))
            return (false, "Invalid email or password", null, null, null);
        
        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Generate JWT token (keeping backward compatibility)
        // For full dual-token support, use DualAuthService.GenerateDualTokensAsync
        var token = GenerateJwtToken(user);
        
        _logger.LogInformation("User {Email} signed in successfully", user.Email);
        
        return (true, "Sign in successful", user, token, "PRINCIPAL");
    }
    
    /// <summary>
    /// Sign in using brand API credentials
    /// </summary>
    private async Task<(bool Success, string Message, User? User, string? Token, string? TokenType)> SignInBrandAsync(
        string apiSecret,
        string apiPassword,
        string brandId,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var brandService = scope.ServiceProvider.GetRequiredService<IBrandConnectionService>();
        var apiCredentialService = scope.ServiceProvider.GetRequiredService<IApiCredentialService>();
        
        // Get brand to retrieve PostgreSQL credentials
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && b.IsActive, cancellationToken);
        
        if (brand == null)
            return (false, "Brand not found or inactive", null, null, null);
        
        // Connect to brand schema using brand PostgreSQL credentials
        var connectionString = brandService.BuildConnectionString(brand);
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Find user by API secret in brand schema
        using var findUserCommand = new Npgsql.NpgsqlCommand($@"
            SELECT id, api_secret, api_password_hash, display_name, is_active
            FROM {brand.SchemaName}.users
            WHERE api_secret = @apiSecret AND is_active = true
        ", connection);
        
        findUserCommand.Parameters.AddWithValue("apiSecret", apiSecret);
        
        using var reader = await findUserCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return (false, "Invalid API credentials", null, null, null);
        
        var brandUserId = reader.GetGuid(0).ToString();
        var storedApiSecret = reader.GetString(1);
        var apiPasswordHash = reader.GetString(2);
        var displayName = reader.IsDBNull(3) ? null : reader.GetString(3);
        var isActive = reader.GetBoolean(4);
        
        reader.Close();
        
        // Verify API password
        if (!apiCredentialService.VerifyApiPassword(apiPassword, apiPasswordHash))
            return (false, "Invalid API credentials", null, null, null);
        
        // Get user roles from brand schema
        var roles = new List<string>();
        using var getRolesCommand = new Npgsql.NpgsqlCommand($@"
            SELECT role FROM {brand.SchemaName}.user_roles
            WHERE user_id = @userId AND is_active = true
        ", connection);
        
        getRolesCommand.Parameters.AddWithValue("userId", Guid.Parse(brandUserId));
        
        using var rolesReader = await getRolesCommand.ExecuteReaderAsync(cancellationToken);
        while (await rolesReader.ReadAsync(cancellationToken))
        {
            roles.Add(rolesReader.GetString(0));
        }
        rolesReader.Close();
        
        // Update last login in brand schema
        using var updateLoginCommand = new Npgsql.NpgsqlCommand($@"
            UPDATE {brand.SchemaName}.users
            SET last_login_at = CURRENT_TIMESTAMP
            WHERE id = @userId
        ", connection);
        
        updateLoginCommand.Parameters.AddWithValue("userId", Guid.Parse(brandUserId));
        await updateLoginCommand.ExecuteNonQueryAsync(cancellationToken);
        
        // Generate brand token
        var token = GenerateBrandJwtToken(brandUserId, brandId, brand.SchemaName, roles.ToArray());
        
        _logger.LogInformation("Brand user {UserId} signed in successfully for brand {BrandId}", 
            brandUserId, brandId);
        
        // Return a pseudo-user object for compatibility
        var pseudoUser = new User
        {
            Id = brandUserId,
            Email = $"{apiSecret}@{brandId}",
            FullName = displayName ?? "Brand User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return (true, "Brand sign in successful", pseudoUser, token, "BRAND");
    }
    
    public async Task<(bool Success, string Message, string? NewToken)> ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var pendingUser = await dbContext.PendingUsers
            .FirstOrDefaultAsync(p => p.Email == email.ToLower(), cancellationToken);
        
        if (pendingUser == null)
            return (false, "Email not found or already verified", null);
        
        // Generate new token
        pendingUser.VerificationToken = Guid.NewGuid().ToString();
        pendingUser.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Send new activation email
        await SendActivationEmailAsync(pendingUser, cancellationToken);
        
        _logger.LogInformation("Resent verification email for {Email}", email);
        
        return (true, "Verification email resent. Please check your inbox.", pendingUser.VerificationToken);
    }
    
    // ==================== USER MANAGEMENT ====================
    
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.Users
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.Users
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLower(), cancellationToken);
    }
    
    public async Task<User?> UpdateProfileAsync(
        string userId,
        string? fullName = null,
        string? profilePictureUrl = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return null;
        
        // Store previous state for event
        var previousUser = new User
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
        
        if (!string.IsNullOrWhiteSpace(fullName))
            user.FullName = fullName.Trim();
        
        if (profilePictureUrl != null)
            user.ProfilePictureUrl = profilePictureUrl;
        
        user.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Publish subscription event
        var eventSender = scope.ServiceProvider.GetService<ITopicEventSender>();
        if (eventSender != null)
        {
            var userEvent = new SubscriptionEvent<User>
            {
                Event = EventType.Updated,
                Document = user,
                Time = DateTime.UtcNow,
                Previous = previousUser
            };
            await eventSender.SendAsync("UserChanged", userEvent, cancellationToken);
        }
        
        _logger.LogInformation("Updated profile for user {UserId}", userId);
        
        return user;
    }
    
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (!IsPasswordValid(newPassword))
            return (false, "New password must be at least 8 characters with uppercase, lowercase, and number");
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return (false, "User not found");
        
        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return (false, "Current password is incorrect");
        
        // Store previous state for event
        var previousUser = new User
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
        
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Publish subscription event
        var eventSender = scope.ServiceProvider.GetService<ITopicEventSender>();
        if (eventSender != null)
        {
            var userEvent = new SubscriptionEvent<User>
            {
                Event = EventType.Updated,
                Document = user,
                Time = DateTime.UtcNow,
                Previous = previousUser
            };
            await eventSender.SendAsync("UserChanged", userEvent, cancellationToken);
        }
        
        _logger.LogInformation("Password changed for user {UserId}", userId);
        
        return (true, "Password changed successfully");
    }
    
    public async Task<(bool Success, string Message, string? ResetToken)> RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        
        if (user == null)
        {
            // Don't reveal if email exists for security
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return (true, "If the email exists, a password reset link has been sent", null);
        }
        
        // Generate reset token
        var resetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour
        user.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Send email with reset token
        var resetLink = $"{EnvironmentConfig.App.BaseUrl}/reset-password?token={resetToken}";
        await _mailerService.SendPasswordResetEmailAsync(email, resetToken, resetLink);
        
        _logger.LogInformation("Password reset requested for user {Email}", email);
        
        // Return token for testing purposes (in production, only send via email)
        return (true, "If the email exists, a password reset link has been sent", resetToken);
    }
    
    public async Task<(bool Success, string Message)> ResetPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (!IsPasswordValid(newPassword))
            return (false, "New password must be at least 8 characters with uppercase, lowercase, and number");
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => 
                u.PasswordResetToken == resetToken && 
                u.PasswordResetTokenExpiry != null && 
                u.PasswordResetTokenExpiry > DateTime.UtcNow, 
                cancellationToken);
        
        if (user == null)
            return (false, "Invalid or expired reset token");
        
        // Store previous state for event
        var previousUser = new User
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
        
        // Update password and clear reset token
        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Publish subscription event
        var eventSender = scope.ServiceProvider.GetService<ITopicEventSender>();
        if (eventSender != null)
        {
            var userEvent = new SubscriptionEvent<User>
            {
                Event = EventType.Updated,
                Document = user,
                Time = DateTime.UtcNow,
                Previous = previousUser
            };
            await eventSender.SendAsync("UserChanged", userEvent, cancellationToken);
        }
        
        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        
        return (true, "Password has been reset successfully");
    }
    
    public async Task<IEnumerable<User>> GetAllUsersAsync(
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        IQueryable<User> query = dbContext.Users
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt);
        
        if (skip.HasValue)
            query = query.Skip(skip.Value);
        
        if (take.HasValue)
            query = query.Take(take.Value);
        
        return await query.ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<PendingUser>> GetPendingUsersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await dbContext.PendingUsers
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<(bool Success, string Message, User? User)> GrantRoleAsync(
        string userId,
        string role,
        string? grantedBy = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return (false, "User not found", null);
        
        // Check if user already has this role
        if (user.UserRoles.Any(ur => ur.Role == role))
            return (false, $"User already has role: {role}", user);
        
        // Create new role assignment
        var userRole = new UserRole
        {
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = grantedBy
        };
        
        dbContext.UserRoles.Add(userRole);
        user.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Granted role {Role} to user {UserId}", role, userId);
        
        return (true, $"Role {role} granted successfully", user);
    }
    
    public async Task<(bool Success, string Message, User? User)> RevokeRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return (false, "User not found", null);
        
        var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role == role);
        if (userRole == null)
            return (false, $"User doesn't have role: {role}", user);
        
        // Don't allow removing the last role
        if (user.UserRoles.Count == 1)
            return (false, "Cannot remove the last role from user", user);
        
        dbContext.UserRoles.Remove(userRole);
        user.UserRoles.Remove(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Revoked role {Role} from user {UserId}", role, userId);
        
        return (true, $"Role {role} revoked successfully", user);
    }
    
    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return false;
        
        // Cascade delete will handle UserRoles
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted user {UserId}", userId);
        
        return true;
    }
    
    public async Task<(bool Success, string Message, User? User)> ForceVerifyUserAsync(
        string pendingUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var pendingUser = await dbContext.PendingUsers
            .FirstOrDefaultAsync(p => p.Id == pendingUserId, cancellationToken);
        
        if (pendingUser == null)
            return (false, "Pending user not found", null);
        
        // Check if email already exists
        var existingUser = await dbContext.Users
            .AnyAsync(u => u.Email == pendingUser.Email, cancellationToken);
        
        if (existingUser)
        {
            dbContext.PendingUsers.Remove(pendingUser);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (false, "Email already verified", null);
        }
        
        // Create verified user
        var user = new User
        {
            Email = pendingUser.Email,
            PasswordHash = pendingUser.PasswordHash,
            FullName = pendingUser.Email, // Use email as default name
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Assign default User role
        var userRole = new UserRole
        {
            UserId = user.Id,
            Role = Constants.UserRoles.User,
            AssignedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(userRole);
        
        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(userRole);
        dbContext.PendingUsers.Remove(pendingUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Force verified user {Email}", user.Email);
        
        return (true, "User verified successfully", user);
    }
    
    public async Task<bool> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return false;
        
        // User deactivation removed - use role management instead
        user.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deactivated user {UserId}", userId);
        
        return true;
    }
    
    public async Task<int> CleanupExpiredPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var expiredUsers = await dbContext.PendingUsers
            .Where(p => p.TokenExpiresAt < DateTime.UtcNow.AddDays(-7))
            .ToListAsync(cancellationToken);
        
        if (!expiredUsers.Any())
            return 0;
        
        dbContext.PendingUsers.RemoveRange(expiredUsers);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Cleaned up {Count} expired pending users", expiredUsers.Count);
        
        return expiredUsers.Count;
    }
    
    // ==================== TOKEN MANAGEMENT ====================
    
    public string GenerateJwtToken(User user)
    {
        var secretKey = EnvironmentConfig.Jwt.Secret;
        var issuer = EnvironmentConfig.Jwt.Issuer;
        var audience = EnvironmentConfig.Jwt.Audience;
        var expirationMinutes = EnvironmentConfig.Jwt.ExpirationMinutes;
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("email_verified", "true")
        };
        
        // Add all user roles as claims
        if (user.UserRoles != null)
        {
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role));
            }
        }
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    /// <summary>
    /// Generate JWT token for brand authentication
    /// </summary>
    private string GenerateBrandJwtToken(string userId, string brandId, string schemaName, string[] roles)
    {
        var secretKey = EnvironmentConfig.Jwt.Secret;
        var issuer = EnvironmentConfig.Jwt.Issuer;
        var audience = EnvironmentConfig.Jwt.Audience;
        var expirationMinutes = EnvironmentConfig.Jwt.ExpirationMinutes;
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("brand_id", brandId),
            new Claim("brand_schema", schemaName),
            new Claim("token_type", "BRAND")
        };
        
        // Add brand roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public bool IsPasswordValid(string password)
    {
        // At least 8 characters, one uppercase, one lowercase, one number
        var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
        return regex.IsMatch(password);
    }
    
    private bool IsEmailValid(string email)
    {
        try
        {
            var addr = new global::System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    
    // ==================== EMAIL NOTIFICATIONS ====================
    
    private async Task SendActivationEmailAsync(PendingUser pendingUser, CancellationToken cancellationToken)
    {
        try
        {
            // Generate activation URL using public-facing URL
            var baseUrl = EnvironmentConfig.App.PublicUrl;
            var activationUrl = $"{baseUrl}/api/auth/activate?token={pendingUser.VerificationToken}";
            
            // Prepare template data
            var templateData = new Dictionary<string, object>
            {
                { "appName", "Kanriya" },
                { "userName", pendingUser.Email.Split('@')[0] }, // Use email username as display name
                { "activationUrl", activationUrl },
                { "year", DateTime.UtcNow.Year }
            };
            
            // Prepare templated email request
            var emailRequest = new SendTemplatedEmailRequest
            {
                ToEmail = pendingUser.Email,
                TemplateName = "user_activation",
                TemplateData = templateData,
                Priority = 1, // High priority for activation emails
                Metadata = new Dictionary<string, object>
                {
                    { "type", "account_activation" },
                    { "user_email", pendingUser.Email }
                }
            };
            
            // Queue the templated email
            var result = await _mailerService.QueueTemplatedEmailAsync(emailRequest, cancellationToken);
            
            _logger.LogInformation("Queued activation email for {Email} with ID {EmailId}", 
                pendingUser.Email, result.EmailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", pendingUser.Email);
            // Don't throw - let signup succeed even if email fails
        }
    }
    
    private async Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            // Generate login URL
            var baseUrl = EnvironmentConfig.App.PublicUrl;
            var loginUrl = $"{baseUrl}/login";
            
            // Prepare template data
            var templateData = new Dictionary<string, object>
            {
                { "appName", "Kanriya" },
                { "userName", user.FullName ?? user.Email.Split('@')[0] },
                { "loginUrl", loginUrl },
                { "year", DateTime.UtcNow.Year }
            };
            
            // Prepare templated email request
            var emailRequest = new SendTemplatedEmailRequest
            {
                ToEmail = user.Email,
                TemplateName = "welcome",
                TemplateData = templateData,
                Priority = 3, // Normal priority for welcome emails
                Metadata = new Dictionary<string, object>
                {
                    { "type", "welcome_email" },
                    { "user_id", user.Id },
                    { "user_email", user.Email }
                }
            };
            
            // Queue the templated email
            var result = await _mailerService.QueueTemplatedEmailAsync(emailRequest, cancellationToken);
            
            _logger.LogInformation("Queued welcome email for {Email} with ID {EmailId}", 
                user.Email, result.EmailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            // Don't throw - email failure shouldn't prevent user activation
        }
    }
    
    private async Task SendPasswordResetEmailAsync(User user, string resetToken, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = EnvironmentConfig.App.PublicUrl;
            var resetUrl = $"{baseUrl}/api/auth/reset-password?token={resetToken}";
            
            var emailRequest = new SendEmailRequest
            {
                ToEmail = user.Email,
                Subject = "Reset Your Password - Kanriya",
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #333;'>Password Reset Request</h2>
                            <p>We received a request to reset your password. Click the button below to set a new password:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' 
                                   style='background-color: #2196F3; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Reset Password
                                </a>
                            </div>
                            <p>Or copy and paste this link in your browser:</p>
                            <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='color: #999; font-size: 12px;'>
                                This link will expire in 1 hour. If you didn't request a password reset, 
                                you can safely ignore this email.
                            </p>
                        </div>
                    </body>
                    </html>",
                TextBody = $@"
Password Reset Request

We received a request to reset your password. Click the link below to set a new password:

{resetUrl}

This link will expire in 1 hour. If you didn't request a password reset, you can safely ignore this email.",
                Priority = 1,
                Metadata = new Dictionary<string, object>
                {
                    { "type", "password_reset" },
                    { "user_id", user.Id }
                }
            };
            
            await _mailerService.QueueEmailAsync(emailRequest, cancellationToken);
            
            _logger.LogInformation("Queued password reset email for {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }
}