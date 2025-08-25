using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;

namespace Kanriya.Server.Program;

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
                
                // Configure JWT events for custom token extraction
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Check for access token in query string (used by WebSocket connections)
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        // Check for custom X-API-TOKEN header
                        var apiToken = context.Request.Headers["X-API-TOKEN"].FirstOrDefault();
                        
                        // If X-API-TOKEN is present, use it
                        if (!string.IsNullOrEmpty(apiToken))
                        {
                            context.Token = apiToken;
                        }
                        // Otherwise, if the request is for GraphQL and the token is in query string
                        else if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/graphql"))
                        {
                            context.Token = accessToken;
                        }
                        
                        return Task.CompletedTask;
                    }
                };
            });

        LogService.LogSuccess($"JWT authentication configured with issuer: {jwtIssuer}");
    }
}