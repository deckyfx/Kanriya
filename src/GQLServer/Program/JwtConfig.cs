using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GQLServer.Program;

/// <summary>
/// Configures JWT Bearer authentication for the application
/// </summary>
public static class JwtConfig
{
    /// <summary>
    /// Configure JWT Bearer authentication with token validation parameters
    /// </summary>
    public static void ConfigureJwtAuthentication(IServiceCollection services)
    {
        // Get JWT configuration values from environment
        var jwtSecret = EnvironmentConfig.Jwt.Secret;
        var jwtIssuer = EnvironmentConfig.Jwt.Issuer;
        var jwtAudience = EnvironmentConfig.Jwt.Audience;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Configure token validation parameters
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero // Remove default 5 min clock skew
                };
                
                // Configure JWT events for GraphQL subscriptions over WebSocket
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Check for access token in query string (used by WebSocket connections)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        // If the request is for GraphQL and the token is in query string,
                        // extract it and make it available for authentication
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/graphql"))
                        {
                            context.Token = accessToken;
                        }
                        
                        return Task.CompletedTask;
                    }
                };
            });

        Console.WriteLine($"✓ JWT authentication configured with issuer: {jwtIssuer}");
    }
}