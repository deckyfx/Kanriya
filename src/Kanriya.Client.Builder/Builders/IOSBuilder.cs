using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class IOSBuilder : BasePlatformBuilder
{
    public override string PlatformName => "iOS Simulator";
    public override string[] Aliases => new[] { "ios", "iphone", "ipad" };
    
    public override async Task<bool> CanBuildAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return false;
        }
        
        return await CheckiOSWorkload();
    }
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building iOS Simulator...[/]");
        task?.Increment(10);
        
        if (!await CanBuildAsync())
        {
            AnsiConsole.MarkupLine("[red]❌ iOS build not available on this platform or iOS workload not installed.[/]");
            AnsiConsole.MarkupLine("[yellow]Run: dotnet workload install maui-ios[/]");
            return;
        }
        
        task?.Increment(10);
        
        var outputDir = Path.Combine(PublishDir, "ios-simulator");
        
        // For iOS Simulator, we use build instead of publish
        await RunDotnetCommand($"build \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.iOS", "Kanriya.Client.Avalonia.iOS.csproj")}\" " +
            $"-c Release -f net9.0-ios " +
            $"-p:RuntimeIdentifier=iossimulator-x64 -p:CodesignKey=\"\" -p:CodesignProvision=\"\" " +
            $"-p:EnableCodeSigning=false -p:_RequireCodeSigning=false");
        
        task?.Increment(60);
        
        // Find the .app bundle in the build output
        var buildOutputDir = Path.Combine(ClientProject, "Kanriya.Client.Avalonia.iOS", "bin", "Release", "net9.0-ios", "iossimulator-x64");
        var appPath = FindiOSApp(buildOutputDir);
        
        if (appPath != null)
        {
            // Copy the .app bundle to our output directory
            var appName = Path.GetFileName(appPath);
            var destinationPath = Path.Combine(outputDir, appName);
            
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            
            CopyDirectory(appPath, destinationPath);
            
            AnsiConsole.MarkupLine($"✅ [green]iOS Simulator app ready: {destinationPath}[/]");
            task?.Increment(100 - (task?.Value ?? 0));
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ iOS app built but .app bundle not found[/]");
            AnsiConsole.MarkupLine($"[dim]Searched in: {buildOutputDir}[/]");
            task?.Increment(100 - (task?.Value ?? 0));
        }
    }
    
    public override Task<bool> CanLaunchAsync()
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var outputDir = Path.Combine(PublishDir, "ios-simulator");
        var appPath = FindiOSApp(outputDir);
        
        if (appPath == null)
        {
            AnsiConsole.MarkupLine("[red]❌ iOS app not found. Build iOS platform first.[/]");
            return;
        }
        
        if (!await CanLaunchAsync())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ iOS launch only available on macOS[/]");
            return;
        }
        
        if (AnsiConsole.Confirm("📱 [bold]Launch in iOS Simulator?[/]", true))
        {
            await LaunchiOSApp(appPath);
        }
    }
    
    private static async Task<bool> CheckiOSWorkload()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "workload list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return output.Contains("maui-ios") || output.Contains("ios");
        }
        catch
        {
            return false;
        }
    }
    
    private static string? FindiOSApp(string outputDir)
    {
        try
        {
            if (Directory.Exists(outputDir))
            {
                var appDirs = Directory.GetDirectories(outputDir, "*.app", SearchOption.AllDirectories);
                return appDirs.FirstOrDefault();
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private static async Task LaunchiOSApp(string appPath)
    {
        try
        {
            // First, check for booted simulators
            var bootedSimulators = await GetBootedSimulators();
            string selectedSimulator;
            
            if (bootedSimulators.Any())
            {
                // Use already booted simulator
                if (bootedSimulators.Count == 1)
                {
                    selectedSimulator = bootedSimulators.First().Key;
                    AnsiConsole.MarkupLine($"📱 [blue]Using booted simulator: {bootedSimulators.First().Value}[/]");
                }
                else
                {
                    var choices = bootedSimulators.Select(s => $"{s.Value} ({s.Key})").ToList();
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("📱 [bold]Select booted iOS Simulator:[/]")
                            .AddChoices(choices));
                    
                    selectedSimulator = bootedSimulators.First(s => choice.Contains(s.Key)).Key;
                }
            }
            else
            {
                // No booted simulators, get all available simulators
                var allSimulators = await GetAllSimulators();
                
                if (!allSimulators.Any())
                {
                    AnsiConsole.MarkupLine("[red]❌ No iOS simulators found[/]");
                    AnsiConsole.MarkupLine("[yellow]Install Xcode and create simulators first[/]");
                    return;
                }
                
                AnsiConsole.MarkupLine("[yellow]⚠️ No simulators are currently booted[/]");
                
                // Let user choose which simulator to boot
                if (allSimulators.Count == 1)
                {
                    selectedSimulator = allSimulators.First().Key;
                    AnsiConsole.MarkupLine($"📱 [blue]Booting simulator: {allSimulators.First().Value}[/]");
                }
                else
                {
                    var choices = allSimulators.Select(s => $"{s.Value} ({s.Key})").ToList();
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("📱 [bold]Select iOS Simulator to boot:[/]")
                            .AddChoices(choices));
                    
                    selectedSimulator = allSimulators.First(s => choice.Contains(s.Key)).Key;
                }
                
                // Boot the selected simulator
                AnsiConsole.MarkupLine("🚀 [blue]Starting iOS Simulator...[/]");
                await RunSimulatorCommand($"boot {selectedSimulator}");
                
                // Give simulator time to boot
                AnsiConsole.MarkupLine("[dim]Waiting for simulator to boot...[/]");
                await Task.Delay(5000);
            }
            
            // Install and launch app
            AnsiConsole.MarkupLine("📦 [blue]Installing app...[/]");
            await RunSimulatorCommand($"install {selectedSimulator} \"{appPath}\"");
            
            // Extract bundle identifier from Info.plist
            var bundleId = ExtractBundleIdentifier(appPath);
            if (!string.IsNullOrEmpty(bundleId))
            {
                AnsiConsole.MarkupLine("🚀 [blue]Launching app...[/]");
                await RunSimulatorCommand($"launch {selectedSimulator} {bundleId}");
                AnsiConsole.MarkupLine("✅ [green]App launched in iOS Simulator![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ Could not determine bundle identifier[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not launch iOS app: {ex.Message}[/]");
        }
    }
    
    private static async Task<Dictionary<string, string>> GetBootedSimulators()
    {
        return await GetSimulatorsByState("Booted");
    }
    
    private static async Task<Dictionary<string, string>> GetAllSimulators()
    {
        return await GetSimulatorsByState(null); // null means all states
    }
    
    private static async Task<Dictionary<string, string>> GetAvailableSimulators()
    {
        return await GetSimulatorsByState("Shutdown,Booted"); // Both shutdown and booted
    }
    
    private static async Task<Dictionary<string, string>> GetSimulatorsByState(string? stateFilter)
    {
        var simulators = new Dictionary<string, string>();
        
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xcrun",
                    Arguments = "simctl list devices --json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            // Parse JSON properly using System.Text.Json
            using var document = System.Text.Json.JsonDocument.Parse(output);
            var root = document.RootElement;
            
            if (root.TryGetProperty("devices", out var devices))
            {
                foreach (var runtime in devices.EnumerateObject())
                {
                    foreach (var device in runtime.Value.EnumerateArray())
                    {
                        if (device.TryGetProperty("name", out var nameElement) &&
                            device.TryGetProperty("udid", out var udidElement) &&
                            device.TryGetProperty("state", out var stateElement) &&
                            device.TryGetProperty("isAvailable", out var availableElement) &&
                            availableElement.GetBoolean())
                        {
                            var name = nameElement.GetString();
                            var udid = udidElement.GetString();
                            var state = stateElement.GetString();
                            
                            // Only include iPhone and iPad devices
                            if (name != null && udid != null && state != null && 
                                (name.Contains("iPhone") || name.Contains("iPad")))
                            {
                                // Filter by state if specified
                                if (stateFilter == null)
                                {
                                    // Include all simulators regardless of state
                                    simulators[udid] = name;
                                }
                                else if (stateFilter.Contains("Booted") && state == "Booted")
                                {
                                    simulators[udid] = name;
                                }
                                else if (stateFilter.Contains("Shutdown") && state == "Shutdown")
                                {
                                    simulators[udid] = name;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Return empty dictionary on error
        }
        
        return simulators;
    }
    
    private static async Task RunSimulatorCommand(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xcrun",
                Arguments = $"simctl {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            // Don't throw for already booted simulator
            if (!error.Contains("Unable to boot device in current state: Booted"))
            {
                throw new InvalidOperationException($"Simulator command failed: {error}");
            }
        }
    }
    
    private static string ExtractBundleIdentifier(string appPath)
    {
        try
        {
            var infoPlistPath = Path.Combine(appPath, "Info.plist");
            if (File.Exists(infoPlistPath))
            {
                var content = File.ReadAllText(infoPlistPath);
                var bundleIdIndex = content.IndexOf("<key>CFBundleIdentifier</key>");
                if (bundleIdIndex >= 0)
                {
                    var stringStart = content.IndexOf("<string>", bundleIdIndex) + 8;
                    var stringEnd = content.IndexOf("</string>", stringStart);
                    return content.Substring(stringStart, stringEnd - stringStart);
                }
            }
            
            // Fallback bundle identifier
            return "com.kanriya.client";
        }
        catch
        {
            return "com.kanriya.client";
        }
    }
    
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }
        
        // Copy all subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(subDir, destSubDir);
        }
    }
}