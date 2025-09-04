using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;
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
                _logger.LogDebug("Token extracted from request: {Token}", token.Substring(0, Math.Min(20, token.Length)) + "...");
                var principal = ValidateToken(token);
                
                if (principal != null)
                {
                    _logger.LogDebug("Token validated successfully");
                    var currentUser = new CurrentUser();
                    
                    // Check if this is a brand-context token
                    var brandIdClaim = principal.FindFirst("brand_id");
                    var brandSchemaClaim = principal.FindFirst("brand_schema");
                    var tokenTypeClaim = principal.FindFirst("token_type");
                    
                    if (brandIdClaim != null && brandSchemaClaim != null && tokenTypeClaim?.Value == "BRAND")
                    {
                        // Brand-context token
                        var brandUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        
                        if (!string.IsNullOrEmpty(brandUserId))
                        {
                            // For now, create a minimal BrandUser object
                            // In the future, you might want to load this from the brand schema
                            var brandUser = new BrandUser
                            {
                                Id = brandUserId,
                                IsActive = true
                            };
                            
                            // Get roles from claims
                            var roles = principal.FindAll(ClaimTypes.Role)
                                .Select(c => new BrandUserRole { Role = c.Value })
                                .ToList();
                            brandUser.Roles = roles;
                            
                            currentUser.BrandUser = brandUser;
                            currentUser.BrandId = brandIdClaim.Value;
                            currentUser.BrandSchema = brandSchemaClaim.Value;
                            
                            // Extract outlet access information
                            var outletAccessClaim = principal.FindFirst("outlet_access");
                            if (outletAccessClaim != null && outletAccessClaim.Value == "all")
                            {
                                currentUser.HasAllOutletAccess = true;
                            }
                            else
                            {
                                // Get specific outlet IDs from claims
                                var outletIds = principal.FindAll("outlet_id")
                                    .Select(c => c.Value)
                                    .ToList();
                                currentUser.OutletIds = outletIds;
                            }
                            
                            _logger.LogDebug("Authenticated brand user {UserId} for brand {BrandId} with {OutletCount} outlet(s) access", 
                                brandUserId, brandIdClaim.Value, 
                                currentUser.HasAllOutletAccess ? "all" : currentUser.OutletIds.Count.ToString());
                        }
                    }
                    else
                    {
                        // Principal token
                        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                        
                        if (userIdClaim != null)
                        {
                            var user = await userService.GetByIdAsync(userIdClaim.Value);
                            
                            if (user != null)
                            {
                                currentUser.User = user;
                                _logger.LogDebug("Authenticated principal user {UserId}", user.Id);
                            }
                            else
                            {
                                _logger.LogWarning("User not found in database for ID: {UserId}", userIdClaim.Value);
                            }
                        }
                    }
                    
                    // Add to HttpContext if authenticated
                    if (currentUser.IsAuthenticated)
                    {
                        context.Items["CurrentUser"] = currentUser;
                        context.User = principal;
                    }
                }
                else
                {
                    _logger.LogDebug("Token validation failed");
                }
            }
            else
            {
                _logger.LogDebug("No token found in request for path: {Path}", context.Request.Path);
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
        // First check Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Token found in Authorization header");
            return authHeader.Substring(7);
        }
        
        // Then check for token in cookies (for Blazor Server)
        if (context.Request.Cookies.TryGetValue("AuthToken", out var cookieToken))
        {
            _logger.LogDebug("Token found in cookie, length: {Length}", cookieToken?.Length ?? 0);
            // The token might be URL encoded in the cookie
            var decodedToken = System.Web.HttpUtility.UrlDecode(cookieToken);
            _logger.LogDebug("Decoded token length: {Length}", decodedToken?.Length ?? 0);
            return decodedToken;
        }
        
        _logger.LogDebug("No token found in Authorization header or cookies");
        return null;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSecret = Shared.EnvironmentConfig.Jwt.Secret;
            var jwtIssuer = Shared.EnvironmentConfig.Jwt.Issuer;
            var jwtAudience = Shared.EnvironmentConfig.Jwt.Audience;
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
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