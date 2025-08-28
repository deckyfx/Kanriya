using System;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kanriya.Client.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Kanriya Client - Built with Avalonia!";
    
    [ObservableProperty]
    private string _platform;

    public MainViewModel()
    {
        _platform = GetPlatformInfo();
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
}
