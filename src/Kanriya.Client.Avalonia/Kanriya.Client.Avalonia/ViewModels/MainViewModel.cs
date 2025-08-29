using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Kanriya.Shared;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Kanriya Client - Built with Avalonia!";
    
    [ObservableProperty]
    private string _platform;
    
    [ObservableProperty]
    private string _appInfo = "";
    
    [ObservableProperty]
    private string _serverConfig = "";

    public MainViewModel()
    {
        // Initialize ClientEnvironmentConfig with shared library assembly (contains server config)
        var sharedAssembly = Assembly.GetExecutingAssembly();
        ClientEnvironmentConfig.Initialize(sharedAssembly);
        
        _platform = GetPlatformInfo();
        _appInfo = GetAppInfo();
        _serverConfig = GetServerConfig();
    }

    private string GetPlatformInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"Windows ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"Linux ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"macOS ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return $"FreeBSD ({RuntimeInformation.OSArchitecture})";
        else if (OperatingSystem.IsAndroid())
            return "Android";
        else if (OperatingSystem.IsIOS())
            return "iOS";
        else if (OperatingSystem.IsBrowser())
            return "Web Browser (WASM)";
        else
            return $"Unknown ({RuntimeInformation.OSDescription})";
    }

    private string GetAppInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var appName = BuildInfo.GetAppName(assembly);
        var appId = BuildInfo.GetAppId(assembly);
        var version = BuildInfo.GetVersion(assembly);
        var codename = BuildInfo.GetCodename(assembly);
        var buildDate = BuildInfo.GetBuildDate(assembly);
        
        return $"App: {appName}\n" +
               $"ID: {appId}\n" +
               $"Version: {version} ({codename})\n" +
               $"Built: {buildDate}";
    }

    private string GetServerConfig()
    {
        return $"Server: {ClientEnvironmentConfig.Server.BaseUrl}\n" +
               $"GraphQL: {ClientEnvironmentConfig.Server.GraphQLUrl}\n" +
               $"API: {ClientEnvironmentConfig.Server.ApiBaseUrl}\n" +
               $"WebSocket: {ClientEnvironmentConfig.Server.WebSocketUrl}\n" +
               $"Environment: {ClientEnvironmentConfig.App.Environment}\n" +
               $"Debug Mode: {ClientEnvironmentConfig.App.Debug}\n" +
               $"Platform: {ClientEnvironmentConfig.Platform.Name}";
    }
    
}
