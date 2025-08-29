using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Kanriya.Server.Services.Data;

namespace Kanriya.Server.Program;

/// <summary>
/// Handles database configuration including Entity Framework setup
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Configure Entity Framework with PostgreSQL
    /// </summary>
    public static void ConfigureDatabase(IServiceCollection services)
    {
        var connectionString = Shared.EnvironmentConfig.Database.GetConnectionString();
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention(); // Apply snake_case naming for PostgreSQL
            
            // Enable detailed errors in development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });
        
        // Add DbContextFactory for services that need to create contexts
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
                   
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });
        
        // Register DatabaseSeeder service
        services.AddScoped<DatabaseSeeder>();
        
        LogService.LogSuccess($"Database configured with PostgreSQL");
        LogService.LogInfo($"Connection: {GetSafeConnectionString(connectionString)}");
        LogService.LogInfo($"Using {Shared.EnvironmentConfig.Database.Host}:{Shared.EnvironmentConfig.Database.Port} (set POSTGRES_HOST and POSTGRES_PORT in .env to change)");
    }
    
    /// <summary>
    /// Ensure database is created and apply migrations
    /// </summary>
    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        
        try
        {
            // Test connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                LogService.LogSuccess("Successfully connected to PostgreSQL database");
                
                // Create database if it doesn't exist
                await dbContext.Database.EnsureCreatedAsync();
                LogService.LogSuccess("Database is ready");
                
                // Run database seeders
                LogService.LogInfo("Running database seeders...");
                await seeder.SeedAsync();
                LogService.LogSuccess("Database seeding completed");
            }
            else
            {
                LogService.LogError("Failed to connect to PostgreSQL database");
            }
        }
        catch (Exception ex)
        {
            LogService.LogError($"Database connection error: {ex.Message}", ex);
            LogService.LogInfo("Make sure PostgreSQL is running on localhost:10005");
            LogService.LogInfo("Check your docker-compose is up: docker-compose up -d");
        }
    }
    
    /// <summary>
    /// Get connection string with password masked for logging
    /// </summary>
    private static string GetSafeConnectionString(string connectionString)
    {
        // Mask the password in the connection string for security
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString, 
            @"Password=[^;]+", 
            "Password=***"
        );
    }
}