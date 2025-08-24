using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kanriya.Server.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Types;
using Microsoft.IdentityModel.Tokens;

namespace Kanriya.Server.Program;

/// <summary>
/// Middleware to handle JWT authentication and populate CurrentUser
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        try
        {
            var token = ExtractToken(context);
            
            if (!string.IsNullOrEmpty(token))
            {
                var principal = ValidateToken(token);
                
                if (principal != null)
                {
                    var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                    
                    if (userIdClaim != null)
                    {
                        var user = await userService.GetByIdAsync(userIdClaim.Value);
                        
                        if (user != null)
                        {
                            // Create CurrentUser and add to HttpContext
                            var currentUser = new CurrentUser { User = user };
                            context.Items["CurrentUser"] = currentUser;
                            
                            // Set the ClaimsPrincipal for authorization
                            context.User = principal;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
        }

        await _next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
            return null;
        
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader.Substring(7);
        
        return null;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? "your-secret-key-here-replace-in-production";
            var key = Encoding.ASCII.GetBytes(jwtSecret);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Extension method to register authentication middleware
/// </summary>
public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}