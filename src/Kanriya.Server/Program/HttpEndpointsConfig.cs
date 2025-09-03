using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Kanriya.Shared;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Kanriya.Server.Program;

/// <summary>
/// Configuration for HTTP endpoints and Swagger
/// </summary>
public static class HttpEndpointsConfig
{
    /// <summary>
    /// Configure HTTP services including API Controllers and Swagger
    /// </summary>
    public static void ConfigureHttpServices(IServiceCollection services)
    {
        // Add API Controllers only (no views needed)
        services.AddControllers();
        
        // Add API endpoint explorer for Swagger
        services.AddEndpointsApiExplorer();
        
        // Configure Swagger/OpenAPI
        services.AddSwaggerGen(options =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GQLServer API",
                Version = BuildInfo.GetShortVersion(assembly),
                Description = "REST API endpoints for GQLServer alongside GraphQL",
                Contact = new OpenApiContact
                {
                    Name = "GQLServer Team",
                    Email = "support@gqlserver.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
            
            // Add JWT Bearer authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
        
        LogService.LogSuccess("HTTP services configured (API Controllers, Swagger)");
    }
    
    /// <summary>
    /// Configure HTTP middleware and map routes
    /// </summary>
    public static void ConfigureHttpMiddleware(WebApplication app)
    {
        // Enable Swagger in development and production
        if (!app.Environment.IsProduction() || true) // Always enable for now
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "GQLServer API v1");
                options.RoutePrefix = "swagger"; // Swagger UI at /swagger
                options.DocumentTitle = "GQLServer API Documentation";
                options.EnableDeepLinking();
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
            });
            
            LogService.LogSuccess("Swagger UI enabled at /swagger");
        }
        
        // Serve static files (css, js, images, etc.)
        app.UseStaticFiles();
        
        // Enable routing
        app.UseRouting();
        
        // Add session middleware (must be after UseRouting and before UseEndpoints)
        app.UseSession();
        
        // Configure endpoints
        app.UseEndpoints(endpoints =>
        {
            // Map API Controllers for RESTful endpoints
            endpoints.MapControllers();
            
            // Map Razor Pages (only for _Host which hosts Blazor)
            endpoints.MapRazorPages();
            
            // Map Blazor Server SignalR hub
            endpoints.MapBlazorHub();
            
            // Fallback to Blazor host for all routes except API and static files
            endpoints.MapFallbackToPage("/{*path:nonfile}", "/_Host");
        });
        
        LogService.LogSuccess("Blazor application configured at root path");
    }
}