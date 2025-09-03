using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kanriya.Shared;
using Kanriya.Shared.Services;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class BuildInfoViewModel : ViewModelBase
{
    private readonly LocalizationService _localization;
    
    [ObservableProperty]
    private string _platform = string.Empty;
    
    [ObservableProperty]
    private string _appInfo = "";
    
    [ObservableProperty]
    private string _serverConfig = "";
    
    [ObservableProperty]
    private string _platformInfoHeader = "Platform Information";
    
    [ObservableProperty]
    private string _appInfoHeader = "Application Information";
    
    [ObservableProperty]
    private string _serverConfigHeader = "Server Configuration";
    
    [ObservableProperty]
    private string _currentLanguage = "en";
    
    [ObservableProperty]
    private List<string> _availableLanguages = new();

    public BuildInfoViewModel()
    {
        // Initialize ClientEnvironmentConfig with shared library assembly (contains server config)
        var sharedAssembly = Assembly.GetExecutingAssembly();
        ClientEnvironmentConfig.Initialize(sharedAssembly);
        
        // Initialize localization
        _localization = ClientEnvironmentConfig.Localization.Service;
        _localization.LanguageChanged += OnLanguageChanged;
        
        // Set initial values with localization
        _currentLanguage = _localization.CurrentLanguage;
        _availableLanguages = new List<string>(_localization.SupportedLanguages);
        
        UpdateLocalizedStrings();
        _platform = GetPlatformInfo();
        _appInfo = GetAppInfo();
        _serverConfig = GetServerConfig();
    }
    
    private void OnLanguageChanged(object? sender, string newLanguage)
    {
        CurrentLanguage = newLanguage;
        UpdateLocalizedStrings();
        
        // Update all displayed information with new language
        Platform = GetPlatformInfo();
        AppInfo = GetAppInfo();
        ServerConfig = GetServerConfig();
    }
    
    private void UpdateLocalizedStrings()
    {
        PlatformInfoHeader = _localization["navigation.platformInfo"].Value ?? "Platform Information";
        AppInfoHeader = _localization["navigation.appInfo"].Value ?? "Application Information";
        ServerConfigHeader = _localization["navigation.serverConfig"].Value ?? "Server Configuration";
    }
    
    [RelayCommand]
    private void ChangeLanguage(string language)
    {
        ClientEnvironmentConfig.Localization.SetLanguage(language);
    }

    private string GetPlatformInfo()
    {
        string platformName;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            platformName = $"Windows ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            platformName = $"Linux ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            platformName = $"macOS ({RuntimeInformation.OSArchitecture})";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            platformName = $"FreeBSD ({RuntimeInformation.OSArchitecture})";
        else if (OperatingSystem.IsAndroid())
            platformName = _localization["platform.android"].Value;
        else if (OperatingSystem.IsIOS())
            platformName = _localization["platform.ios"].Value;
        else if (OperatingSystem.IsBrowser())
            platformName = _localization["platform.browser"].Value;
        else
            platformName = $"Unknown ({RuntimeInformation.OSDescription})";
            
        return _localization["platform.info", platformName].Value;
    }

    private string GetAppInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var appName = BuildInfo.GetAppName(assembly);
        var appId = BuildInfo.GetAppId(assembly);
        var version = BuildInfo.GetVersion(assembly);
        var codename = BuildInfo.GetCodename(assembly);
        var buildDate = BuildInfo.GetBuildDate(assembly);
        
        return $"{_localization["app.name"]}: {appName}\n" +
               $"ID: {appId}\n" +
               $"{_localization["platform.version", version]} ({codename})\n" +
               $"{_localization["platform.build", buildDate]}";
    }

    private string GetServerConfig()
    {
        return $"{_localization["server.connection"]}: {ClientEnvironmentConfig.Server.BaseUrl}\n" +
               $"GraphQL: {ClientEnvironmentConfig.Server.GraphQLUrl}\n" +
               $"API: {ClientEnvironmentConfig.Server.ApiBaseUrl}\n" +
               $"WebSocket: {ClientEnvironmentConfig.Server.WebSocketUrl}\n" +
               $"{_localization["platform.environment", ClientEnvironmentConfig.App.Environment]}\n" +
               $"Debug Mode: {ClientEnvironmentConfig.App.Debug}\n" +
               $"{_localization["platform.info", ClientEnvironmentConfig.Platform.Name]}";
    }
}