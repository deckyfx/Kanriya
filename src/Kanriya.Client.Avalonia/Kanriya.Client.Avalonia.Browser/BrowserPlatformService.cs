using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Kanriya.Client.Avalonia.Browser.Interop;

namespace Kanriya.Client.Avalonia.Browser;

/// <summary>
/// Browser-specific platform service for WebAssembly
/// </summary>
[SupportedOSPlatform("browser")]
public class BrowserPlatformService
{
    private static BrowserPlatformService? _instance;
    
    public static BrowserPlatformService Instance => _instance ??= new BrowserPlatformService();
    
    private BrowserPlatformService()
    {
        // Initialize browser-specific features
    }
    
    /// <summary>
    /// Send application ready event to JavaScript
    /// </summary>
    public void NotifyApplicationReady()
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                JSInterop.SendEventToJS("avaloniaReady", "Application initialized successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to notify JS: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Update loading progress in the splash screen
    /// </summary>
    public void UpdateProgress(int percentage, string message)
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                JSInterop.UpdateLoadingProgress(percentage, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update progress: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Show a browser notification
    /// </summary>
    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                JSInterop.ShowNotification(title, message, type.ToString().ToLower());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show notification: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Send custom event to JavaScript
    /// </summary>
    public void SendCustomEvent(string eventName, string data)
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                JSInterop.SendEventToJS(eventName, data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send event: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Close the splash screen programmatically
    /// </summary>
    public void CloseSplashScreen()
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                JSInterop.CloseSplashScreen();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to close splash screen: {ex.Message}");
        }
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}