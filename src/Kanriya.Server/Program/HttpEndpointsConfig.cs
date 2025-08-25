using Kanriya.Server.HttpRoutes;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Microsoft.OpenApi.Models;

namespace Kanriya.Server.Program;

/// <summary>
/// Configuration for HTTP endpoints and Swagger
/// </summary>
public static class HttpEndpointsConfig
{
    /// <summary>
    /// Configure HTTP services including MVC, Swagger, and Razor
    /// </summary>
    public static void ConfigureHttpServices(IServiceCollection services)
    {
        // Add MVC with Razor views support
        services.AddControllersWithViews();
        
        // Configure Razor view engine
        services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
        {
            options.ViewLocationFormats.Clear();
            options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
        });
        
        // Add API endpoint explorer for Swagger
        services.AddEndpointsApiExplorer();
        
        // Configure Swagger/OpenAPI
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GQLServer API",
                Version = AppVersion.GetShortVersion(),
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
        
        LogService.LogSuccess("HTTP services configured (MVC, Razor, Swagger)");
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
        
        // Map controller routes
        app.MapControllers();
        
        // Map controller routes with views
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        
        // Map custom API routes (health and api info)
        app.MapApiInfoRoutes();
    }
}