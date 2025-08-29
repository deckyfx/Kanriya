using System.Reflection;
using Spectre.Console;

namespace Kanriya.Shared;

/// <summary>
/// Utility class for displaying application banners and metadata
/// Provides consistent banner formatting across all subprojects
/// </summary>
public static class BannerUtils
{
    /// <summary>
    /// Display a complete application banner with metadata
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="additionalInfo">Optional additional information to display</param>
    public static void DisplayAppBanner(Assembly assembly, string? additionalInfo = null)
    {
        var appName = BuildInfo.GetAppName(assembly);
        var version = BuildInfo.GetFullVersion(assembly);
        
        Console.WriteLine();
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine($"{appName.ToUpper()}");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine($"Version: {version}");
        Console.WriteLine($"App ID: {BuildInfo.GetAppId(assembly)}");
        Console.WriteLine($"Codename: {BuildInfo.GetCodename(assembly)}");
        Console.WriteLine($"Build Date: {BuildInfo.GetBuildDate(assembly)}");
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            Console.WriteLine();
            Console.WriteLine(additionalInfo);
        }
        
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine();
    }

    /// <summary>
    /// Display a simple banner with just app name and version
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    public static void DisplaySimpleBanner(Assembly assembly)
    {
        var appName = BuildInfo.GetAppName(assembly);
        var version = BuildInfo.GetVersion(assembly);
        
        Console.WriteLine($"{appName} v{version}");
    }

    /// <summary>
    /// Display application metadata in a structured format
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    public static void DisplayAppInfo(Assembly assembly)
    {
        Console.WriteLine("Application Information:");
        Console.WriteLine($"  Name: {BuildInfo.GetAppName(assembly)}");
        Console.WriteLine($"  ID: {BuildInfo.GetAppId(assembly)}");
        Console.WriteLine($"  Version: {BuildInfo.GetFullVersion(assembly)}");
        Console.WriteLine($"  Codename: {BuildInfo.GetCodename(assembly)}");
        Console.WriteLine($"  Build Date: {BuildInfo.GetBuildDate(assembly)}");
    }

    /// <summary>
    /// Create a formatted banner string without displaying it
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="additionalInfo">Optional additional information to include</param>
    /// <returns>Formatted banner string</returns>
    public static string CreateBannerString(Assembly assembly, string? additionalInfo = null)
    {
        var appName = BuildInfo.GetAppName(assembly);
        var version = BuildInfo.GetFullVersion(assembly);
        var lines = new List<string>
        {
            "",
            "=".PadRight(50, '='),
            appName.ToUpper(),
            "=".PadRight(50, '='),
            $"Version: {version}",
            $"App ID: {BuildInfo.GetAppId(assembly)}",
            $"Codename: {BuildInfo.GetCodename(assembly)}",
            $"Build Date: {BuildInfo.GetBuildDate(assembly)}"
        };
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            lines.Add("");
            lines.Add(additionalInfo);
        }
        
        lines.Add("=".PadRight(50, '='));
        lines.Add("");
        
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Display startup information with environment details
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="environmentInfo">Environment-specific information to display</param>
    public static void DisplayStartupInfo(Assembly assembly, string? environmentInfo = null)
    {
        DisplayAppBanner(assembly);
        
        Console.WriteLine("Startup Information:");
        Console.WriteLine($"  Platform: {Environment.OSVersion.Platform}");
        Console.WriteLine($"  Runtime: {Environment.Version}");
        Console.WriteLine($"  Working Directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"  Machine Name: {Environment.MachineName}");
        Console.WriteLine($"  User: {Environment.UserName}");
        Console.WriteLine($"  Process ID: {Environment.ProcessId}");
        
        if (!string.IsNullOrEmpty(environmentInfo))
        {
            Console.WriteLine();
            Console.WriteLine("Environment:");
            Console.WriteLine($"  {environmentInfo}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Get application metadata as key-value pairs for external display libraries
    /// Useful for Spectre.Console or other UI frameworks
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <returns>Dictionary of metadata key-value pairs</returns>
    public static Dictionary<string, string> GetAppMetadata(Assembly assembly)
    {
        return new Dictionary<string, string>
        {
            ["Name"] = BuildInfo.GetAppName(assembly),
            ["ID"] = BuildInfo.GetAppId(assembly),
            ["Version"] = BuildInfo.GetFullVersion(assembly),
            ["Short Version"] = BuildInfo.GetVersion(assembly),
            ["Codename"] = BuildInfo.GetCodename(assembly),
            ["Build Date"] = BuildInfo.GetBuildDate(assembly)
        };
    }

    /// <summary>
    /// Display application metadata after a custom banner
    /// Useful when you want custom banner styling but standard metadata display
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    public static void DisplayMetadataOnly(Assembly assembly)
    {
        var metadata = GetAppMetadata(assembly);
        Console.WriteLine("Application Information:");
        foreach (var kvp in metadata)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Display a fancy ASCII art banner using Spectre.Console with Figlet text
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="additionalInfo">Optional additional information to display</param>
    /// <param name="color">Color for the ASCII art (default: Blue)</param>
    public static void DisplayFancyBanner(Assembly assembly, string? additionalInfo = null, Color? color = null)
    {
        var appName = BuildInfo.GetAppName(assembly);
        var version = BuildInfo.GetFullVersion(assembly);
        var bannerColor = color ?? Color.Blue;
        
        AnsiConsole.Clear();
        
        // Create fancy ASCII art title
        AnsiConsole.Write(
            new FigletText(appName)
                .Centered()
                .Color(bannerColor)
        );
        
        // Version and metadata in a nice panel
        var metadata = GetAppMetadata(assembly);
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow($"[bold blue]Version:[/]", $"[blue]{metadata["Version"]}[/]");
        grid.AddRow($"[bold blue]App ID:[/]", $"[blue]{metadata["ID"]}[/]");
        grid.AddRow($"[bold blue]Codename:[/]", $"[blue]{metadata["Codename"]}[/]");
        grid.AddRow($"[bold blue]Build Date:[/]", $"[blue]{metadata["Build Date"]}[/]");
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            grid.AddRow("", "");
            grid.AddRow($"[bold yellow]Description:[/]", $"[yellow]{additionalInfo}[/]");
        }
        
        var panel = new Panel(grid)
            .Header($"[bold blue]Application Information[/]")
            .Border(BoxBorder.Double)
            .BorderColor(bannerColor);
            
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display a compact fancy banner with smaller ASCII art
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="color">Color for the display (default: Green)</param>
    public static void DisplayCompactFancyBanner(Assembly assembly, Color? color = null)
    {
        var appName = BuildInfo.GetAppName(assembly);
        var version = BuildInfo.GetVersion(assembly);
        var bannerColor = color ?? Color.Green;
        
        // Create a rule with app name
        var rule = new Rule($"[bold green]{appName.ToUpper()} v{version}[/]")
            .RuleStyle(Style.Parse("green"))
            .LeftJustified();
            
        AnsiConsole.Write(rule);
        
        // Compact metadata
        var appId = BuildInfo.GetAppId(assembly);
        var codename = BuildInfo.GetCodename(assembly);
        
        AnsiConsole.MarkupLine($"[dim]ID: {appId} | Codename: {codename}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display fancy metadata panel using Spectre.Console
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from</param>
    /// <param name="title">Panel title (default: "Application Information")</param>
    /// <param name="color">Panel color (default: Cyan)</param>
    public static void DisplayFancyMetadata(Assembly assembly, string? title = null, Color? color = null)
    {
        var metadata = GetAppMetadata(assembly);
        var panelColor = color ?? Color.Cyan1;
        var panelTitle = title ?? "Application Information";
        
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        foreach (var kvp in metadata)
        {
            grid.AddRow($"[bold cyan]{kvp.Key}:[/]", $"[cyan]{kvp.Value}[/]");
        }
        
        var panel = new Panel(grid)
            .Header($"[bold cyan]{panelTitle}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(panelColor);
            
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
}