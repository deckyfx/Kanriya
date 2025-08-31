using System.Reflection;
using Kanriya.Shared.Services;

namespace Kanriya.Shared;

/// <summary>
/// Client-specific environment configuration that reads from BuildInfo (embedded at build time)
/// This provides a clean way to configure client apps using assembly metadata
/// </summary>
public static class ClientEnvironmentConfig
{
    private static Assembly? _sharedAssembly;
    
    /// <summary>
    /// Initialize client environment configuration with the shared library assembly
    /// Call this at application startup: ClientEnvironmentConfig.Initialize(Assembly.GetExecutingAssembly())
    /// Server config comes from shared library, platform info comes from entry assembly
    /// </summary>
    public static void Initialize(Assembly sharedAssembly)
    {
        _sharedAssembly = sharedAssembly;
    }
    
    private static Assembly GetSharedAssembly()
    {
        return _sharedAssembly ?? Assembly.GetExecutingAssembly();
    }
    
    private static Assembly GetEntryAssembly()
    {
        return Assembly.GetEntryAssembly() ?? GetSharedAssembly();
    }
    
    /// <summary>
    /// Server configuration for client connections (read from BuildInfo)
    /// </summary>
    public static class Server
    {
        /// <summary>
        /// Base server URL (from shared library, patched at build time)
        /// </summary>
        public static string BaseUrl => BuildInfo.GetServerUrl(GetSharedAssembly());
        
        /// <summary>
        /// GraphQL endpoint URL (from shared library, patched at build time)
        /// </summary>
        public static string GraphQLUrl => BuildInfo.GetGraphQLUrl(GetSharedAssembly());
        
        /// <summary>
        /// REST API base URL (from shared library, patched at build time)
        /// </summary>
        public static string ApiBaseUrl => BuildInfo.GetApiBaseUrl(GetSharedAssembly());
        
        /// <summary>
        /// WebSocket URL for subscriptions (from shared library, patched at build time)
        /// </summary>
        public static string WebSocketUrl => BuildInfo.GetWebSocketUrl(GetSharedAssembly());
    }
    
    /// <summary>
    /// Application configuration (read from BuildInfo)
    /// </summary>
    public static class App
    {
        /// <summary>
        /// Application environment (from shared library, patched at build time)
        /// </summary>
        public static string Environment => BuildInfo.GetAppEnvironment(GetSharedAssembly());
        
        /// <summary>
        /// Debug mode (from shared library, patched at build time)
        /// </summary>
        public static bool Debug => BuildInfo.GetDebugMode(GetSharedAssembly());
        
        /// <summary>
        /// Application name (from entry assembly, platform-specific)
        /// </summary>
        public static string Name => BuildInfo.GetAppName(GetEntryAssembly());
        
        /// <summary>
        /// Application ID (from entry assembly, platform-specific)
        /// </summary>
        public static string Id => BuildInfo.GetAppId(GetEntryAssembly());
        
        /// <summary>
        /// Application version (from entry assembly, platform-specific)
        /// </summary>
        public static string Version => BuildInfo.GetVersion(GetEntryAssembly());
    }
    
    /// <summary>
    /// Platform-specific configuration
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Current platform name (detected automatically)
        /// </summary>
        public static string Name => GetPlatformName();
        
        /// <summary>
        /// Platform-specific data directory
        /// </summary>
        public static string DataDirectory => GetPlatformDataDirectory();
        
        /// <summary>
        /// Platform-specific cache directory
        /// </summary>
        public static string CacheDirectory => GetPlatformCacheDirectory();
    }
    
    private static string GetPlatformName()
    {
#if NET9_0_ANDROID
        return "Android";
#elif NET9_0_IOS
        return "iOS";
#elif NET9_0_BROWSER
        return "Browser";
#else
        return "Desktop";
#endif
    }
    
    private static string GetPlatformDataDirectory()
    {
        var appId = App.Id;
        return GetPlatformName() switch
        {
            "Desktop" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kanriya"),
            "Android" => $"/data/data/{appId}/files",
            "iOS" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".kanriya"),
            "Browser" => "/idbfs/kanriya", // IndexedDB virtual path for WASM
            _ => Path.Combine(Environment.CurrentDirectory, "data")
        };
    }
    
    private static string GetPlatformCacheDirectory()
    {
        var appId = App.Id;
        return GetPlatformName() switch
        {
            "Desktop" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kanriya", "Cache"),
            "Android" => $"/data/data/{appId}/cache",
            "iOS" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".kanriya", "cache"),
            "Browser" => "/idbfs/kanriya/cache", // IndexedDB virtual path for WASM
            _ => Path.Combine(Environment.CurrentDirectory, "cache")
        };
    }
    
    /// <summary>
    /// Localization configuration for client applications
    /// </summary>
    public static class Localization
    {
        /// <summary>
        /// Get the shared LocalizationService instance
        /// </summary>
        public static LocalizationService Service => LocalizationService.Instance;
        
        /// <summary>
        /// Get the current language code
        /// </summary>
        public static string CurrentLanguage => Service.CurrentLanguage;
        
        /// <summary>
        /// Set the current language
        /// </summary>
        public static void SetLanguage(string culture) => Service.SetLanguage(culture);
        
        /// <summary>
        /// Get all supported language codes
        /// </summary>
        public static IEnumerable<string> SupportedLanguages => Service.SupportedLanguages;
        
        /// <summary>
        /// Node.js-like t() function for getting translations
        /// </summary>
        public static string t(string key, params object[] args) => Service.t(key, args);
    }
}