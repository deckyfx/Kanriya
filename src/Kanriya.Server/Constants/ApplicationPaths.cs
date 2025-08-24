using System.Reflection;
using IOPath = System.IO.Path;

namespace Kanriya.Server.Constants;

/// <summary>
/// Provides centralized access to application paths and directories
/// </summary>
public static class ApplicationPaths
{
    private static readonly Lazy<string> _workingDirectory = new(() => Directory.GetCurrentDirectory());
    private static readonly Lazy<string> _executableDirectory = new(() => AppContext.BaseDirectory);
    private static readonly Lazy<string> _assemblyDirectory = new(() => 
        IOPath.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory);

    /// <summary>
    /// Gets the current working directory where the application was started
    /// </summary>
    public static string WorkingDirectory => _workingDirectory.Value;

    /// <summary>
    /// Gets the directory containing the executable
    /// </summary>
    public static string ExecutableDirectory => _executableDirectory.Value;

    /// <summary>
    /// Gets the directory containing the main assembly
    /// </summary>
    public static string AssemblyDirectory => _assemblyDirectory.Value;

    /// <summary>
    /// Gets the data directory path
    /// </summary>
    public static string DataDirectory => IOPath.Combine(WorkingDirectory, "data");
    
    /// <summary>
    /// Gets the cache directory path
    /// </summary>
    public static string CacheDirectory => IOPath.Combine(WorkingDirectory, "cache");
    
    /// <summary>
    /// Gets the logs directory path (inside cache directory)
    /// </summary>
    public static string LogsDirectory => IOPath.Combine(CacheDirectory, "logs");

    /// <summary>
    /// Gets potential paths for .env file in priority order
    /// </summary>
    public static string[] GetEnvironmentFilePaths()
    {
        return new[]
        {
            IOPath.Combine(WorkingDirectory, ".env"),                    // Working directory
            IOPath.Combine(WorkingDirectory, "../../.env"),              // Project root from src/GQLServer
            IOPath.Combine(ExecutableDirectory, ".env"),                 // Executable directory
            IOPath.Combine(ExecutableDirectory, "../../../.env"),        // Development path from bin/Debug
            IOPath.Combine(ExecutableDirectory, "../../../../../.env"),  // Project root from bin/Debug/net9.0
            IOPath.Combine(AssemblyDirectory, ".env"),                   // Assembly directory
            ".env"                                                        // Current directory fallback
        };
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Gets the application name
    /// </summary>
    public static string ApplicationName => 
        Assembly.GetExecutingAssembly().GetName().Name ?? "GQLServer";

    /// <summary>
    /// Gets the application version
    /// </summary>
    public static string ApplicationVersion => 
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Display current path information for debugging
    /// </summary>
    public static void DisplayPathInfo()
    {
        Console.WriteLine("Application Path Information:");
        Console.WriteLine($"  Working Directory: {WorkingDirectory}");
        Console.WriteLine($"  Executable Directory: {ExecutableDirectory}");
        Console.WriteLine($"  Assembly Directory: {AssemblyDirectory}");
        Console.WriteLine($"  Data Directory: {DataDirectory}");
        Console.WriteLine($"  Cache Directory: {CacheDirectory}");
        Console.WriteLine($"  Logs Directory: {LogsDirectory}");
    }
}