using Kanriya.Server.Data;
using Kanriya.Server.Services.Data;

namespace Kanriya.Server.Blazor.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<(bool success, string? token, User? user)> AuthenticateAsync(string email, string password)
        {
            try
            {
                // Find user by email
                var user = await _userService.GetByEmailAsync(email);
                if (user == null)
                {
                    return (false, null, null);
                }

                // For now, we'll use a simple password check
                // In production, use proper password hashing verification
                // The actual authentication happens through GraphQL mutations
                
                // Generate a simple token (not JWT for now)
                var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Email}"));

                // Set auth cookie (httpOnly)
                SetAuthCookie(token);

                return (true, token, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email: {Email}", email);
                return (false, null, null);
            }
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            // Simple validation for now
            // In production, use proper JWT validation
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                return Task.FromResult(decoded.Contains(":"));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task SignOutAsync()
        {
            // Clear auth cookie
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("auth-token");
            }
            return Task.CompletedTask;
        }

        private void SetAuthCookie(string token)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Append("auth-token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30)
                });
            }
        }
    }
}