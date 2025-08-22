using IOPath = System.IO.Path;

namespace GQLServer.Program;

/// <summary>
/// Handles environment configuration including .env file loading and server URL setup
/// </summary>
public static class EnvironmentConfig
{
    /// <summary>
    /// Load environment variables from .env file and configure server URLs
    /// </summary>
    public static void LoadEnvironment(WebApplicationBuilder builder)
    {
        LoadEnvFile();
        ConfigureServerUrls(builder);
    }
    
    /// <summary>
    /// Load .env file from multiple possible locations
    /// </summary>
    private static void LoadEnvFile()
    {
        var envPaths = new[]
        {
            ".env",                                                      // Current directory
            IOPath.Combine(AppContext.BaseDirectory, ".env"),           // Where the binary is
            IOPath.Combine(AppContext.BaseDirectory, "../../../.env"),  // Development path
            IOPath.Combine(Directory.GetCurrentDirectory(), ".env"),    // Working directory
            "/home/decky/Documents/works/others/learn-csharp/.env"      // Absolute path fallback
        };

        bool envLoaded = false;
        foreach (var path in envPaths)
        {
            if (File.Exists(path))
            {
                DotNetEnv.Env.Load(path);
                Console.WriteLine($"✓ Loaded .env from: {path}");
                envLoaded = true;
                break;
            }
        }

        if (!envLoaded)
        {
            Console.WriteLine("⚠ No .env file found, using defaults");
        }
    }
    
    /// <summary>
    /// Configure Kestrel server URLs from environment variables
    /// </summary>
    private static void ConfigureServerUrls(WebApplicationBuilder builder)
    {
        var appIp = Environment.GetEnvironmentVariable("APP_IP") ?? "localhost";
        var appPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "5000";
        var urls = $"http://{appIp}:{appPort}";
        
        builder.WebHost.UseUrls(urls);
        Console.WriteLine($"Server will listen on: {urls}");
    }
    
    /// <summary>
    /// Get JWT configuration values from environment
    /// </summary>
    public static class Jwt
    {
        public static string Secret => 
            Environment.GetEnvironmentVariable("JWT__Secret") ?? 
            "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
            
        public static string Issuer => 
            Environment.GetEnvironmentVariable("JWT__Issuer") ?? 
            "YourGraphQLServer";
            
        public static string Audience => 
            Environment.GetEnvironmentVariable("JWT__Audience") ?? 
            "YourGraphQLClient";
            
        public static int ExpirationMinutes => 
            int.Parse(Environment.GetEnvironmentVariable("JWT__ExpirationMinutes") ?? "60");
    }
}