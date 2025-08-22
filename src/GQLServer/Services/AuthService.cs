using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GQLServer.Types.Outputs;

namespace GQLServer.Services;

// AUTHENTICATION SERVICE - Handles user authentication and JWT token generation
// =============================================================================
// This service manages:
// - User registration and login
// - Password hashing with BCrypt
// - JWT token generation and validation
// - User roles and claims

public class AuthService
{
    // In-memory user storage (replace with database in production)
    private static readonly List<UserData> _users = new()
    {
        // Seed data with different roles
        new UserData 
        { 
            Id = 1, 
            Username = "admin", 
            Email = "admin@example.com",
            // Password: "admin123" (hashed with BCrypt)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        },
        new UserData 
        { 
            Id = 2, 
            Username = "moderator", 
            Email = "mod@example.com",
            // Password: "mod123" (hashed with BCrypt)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("mod123"),
            Role = "Moderator",
            CreatedAt = DateTime.UtcNow
        },
        new UserData 
        { 
            Id = 3, 
            Username = "user", 
            Email = "user@example.com",
            // Password: "user123" (hashed with BCrypt)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        }
    };
    
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;
    
    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Read JWT settings from configuration or use defaults
        _jwtSecret = _configuration["JWT:Secret"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
        _jwtIssuer = _configuration["JWT:Issuer"] ?? "YourGraphQLServer";
        _jwtAudience = _configuration["JWT:Audience"] ?? "YourGraphQLClient";
        _jwtExpirationMinutes = int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "60");
    }
    
    // Register a new user
    public async Task<User?> RegisterAsync(string username, string email, string password, string? role = null)
    {
        // Check if user already exists
        if (_users.Any(u => u.Username == username || u.Email == email))
        {
            throw new GraphQLException("Username or email already exists");
        }
        
        // Create new user with hashed password
        var newUser = new UserData
        {
            Id = _users.Count + 1,
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role ?? "User", // Default to "User" role
            CreatedAt = DateTime.UtcNow
        };
        
        _users.Add(newUser);
        
        // Return user without password hash
        return new User
        {
            Id = newUser.Id,
            Username = newUser.Username,
            Email = newUser.Email,
            Role = newUser.Role,
            CreatedAt = newUser.CreatedAt
        };
    }
    
    // Authenticate user and generate JWT token
    public async Task<AuthPayload?> LoginAsync(string username, string password)
    {
        // Find user by username
        var userData = _users.FirstOrDefault(u => u.Username == username);
        
        if (userData == null)
        {
            throw new GraphQLException("Invalid username or password");
        }
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, userData.PasswordHash))
        {
            throw new GraphQLException("Invalid username or password");
        }
        
        // Generate JWT token
        var token = GenerateJwtToken(userData);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);
        
        // Return auth payload
        return new AuthPayload
        {
            Token = token,
            User = new User
            {
                Id = userData.Id,
                Username = userData.Username,
                Email = userData.Email,
                Role = userData.Role,
                CreatedAt = userData.CreatedAt
            },
            ExpiresAt = expiresAt
        };
    }
    
    // Get user by ID
    public User? GetUserById(int id)
    {
        var userData = _users.FirstOrDefault(u => u.Id == id);
        
        if (userData == null)
            return null;
        
        return new User
        {
            Id = userData.Id,
            Username = userData.Username,
            Email = userData.Email,
            Role = userData.Role,
            CreatedAt = userData.CreatedAt
        };
    }
    
    // Get all users (admin only)
    public List<User> GetAllUsers()
    {
        return _users.Select(u => new User
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList();
    }
    
    // Generate JWT token
    private string GenerateJwtToken(UserData user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        
        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("role", user.Role), // HotChocolate looks for this claim
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        // Create token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        // Create and write token
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    // Internal user data class (includes password hash)
    private class UserData
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }
    }
}