using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class LinuxBuilder : BasePlatformBuilder
{
    public override string PlatformName => "Linux x64";
    public override string[] Aliases => new[] { "linux", "linux-x64" };
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building Linux x64...[/]");
        task?.Increment(20);
        
        var outputDir = Path.Combine(PublishDir, "linux-x64");
        
        await RunDotnetCommand($"publish \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Desktop", "Kanriya.Client.Avalonia.Desktop.csproj")}\" " +
            $"-c Release -r linux-x64 --self-contained -o \"{outputDir}\"");
        
        task?.Increment(40);
        
        var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop");
        MakeExecutable(executable);
        
        await CreateLauncherScript(outputDir, "run-kanriya.sh", executable);
        
        task?.Increment(20);
        
        if (!skipZip)
        {
            await CreateZip("linux-x64", outputDir);
            task?.Increment(15);
        }
        
        AnsiConsole.MarkupLine("✅ [green]Linux build complete![/]");
        task?.Increment(100 - (task?.Value ?? 0));
    }
    
    public override Task<bool> CanLaunchAsync()
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var outputDir = Path.Combine(PublishDir, "linux-x64");
        
        if (!await CanLaunchAsync())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ Linux launch only available on Linux[/]");
            return;
        }
        
        if (AnsiConsole.Confirm("🚀 [bold]Launch the desktop app now?[/]", true))
        {
            await LaunchLinuxApp(outputDir);
        }
    }
    
    private static async Task LaunchLinuxApp(string outputDir)
    {
        try
        {
            var executable = Path.Combine(outputDir, "Kanriya.Client.Avalonia.Desktop");
            if (File.Exists(executable))
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = executable,
                        UseShellExecute = true,
                        WorkingDirectory = outputDir
                    }
                };
                
                process.Start();
                await Task.Delay(2000);
                AnsiConsole.MarkupLine("✅ [green]App launched![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Linux executable not found. Build Linux platform first.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not launch app: {ex.Message}[/]");
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
}