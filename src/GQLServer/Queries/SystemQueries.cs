using GQLServer.Program;

namespace GQLServer.Queries;

/// <summary>
/// System-level queries for server information and health checks
/// </summary>
public class SystemQueries
{
    /// <summary>
    /// Get the current server version information
    /// </summary>
    /// <returns>Version information object</returns>
    public VersionInfo GetVersion()
    {
        return new VersionInfo
        {
            Version = AppVersion.Version,
            Codename = AppVersion.Codename,
            BuildDate = AppVersion.BuildDate,
            FullVersion = AppVersion.GetFullVersion()
        };
    }
    
    /// <summary>
    /// Simple health check endpoint
    /// </summary>
    /// <returns>Server status</returns>
    public HealthStatus GetHealth()
    {
        return new HealthStatus
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = AppVersion.GetShortVersion()
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