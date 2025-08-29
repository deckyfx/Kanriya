using System.CommandLine;
using System.Reflection;
using Kanriya.Client.Builder.Utils;
using Kanriya.Shared;
using Spectre.Console;

namespace Kanriya.Client.Builder;

public class Program
{
    private static readonly BuilderManager BuilderManager = new();
    
    public static async Task<int> Main(string[] args)
    {
        ShowBanner();
        
        if (args.Length == 0)
        {
            return await ShowBuildMenuDirectly();
        }
        
        var rootCommand = new RootCommand("Kanriya Build System - Cross-platform build automation");
        
        rootCommand.AddCommand(CreateBuildCommand());
        rootCommand.AddCommand(CreateServeCommand());
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static void ShowBanner()
    {
        var assembly = Assembly.GetExecutingAssembly();
        BannerUtils.DisplayFancyBanner(assembly, "Cross-platform build automation for Kanriya Client", Color.Green);
    }
    
    private static async Task<int> ShowBuildMenuDirectly()
    {
        while (true)
        {
            try
            {
                var result = await HandleBuildMenu();
                if (result == 0) // Exit was selected
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            
            AnsiConsole.WriteLine();
        }
    }
    
    private static async Task<int> HandleBuildMenu()
    {
        var availablePlatforms = BuilderManager.GetAllBuilders()
            .Select(b => $"{GetPlatformEmoji(b.Aliases[0])} {b.PlatformName}")
            .ToList();
        
        availablePlatforms.Add("🚀 All platforms");
        availablePlatforms.Add("❌ Exit");
        
        var platform = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("🎯 [bold]Select platform to build:[/]")
                .AddChoices(availablePlatforms));
        
        if (platform.StartsWith("❌"))
        {
            AnsiConsole.MarkupLine("[dim]Goodbye! 👋[/]");
            return 0;
        }
        
        var publish = false;
        
        // Special handling for iOS
        if (platform.Contains("iOS"))
        {
            var iosOptions = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("📱 [bold]iOS build type:[/]")
                    .AddChoices(
                        "📱 Simulator (free, no signing)",
                        "🏭 Device (requires Apple Developer cert)"));
            
            publish = iosOptions.Contains("Device");
        }
        
        var platformKey = ExtractPlatformKey(platform);
        
        // First, do the build with progress
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]Building {platformKey}...[/]");
                
                if (platformKey == "all")
                {
                    await BuildAllPlatforms(task);
                }
                else
                {
                    await BuilderManager.BuildPlatformAsync(platformKey, publish, false, false, task);
                }
            });
        
        // Then ask about launching
        if (platformKey != "all")
        {
            if (AnsiConsole.Confirm("🚀 [bold]Launch the app now?[/]", false))
            {
                await BuilderManager.LaunchPlatformAsync(platformKey, publish);
            }
        }
        
        return 1; // Continue
    }
    
    private static async Task BuildAllPlatforms(ProgressTask task)
    {
        AnsiConsole.MarkupLine("🚀 [bold]Building all platforms...[/]");
        
        var builders = BuilderManager.GetAllBuilders().ToList();
        var totalBuilders = builders.Count;
        var increment = 100.0 / totalBuilders;
        
        foreach (var builder in builders)
        {
            if (await builder.CanBuildAsync())
            {
                AnsiConsole.MarkupLine($"🔨 [blue]Building {builder.PlatformName}...[/]");
                await BuilderManager.BuildPlatformAsync(builder.Aliases[0], false, false, false);
                task.Increment(increment);
            }
            else
            {
                AnsiConsole.MarkupLine($"⏭️ [yellow]Skipping {builder.PlatformName} (not supported on this platform)[/]");
                task.Increment(increment);
            }
        }
        
        AnsiConsole.MarkupLine("✅ [green]All platform builds complete![/]");
    }
    
    
    private static Command CreateBuildCommand()
    {
        var buildCommand = new Command("build", "Build for specific platform");
        
        var platformArg = new Argument<string>("platform", "Target platform");
        var publishOpt = new Option<bool>("--publish", "Publish for real devices (iOS only)");
        var launchOpt = new Option<bool>("--launch", "Launch after build");
        var skipZipOpt = new Option<bool>("--skip-zip", "Skip creating zip files");
        
        buildCommand.AddArgument(platformArg);
        buildCommand.AddOption(publishOpt);
        buildCommand.AddOption(launchOpt);
        buildCommand.AddOption(skipZipOpt);
        
        buildCommand.SetHandler(async (platform, publish, launch, skipZip) =>
        {
            ShowBanner();
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[green]Building {platform}...[/]");
                    await BuilderManager.BuildPlatformAsync(platform, publish, false, false, task);
                });
            
            if (launch)
            {
                await BuilderManager.LaunchPlatformAsync(platform, publish);
            }
        }, platformArg, publishOpt, launchOpt, skipZipOpt);
        
        return buildCommand;
    }
    
    private static Command CreateServeCommand()
    {
        var serveCommand = new Command("serve", "Start local HTTP server for web builds");
        serveCommand.SetHandler(async () =>
        {
            ShowBanner();
            await BuilderManager.LaunchPlatformAsync("web");
        });
        
        return serveCommand;
    }
    
    
    private static string ExtractPlatformKey(string platform)
    {
        return platform switch
        {
            var p when p.Contains("Windows") => "windows",
            var p when p.Contains("Linux") => "linux", 
            var p when p.Contains("macOS") => "macos",
            var p when p.Contains("Web") => "web",
            var p when p.Contains("Android") => "android",
            var p when p.Contains("iOS") => "ios",
            var p when p.Contains("All") => "all",
            _ => platform.ToLower()
        };
    }
    
    private static string GetPlatformEmoji(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "windows" or "win" => "🪟",
            "linux" => "🐧",
            "macos" or "mac" or "osx" => "🍎",
            "web" or "browser" or "wasm" => "🌐",
            "android" or "apk" => "🤖",
            "ios" or "iphone" or "ipad" => "📱",
            _ => "🔧"
        };
    }
    
}