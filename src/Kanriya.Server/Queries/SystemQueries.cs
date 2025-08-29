using Kanriya.Shared;
using System.Reflection;

namespace Kanriya.Server.Queries;

/// <summary>
/// System-level queries for server information and health checks
/// </summary>
[ExtendObjectType(typeof(RootQuery))]
public class SystemQueries
{
    /// <summary>
    /// Get the current server version information
    /// </summary>
    /// <returns>Version information object</returns>
    public VersionInfo GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return new VersionInfo
        {
            Version = BuildInfo.GetVersion(assembly),
            Codename = BuildInfo.GetCodename(assembly),
            BuildDate = BuildInfo.GetBuildDate(assembly),
            FullVersion = BuildInfo.GetFullVersion(assembly)
        };
    }
    
    /// <summary>
    /// Simple health check endpoint
    /// </summary>
    /// <returns>Server status</returns>
    public HealthStatus GetHealth()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return new HealthStatus
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = BuildInfo.GetShortVersion(assembly)
        };
    }
}

/// <summary>
/// Version information response type
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Semantic version number
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Version codename
    /// </summary>
    public string Codename { get; set; } = string.Empty;
    
    /// <summary>
    /// Build date
    /// </summary>
    public string BuildDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Full version string
    /// </summary>
    public string FullVersion { get; set; } = string.Empty;
}

/// <summary>
/// Health status response type
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// Current server status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Current timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Server version
    /// </summary>
    public string Version { get; set; } = string.Empty;
}