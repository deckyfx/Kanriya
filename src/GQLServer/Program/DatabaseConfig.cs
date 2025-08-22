using Microsoft.EntityFrameworkCore;
using GQLServer.Data;

namespace GQLServer.Program;

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
            options.UseNpgsql(connectionString);
            
            // Enable detailed errors in development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });
        
        Console.WriteLine($"✓ Database configured with PostgreSQL");
        Console.WriteLine($"  Connection: {GetSafeConnectionString(connectionString)}");
        
        // Show which configuration is being used
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        Console.WriteLine($"  Using {host}:{port} (set POSTGRES_HOST and POSTGRES_PORT in .env to change)");
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
                Console.WriteLine("✓ Successfully connected to PostgreSQL database");
                
                // Create database if it doesn't exist
                await dbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("✓ Database is ready");
            }
            else
            {
                Console.WriteLine("✗ Failed to connect to PostgreSQL database");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Database connection error: {ex.Message}");
            Console.WriteLine("  Make sure PostgreSQL is running on localhost:10005");
            Console.WriteLine("  Check your docker-compose is up: docker-compose up -d");
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