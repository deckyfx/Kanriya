using Kanriya.Client.Builder.Builders;
using Kanriya.Client.Builder.Hooks;
using Spectre.Console;

namespace Kanriya.Client.Builder.Utils;

public class BuilderManager
{
    private readonly List<IPlatformBuilder> _builders = new();
    private readonly BuildHookManager _hookManager = new();
    
    public BuilderManager()
    {
        RegisterBuilders();
        RegisterDefaultHooks();
    }
    
    private void RegisterBuilders()
    {
        _builders.Add(new WebBuilder());
        _builders.Add(new WindowsBuilder());
        _builders.Add(new LinuxBuilder());
        _builders.Add(new MacOSBuilder());
        _builders.Add(new AndroidBuilder());
        _builders.Add(new IOSBuilder());
    }
    
    private void RegisterDefaultHooks()
    {
        _hookManager.RegisterPreBuildHook(new ValidateEnvironmentHook());
        _hookManager.RegisterPreBuildHook(new CleanOutputHook());
        _hookManager.RegisterPostBuildHook(new BuildSummaryHook());
    }
    
    public IPlatformBuilder? FindBuilder(string platform)
    {
        platform = platform.ToLowerInvariant();
        
        return _builders.FirstOrDefault(b => 
            b.Aliases.Any(alias => alias.Equals(platform, StringComparison.OrdinalIgnoreCase)));
    }
    
    public IEnumerable<IPlatformBuilder> GetAllBuilders()
    {
        return _builders;
    }
    
    public async Task BuildPlatformAsync(string platform, bool publish, bool launch, bool skipZip, ProgressTask? task = null)
    {
        var builder = FindBuilder(platform);
        if (builder == null)
        {
            AnsiConsole.MarkupLine($"[red]❌ Unknown platform: {platform}[/]");
            AnsiConsole.MarkupLine("[yellow]Available platforms:[/]");
            foreach (var b in _builders)
            {
                AnsiConsole.MarkupLine($"  • {string.Join(", ", b.Aliases)} - {b.PlatformName}");
            }
            return;
        }
        
        if (!await builder.CanBuildAsync())
        {
            AnsiConsole.MarkupLine($"[red]❌ Cannot build {builder.PlatformName} on this platform[/]");
            return;
        }
        
        // Create build context
        var context = new BuildContext
        {
            Platform = platform,
            Launch = launch,
            SkipZip = skipZip,
            Publish = publish,
            OutputDirectory = Path.Combine(BasePlatformBuilderExtensions.PublishDir, GetOutputDirectoryName(platform))
        };
        
        try
        {
            // Execute pre-build hooks
            await _hookManager.ExecutePreBuildHooksAsync(context);
            
            // Build the platform
            await builder.BuildAsync(launch, skipZip, task);
            
            // Execute post-build hooks
            await _hookManager.ExecutePostBuildHooksAsync(context);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Build failed: {ex.Message}[/]");
            throw;
        }
    }
    
    public async Task LaunchPlatformAsync(string platform, bool publish = false)
    {
        var builder = FindBuilder(platform);
        if (builder == null)
        {
            AnsiConsole.MarkupLine($"[red]❌ Unknown platform: {platform}[/]");
            return;
        }
        
        if (!await builder.CanLaunchAsync())
        {
            AnsiConsole.MarkupLine($"[red]❌ Cannot launch {builder.PlatformName} on this platform[/]");
            return;
        }
        
        await builder.LaunchAsync(publish);
    }
    
    public void RegisterPreBuildHook(IBuildHook hook)
    {
        _hookManager.RegisterPreBuildHook(hook);
    }
    
    public void RegisterPostBuildHook(IBuildHook hook)
    {
        _hookManager.RegisterPostBuildHook(hook);
    }
    
    private static string GetOutputDirectoryName(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "windows" or "win" => "win-x64",
            "linux" => "linux-x64",
            "macos" or "mac" or "osx" => "osx-arm64",
            "web" or "browser" or "wasm" => "web",
            "android" or "apk" => "android",
            "ios" or "iphone" or "ipad" => "ios-simulator",
            _ => platform
        };
    }
}

// Extension to expose static members from BasePlatformBuilder
public static class BasePlatformBuilderExtensions
{
    public static string PublishDir => GetPublishDir();
    
    private static string GetPublishDir()
    {
        var projectRoot = GetProjectRoot();
        return Path.Combine(projectRoot, "publish", "client");
    }
    
    private static string GetProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Kanriya.sln")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root");
    }
}