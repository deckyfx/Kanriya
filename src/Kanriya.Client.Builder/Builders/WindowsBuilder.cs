using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class WindowsBuilder : BasePlatformBuilder
{
    public override string PlatformName => "Windows x64";
    public override string[] Aliases => new[] { "windows", "win", "win-x64" };
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building Windows x64...[/]");
        task?.Increment(20);
        
        var outputDir = Path.Combine(PublishDir, "win-x64");
        
        await RunDotnetCommand($"publish \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Desktop", "Kanriya.Client.Avalonia.Desktop.csproj")}\" " +
            $"-c Release -r win-x64 --self-contained -o \"{outputDir}\"");
        
        task?.Increment(50);
        
        // Create launcher script
        await CreateLauncherScript(outputDir, "run-kanriya.bat", "Kanriya.Client.Avalonia.Desktop.exe");
        
        task?.Increment(15);
        
        if (!skipZip)
        {
            await CreateZip("win-x64", outputDir);
            task?.Increment(10);
        }
        
        AnsiConsole.MarkupLine("✅ [green]Windows build complete![/]");
        task?.Increment(100 - (task?.Value ?? 0));
    }
    
    public override Task<bool> CanLaunchAsync()
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var outputDir = Path.Combine(PublishDir, "win-x64");
        var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop.exe");
        
        if (!File.Exists(executable))
        {
            AnsiConsole.MarkupLine("[red]❌ Windows build not found. Build Windows platform first.[/]");
            return;
        }
        
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            });
            await Task.Delay(2000); // Give time to start
            AnsiConsole.MarkupLine("✅ [green]App launched![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not launch app: {ex.Message}[/]");
        }
    }
    
    private static async Task CreateLauncherScript(string outputDir, string scriptName, string executableName)
    {
        try
        {
            var scriptPath = Path.Combine(outputDir, scriptName);
            var content = $"@echo off\ncd /d \"%~dp0\"\nstart \"\" \"{executableName}\"\n";
            
            await File.WriteAllTextAsync(scriptPath, content);
            AnsiConsole.MarkupLine($"📄 [blue]Created launcher: {scriptPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create launcher script: {ex.Message}[/]");
        }
    }
}