using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Services;

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
        var connectionString = EnvironmentConfig.Database.GetConnectionString();
        
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
        
        LogService.LogSuccess($"Database configured with PostgreSQL");
        LogService.LogInfo($"Connection: {GetSafeConnectionString(connectionString)}");
        LogService.LogInfo($"Using {EnvironmentConfig.Database.Host}:{EnvironmentConfig.Database.Port} (set POSTGRES_HOST and POSTGRES_PORT in .env to change)");
    }
    
    /// <summary>
    /// Ensure database is created and apply migrations
    /// </summary>
    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
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
                
                // Email templates seeding removed for now
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