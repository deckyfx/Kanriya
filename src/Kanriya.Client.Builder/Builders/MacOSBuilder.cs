using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class MacOSBuilder : BasePlatformBuilder
{
    public override string PlatformName => "macOS ARM64";
    public override string[] Aliases => new[] { "macos", "mac", "osx", "osx-arm64" };
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building macOS ARM64...[/]");
        task?.Increment(20);
        
        var outputDir = Path.Combine(PublishDir, "osx-arm64");
        
        await RunDotnetCommand($"publish \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Desktop", "Kanriya.Client.Avalonia.Desktop.csproj")}\" " +
            $"-c Release -r osx-arm64 --self-contained -o \"{outputDir}\"");
        
        task?.Increment(40);
        
        var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop");
        MakeExecutable(executable);
        
        await CreateLauncherScript(outputDir, "Run Kanriya.command", executable);
        await CreateAppBundle(outputDir);
        
        task?.Increment(20);
        
        if (!skipZip)
        {
            await CreateZip("osx-arm64", outputDir);
            task?.Increment(15);
        }
        
        AnsiConsole.MarkupLine("✅ [green]macOS build complete![/]");
        task?.Increment(100 - (task?.Value ?? 0));
    }
    
    public override Task<bool> CanLaunchAsync()
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var outputDir = Path.Combine(PublishDir, "osx-arm64");
        
        if (!await CanLaunchAsync())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ macOS launch only available on macOS[/]");
            return;
        }
        
        if (AnsiConsole.Confirm("🚀 [bold]Launch the desktop app now?[/]", true))
        {
            await LaunchMacOSApp(outputDir);
        }
    }
    
    private static async Task LaunchMacOSApp(string outputDir)
    {
        try
        {
            var appBundle = Path.Combine(outputDir, "Kanriya.app");
            var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop");
            
            // Check if app bundle exists and is valid
            if (Directory.Exists(appBundle))
            {
                var bundleExecutable = Path.Combine(appBundle, "Contents", "MacOS", "Kanriya");
                if (File.Exists(bundleExecutable))
                {
                    AnsiConsole.MarkupLine("📱 [blue]Launching app bundle...[/]");
                    
                    // Try to launch the app bundle directly first
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "open",
                            Arguments = $"-W \"{appBundle}\"", // -W waits for app to exit
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]App bundle failed to launch. Exit code: {process.ExitCode}[/]");
                        if (!string.IsNullOrEmpty(error))
                        {
                            AnsiConsole.MarkupLine($"[red]Error: {error}[/]");
                        }
                        
                        // Fall back to direct executable
                        AnsiConsole.MarkupLine("[blue]Trying direct executable...[/]");
                        await LaunchDirectExecutable(executable);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("✅ [green]App launched successfully![/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]App bundle executable not found, trying direct launch...[/]");
                    await LaunchDirectExecutable(executable);
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[blue]No app bundle found, launching executable directly...[/]");
                await LaunchDirectExecutable(executable);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to launch app: {ex.Message}[/]");
            
            // Provide troubleshooting information
            AnsiConsole.MarkupLine("[yellow]💡 Troubleshooting tips:[/]");
            AnsiConsole.MarkupLine("  • Check if the app is blocked by macOS Gatekeeper:");
            AnsiConsole.MarkupLine("    System Preferences → Security & Privacy → General");
            AnsiConsole.MarkupLine("  • Try running manually from Terminal:");
            AnsiConsole.MarkupLine($"    cd \"{outputDir}\"");
            AnsiConsole.MarkupLine("    ./Kanriya.Client.Avalonia.Desktop");
        }
    }
    
    private static async Task LaunchDirectExecutable(string executable)
    {
        if (!File.Exists(executable))
        {
            AnsiConsole.MarkupLine($"[red]❌ Executable not found: {executable}[/]");
            return;
        }
        
        // Ensure executable permissions
        MakeExecutable(executable);
        
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(executable)
            }
        };
        
        process.Start();
        
        // Give the app time to start
        await Task.Delay(3000);
        
        // Check if process is still running
        if (!process.HasExited)
        {
            AnsiConsole.MarkupLine("✅ [green]App launched and running![/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️ App exited with code: {process.ExitCode}[/]");
            AnsiConsole.MarkupLine("[blue]This might indicate missing dependencies or other issues.[/]");
        }
    }
    
    private static async Task CreateLauncherScript(string outputDir, string scriptName, string executable)
    {
        try
        {
            var scriptPath = Path.Combine(outputDir, scriptName);
            var executableName = Path.GetFileName(executable);
            var content = $"#!/bin/bash\ncd \"$(dirname \"$0\")\"\n./{executableName}\n";
            
            await File.WriteAllTextAsync(scriptPath, content);
            MakeExecutable(scriptPath);
            
            AnsiConsole.MarkupLine($"📄 [blue]Created launcher: {scriptPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create launcher script: {ex.Message}[/]");
        }
    }
    
    private static async Task CreateAppBundle(string outputDir)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;
            
        try
        {
            var appName = "Kanriya.app";
            var appPath = Path.Combine(outputDir, appName);
            var contentsPath = Path.Combine(appPath, "Contents");
            var macOSPath = Path.Combine(contentsPath, "MacOS");
            var resourcesPath = Path.Combine(contentsPath, "Resources");
            
            Directory.CreateDirectory(macOSPath);
            Directory.CreateDirectory(resourcesPath);
            
            var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop");
            var bundleExecutable = Path.Combine(macOSPath, "Kanriya");
            
            if (File.Exists(executable))
            {
                File.Copy(executable, bundleExecutable, true);
                MakeExecutable(bundleExecutable);
                
                // Copy all runtime dependencies to the app bundle
                var sourceFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).Equals("Kanriya.Client.Avalonia.Desktop"))
                    .ToList();
                
                foreach (var file in sourceFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(macOSPath, fileName);
                    File.Copy(file, destFile, true);
                }
                
                // Create Info.plist with proper configuration
                var infoPlist = Path.Combine(contentsPath, "Info.plist");
                var plistContent = $"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDisplayName</key>
    <string>Kanriya</string>
    <key>CFBundleExecutable</key>
    <string>Kanriya</string>
    <key>CFBundleIdentifier</key>
    <string>com.kanriya.client</string>
    <key>CFBundleName</key>
    <string>Kanriya</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
</dict>
</plist>
""";
                
                await File.WriteAllTextAsync(infoPlist, plistContent);
                
                AnsiConsole.MarkupLine($"📱 [blue]Created app bundle: {appPath}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create app bundle: {ex.Message}[/]");
        }
    }
}