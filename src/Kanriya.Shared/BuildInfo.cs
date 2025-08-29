using System.Reflection;

namespace Kanriya.Shared;

/// <summary>
/// Build and version information reader for any assembly
/// Provides reusable code to read build metadata from any project
/// </summary>
public static class BuildInfo
{
    /// <summary>
    /// Get application name from specified assembly
    /// </summary>
    public static string GetAppName(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "AppName") ?? "Unknown Application";
    
    /// <summary>
    /// Get application identifier from specified assembly
    /// </summary>
    public static string GetAppId(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "AppId") ?? "unknown.app";
    
    /// <summary>
    /// Get version from specified assembly
    /// </summary>
    public static string GetVersion(Assembly assembly) => 
        assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    
    /// <summary>
    /// Get codename from specified assembly
    /// </summary>
    public static string GetCodename(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "Codename") ?? "Unknown";
    
    /// <summary>
    /// Get build date from specified assembly
    /// </summary>
    public static string GetBuildDate(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "BuildDate") ?? "Unknown";

    /// <summary>
    /// Get server URL from specified assembly (embedded at build time for clients)
    /// </summary>
    public static string GetServerUrl(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "ServerUrl") ?? "http://localhost:5000";

    /// <summary>
    /// Get GraphQL endpoint from specified assembly (embedded at build time for clients)
    /// </summary>
    public static string GetGraphQLUrl(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "GraphQLUrl") ?? $"{GetServerUrl(assembly)}/graphql";

    /// <summary>
    /// Get API base URL from specified assembly (embedded at build time for clients)
    /// </summary>
    public static string GetApiBaseUrl(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "ApiBaseUrl") ?? $"{GetServerUrl(assembly)}/api";

    /// <summary>
    /// Get WebSocket URL from specified assembly (embedded at build time for clients)
    /// </summary>
    public static string GetWebSocketUrl(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "WebSocketUrl") ?? GetGraphQLUrl(assembly).Replace("http://", "ws://").Replace("https://", "wss://");

    /// <summary>
    /// Get application environment from specified assembly (embedded at build time for clients)
    /// </summary>
    public static string GetAppEnvironment(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "AppEnvironment") ?? "Development";

    /// <summary>
    /// Get debug mode from specified assembly (embedded at build time for clients)
    /// </summary>
    public static bool GetDebugMode(Assembly assembly) => 
        GetAssemblyMetadata(assembly, "DebugMode")?.ToLower() == "true";
    
    /// <summary>
    /// Get informational version from specified assembly
    /// </summary>
    public static string GetInformationalVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? 
        GetVersion(assembly);
    
    /// <summary>
    /// Get full version string with all details from specified assembly
    /// </summary>
    public static string GetFullVersion(Assembly assembly)
    {
        var version = GetVersion(assembly);
        var codename = GetCodename(assembly);
        var buildDate = GetBuildDate(assembly);
        return $"v{version} ({codename}) - {buildDate}";
    }
    
    /// <summary>
    /// Get short version string from specified assembly
    /// </summary>
    public static string GetShortVersion(Assembly assembly) => $"v{GetVersion(assembly)}";
    
    /// <summary>
    /// Get assembly metadata value by key from specified assembly
    /// </summary>
    public static string? GetAssemblyMetadata(Assembly assembly, string key)
    {
        var metadataAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        return metadataAttributes.FirstOrDefault(attr => attr.Key == key)?.Value;
    }
}