namespace GQLServer.Program;

/// <summary>
/// Application version information
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Current version of the GraphQL server
    /// Follow Semantic Versioning: MAJOR.MINOR.PATCH
    /// </summary>
    public const string Version = "1.1.0";
    
    /// <summary>
    /// Version codename for major releases
    /// </summary>
    public const string Codename = "GreetLog";
    
    /// <summary>
    /// Build date (update when releasing)
    /// </summary>
    public const string BuildDate = "2025-08-22";
    
    /// <summary>
    /// Get full version string with all details
    /// </summary>
    public static string GetFullVersion() => $"v{Version} ({Codename}) - {BuildDate}";
    
    /// <summary>
    /// Get short version string
    /// </summary>
    public static string GetShortVersion() => $"v{Version}";
}