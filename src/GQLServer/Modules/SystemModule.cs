using GQLServer.Program;
using GQLServer.Queries;

namespace GQLServer.Modules;

/// <summary>
/// GraphQL module for System domain
/// Contains all queries related to system information and health
/// </summary>
[ExtendObjectType(typeof(RootQuery))]
public class SystemQueries
{
    /// <summary>
    /// Get the current server version information
    /// </summary>
    [GraphQLName("version")]
    public VersionInfo GetVersion()
    {
        return new VersionInfo
        {
            Version = AppVersion.GetShortVersion(),
            Codename = AppVersion.Codename,
            BuildDate = AppVersion.BuildDate,
            FullVersion = AppVersion.GetFullVersion()
        };
    }
    
    /// <summary>
    /// Get the current health status of the server
    /// </summary>
    [GraphQLName("health")]
    public HealthStatus GetHealth()
    {
        return new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = AppVersion.GetFullVersion()
        };
    }
}

/// <summary>
/// Version information about the GraphQL server
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// The semantic version number (e.g., "1.1.0")
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// The codename of this version
    /// </summary>
    public string Codename { get; set; } = string.Empty;
    
    /// <summary>
    /// The build date
    /// </summary>
    public string BuildDate { get; set; } = string.Empty;
    
    /// <summary>
    /// The full version string including codename and date
    /// </summary>
    public string FullVersion { get; set; } = string.Empty;
}

/// <summary>
/// Health status of the GraphQL server
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// The current status of the server
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// The timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// The server version
    /// </summary>
    public string Version { get; set; } = string.Empty;
}