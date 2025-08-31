using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Kanriya.Client.Avalonia.Browser.Interop;

/// <summary>
/// JavaScript interop for communication between Avalonia and browser
/// </summary>
public static partial class JSInterop
{
    /// <summary>
    /// Send an event from C# to JavaScript
    /// </summary>
    [JSExport]
    public static void OnAvaloniaReady()
    {
        // This can be called from C# and will trigger JavaScript
        SendEventToJS("avaloniaReady", "Avalonia application is ready");
    }

    /// <summary>
    /// Send custom events to JavaScript
    /// </summary>
    [JSExport]
    public static void SendEventToJS(string eventName, string data)
    {
        // This will be received by JavaScript event listeners
    }

    /// <summary>
    /// Receive events from JavaScript
    /// </summary>
    [JSImport("receiveFromJS", "main.js")]
    public static partial void ReceiveFromJS(string eventName, string data);

    /// <summary>
    /// Update loading progress
    /// </summary>
    [JSImport("updateLoadingProgress", "main.js")]
    public static partial void UpdateLoadingProgress(int percentage, string message);

    /// <summary>
    /// Close splash screen from C#
    /// </summary>
    [JSImport("closeSplashScreen", "main.js")]
    public static partial void CloseSplashScreen();

    /// <summary>
    /// Show notification in browser
    /// </summary>
    [JSImport("showNotification", "main.js")]
    public static partial void ShowNotification(string title, string message, string type);
}