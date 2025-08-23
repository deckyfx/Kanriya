using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using GQLServer.Data;
using GQLServer.Types.Outputs;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GQLServer.Services;

/// <summary>
/// Service implementation for user authentication and management
/// </summary>
public class UserService : IUserService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;
    
    public UserService(
        IServiceProvider serviceProvider,
        ILogger<UserService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
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
        
        // In production, send email here
        // For development, return token
        return (true, "Verification email sent", pendingUser.VerificationToken);
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
        
        return (true, "Email verified successfully", user);
    }
    
    public async Task<(bool Success, string Message, User? User, string? Token)> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Find user by email
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower(), cancellationToken);
        
        if (user == null)
            return (false, "Invalid email or password", null, null);
        
        // Verify password
        if (!VerifyPassword(password, user.PasswordHash))
            return (false, "Invalid email or password", null, null);
        
        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        _logger.LogInformation("User {Email} signed in successfully", user.Email);
        
        return (true, "Sign in successful", user, token);
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
        
        _logger.LogInformation("Resent verification email for {Email}", email);
        
        return (true, "Verification email resent", pendingUser.VerificationToken);
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
        var jwtSettings = _configuration.GetSection("JWT");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "YourGraphQLServer";
        var audience = jwtSettings["Audience"] ?? "YourGraphQLClient";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
        
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
            var addr = new System.Net.Mail.MailAddress(email);
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
}