using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Kanriya.Server.Program;

namespace Kanriya.Server.Data;

/// <summary>
/// Design-time factory for AppDbContext
/// Used by EF Core tools for migrations
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load environment configuration
        // For design-time, we need to use a different method that doesn't require WebApplicationBuilder
        var envPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "../../.env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Configure PostgreSQL with snake_case naming
        var connectionString = $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost"};" +
                             $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "10005"};" +
                             $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "kanriya"};" +
                             $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "kanriya"};" +
                             $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "kanriya"}";
        
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        })
        .UseSnakeCaseNamingConvention();
        
        return new AppDbContext(optionsBuilder.Options);
    }
}