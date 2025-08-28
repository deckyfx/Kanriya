using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public abstract class BasePlatformBuilder : IPlatformBuilder
{
    protected static readonly string ProjectRoot = GetProjectRoot();
    protected static readonly string PublishDir = Path.Combine(ProjectRoot, "publish", "client");
    protected static readonly string ClientProject = Path.Combine(ProjectRoot, "src", "Kanriya.Client.Avalonia");
    
    public abstract string PlatformName { get; }
    public abstract string[] Aliases { get; }
    
    public virtual Task<bool> CanBuildAsync()
    {
        return Task.FromResult(true);
    }
    
    public abstract Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null);
    
    public virtual Task<bool> CanLaunchAsync()
    {
        return Task.FromResult(true);
    }
    
    public virtual Task LaunchAsync(bool publish = false)
    {
        return Task.CompletedTask;
    }
    
    protected async Task RunDotnetCommand(string arguments)
    {
        AnsiConsole.MarkupLine($"[dim]$ dotnet {arguments}[/]");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]❌ dotnet command failed with exit code {process.ExitCode}[/]");
                
                if (!string.IsNullOrEmpty(output))
                {
                    AnsiConsole.MarkupLine($"[yellow]Output:[/]");
                    AnsiConsole.WriteLine(output.Trim()); // Use WriteLine to avoid markup parsing
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/]");
                    AnsiConsole.WriteLine(error.Trim()); // Use WriteLine to avoid markup parsing
                }
                
                throw new InvalidOperationException($"dotnet command failed with exit code {process.ExitCode}");
            }
            
            if (!string.IsNullOrEmpty(output) && (output.Contains("error") || output.Contains("warning")))
            {
                AnsiConsole.WriteLine(output.Trim()); // Use WriteLine to avoid markup parsing
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to execute dotnet command: {ex.Message}[/]");
            throw;
        }
    }
    
    protected static void MakeExecutable(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Could not make file executable: {ex.Message}[/]");
            }
        }
    }
    
    protected static async Task CreateZip(string name, string directory)
    {
        try
        {
            var zipPath = Path.Combine(PublishDir, $"{name}.zip");
            
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"Compress-Archive -Path '{directory}\\*' -DestinationPath '{zipPath}' -Force\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            else
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "zip",
                        Arguments = $"-r \"{zipPath}\" .",
                        WorkingDirectory = directory,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            
            AnsiConsole.MarkupLine($"📦 [blue]Created: {zipPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create zip file: {ex.Message}[/]");
        }
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