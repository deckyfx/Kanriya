using System.Reflection;
using IOPath = System.IO.Path;

namespace Kanriya.Shared;

/// <summary>
/// Handles environment configuration including .env file loading
/// Single source of truth for all projects
/// </summary>
public static class EnvironmentConfig
{
    /// <summary>
    /// Track the last loaded .env file path for logging
    /// </summary>
    public static string? LastLoadedEnvPath { get; private set; }
    
    /// <summary>
    /// Load environment variables from .env file (with debug output)
    /// </summary>
    public static void LoadEnvironment(bool debug = false)
    {
        var envPaths = GetEnvironmentFilePaths();
        
        if (debug)
        {
            Console.WriteLine($"DEBUG: Checking for .env file in:");
            foreach (var path in envPaths)
            {
                Console.WriteLine($"  - {path} (exists: {File.Exists(path)})");
            }
        }
        
        bool envLoaded = false;
        foreach (var path in envPaths)
        {
            if (File.Exists(path))
            {
                DotNetEnv.Env.Load(path);
                LastLoadedEnvPath = path;
                
                if (debug)
                {
                    Console.WriteLine($"DEBUG: Successfully loaded .env from: {path}");
                }
                
                envLoaded = true;
                break;
            }
        }

        if (!envLoaded)
        {
            LastLoadedEnvPath = null;
            if (debug)
            {
                Console.WriteLine("DEBUG: No .env file found, using defaults");
            }
        }
    }
    
    /// <summary>
    /// Gets potential paths for .env file in priority order, always prioritizing root directory
    /// </summary>
    private static string[] GetEnvironmentFilePaths()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var executableDirectory = AppContext.BaseDirectory;
        // Use AppContext.BaseDirectory for single-file app compatibility
        var assemblyDirectory = AppContext.BaseDirectory;
        
        return new[]
        {
            // Always look for root directory first (assuming project structure)
            IOPath.GetFullPath(IOPath.Combine(workingDirectory, "../../../.env")),         // From src/Project to root
            IOPath.GetFullPath(IOPath.Combine(executableDirectory, "../../../../../.env")), // From bin/Debug/net9.0 to root
            IOPath.GetFullPath(IOPath.Combine(assemblyDirectory, "../../../../../.env")),   // From assembly location to root
            
            // Fallback locations
            IOPath.Combine(workingDirectory, ".env"),                    // Working directory
            IOPath.Combine(workingDirectory, "../../.env"),              // Parent directories
            IOPath.Combine(executableDirectory, ".env"),                 // Executable directory
            IOPath.Combine(executableDirectory, "../../../.env"),        // Development path from bin/Debug
            IOPath.Combine(assemblyDirectory, ".env"),                   // Assembly directory
            ".env"                                                        // Current directory fallback
        };
    }
    
    /// <summary>
    /// Get JWT configuration values from environment
    /// </summary>
    public static class Jwt
    {
        public static string Secret => 
            Environment.GetEnvironmentVariable("AUTH_JWT_SECRET") ?? 
            Environment.GetEnvironmentVariable("JWT__Secret") ?? 
            "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
            
        public static string Issuer => 
            Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
            Environment.GetEnvironmentVariable("JWT__Issuer") ?? 
            "YourGraphQLServer";
            
        public static string Audience => 
            Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
            Environment.GetEnvironmentVariable("JWT__Audience") ?? 
            "YourGraphQLClient";
            
        public static int ExpirationMinutes => 
            int.Parse(Environment.GetEnvironmentVariable("ACCESS_TOKEN_EXPIRY_MINUTES") ?? 
                     Environment.GetEnvironmentVariable("JWT__ExpirationMinutes") ?? "60");
                     
        public static int RefreshTokenExpiryDays =>
            int.Parse(Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRY_DAYS") ?? "30");
    }
    
    /// <summary>
    /// Get Mail configuration values from environment
    /// </summary>
    public static class Mail
    {
        public static string Provider => 
            Environment.GetEnvironmentVariable("MAIL_PROVIDER") ?? "smtp";
            
        public static string GmailUsername => 
            Environment.GetEnvironmentVariable("GMAIL_USERNAME") ?? "";
            
        public static string GmailAppPassword => 
            Environment.GetEnvironmentVariable("GMAIL_APP_PASSWORD") ?? "";
            
        public static string FromAddress => 
            Environment.GetEnvironmentVariable("MAIL_FROM_ADDRESS") ?? "noreply@example.com";
            
        public static string FromName => 
            Environment.GetEnvironmentVariable("MAIL_FROM_NAME") ?? "System";
            
        public static int RateLimit => 
            int.Parse(Environment.GetEnvironmentVariable("MAIL_RATE_LIMIT") ?? "100");
            
        public static int BatchSize => 
            int.Parse(Environment.GetEnvironmentVariable("MAIL_BATCH_SIZE") ?? "10");
    }
    
    /// <summary>
    /// Get server configuration values from environment
    /// </summary>
    public static class Server
    {
        public static string Ip => 
            Environment.GetEnvironmentVariable("SERVER_BIND_IP") ?? "localhost";
            
        public static string Port => 
            Environment.GetEnvironmentVariable("SERVER_LISTEN_PORT") ?? "5000";
            
        public static string Urls => $"http://{Ip}:{Port}";
        
        // Public-facing URL for emails, callbacks, etc. (can be different from bind URL)
        public static string PublicUrl => 
            Environment.GetEnvironmentVariable("SERVER_PUBLIC_URL") ?? Urls;
        
        // Base URL for application links (password reset, etc.)
        public static string BaseUrl => 
            Environment.GetEnvironmentVariable("APP_BASE_URL") ?? PublicUrl;
        
        public static string AspNetCoreEnvironment =>
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }
    
    /// <summary>
    /// Get admin seeder configuration values from environment
    /// </summary>
    public static class Admin
    {
        public static string? Username => 
            Environment.GetEnvironmentVariable("SERVER_ADMIN_USERNAME");
            
        public static string? Password => 
            Environment.GetEnvironmentVariable("SERVER_ADMIN_PASSWORD");
            
        public static string FirstName => 
            Environment.GetEnvironmentVariable("SERVER_ADMIN_FIRST_NAME") ?? "Super";
            
        public static string LastName => 
            Environment.GetEnvironmentVariable("SERVER_ADMIN_LAST_NAME") ?? "Admin";
            
        public static bool HasAdminConfig => 
            !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
    }
    
    /// <summary>
    /// Get Seq logging configuration values from environment
    /// </summary>
    public static class Seq
    {
        public static string ServerUrl
        {
            get
            {
                var host = Environment.GetEnvironmentVariable("SEQ_LOG_HOST") ?? "localhost";
                var port = Environment.GetEnvironmentVariable("SEQ_LOG_PORT") ?? "10002";
                var secure = Environment.GetEnvironmentVariable("SEQ_LOG_SECURE") ?? "false";
                var protocol = secure.ToLower() == "true" ? "https" : "http";
                return $"{protocol}://{host}:{port}";
            }
        }
            
        public static string? ApiKey => 
            Environment.GetEnvironmentVariable("SEQ_API_KEY");
    }
    
    /// <summary>
    /// Get PostgreSQL configuration values from environment
    /// </summary>
    public static class Database
    {
        public static string Host => 
            Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            
        public static string Port => 
            Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            
        public static string DatabaseName => 
            Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "mydatabase";
            
        public static string Username => 
            Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "user";
            
        public static string Password => 
            Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "password";
            
        public static string GetConnectionString()
        {
            // POSTGRES_HOST can be:
            // - "localhost" when running GQLServer as executable during development
            // - "db" when running GQLServer as Docker container (using docker-compose service name)
            // POSTGRES_PORT can be:
            // - "10005" when connecting from host machine (exposed port)
            // - "5432" when connecting from within Docker network (internal port)
            return $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";
        }
    }
}