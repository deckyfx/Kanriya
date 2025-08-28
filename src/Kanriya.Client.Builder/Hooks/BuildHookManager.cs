using Spectre.Console;

namespace Kanriya.Client.Builder.Hooks;

public class BuildHookManager
{
    private readonly List<IBuildHook> _preBuildHooks = new();
    private readonly List<IBuildHook> _postBuildHooks = new();
    
    public void RegisterPreBuildHook(IBuildHook hook)
    {
        _preBuildHooks.Add(hook);
        _preBuildHooks.Sort((x, y) => x.Priority.CompareTo(y.Priority));
    }
    
    public void RegisterPostBuildHook(IBuildHook hook)
    {
        _postBuildHooks.Add(hook);
        _postBuildHooks.Sort((x, y) => x.Priority.CompareTo(y.Priority));
    }
    
    public async Task ExecutePreBuildHooksAsync(BuildContext context)
    {
        foreach (var hook in _preBuildHooks)
        {
            try
            {
                AnsiConsole.MarkupLine($"[dim]🪝 Executing pre-build hook: {hook.Name}[/]");
                await hook.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Pre-build hook '{hook.Name}' failed: {ex.Message}[/]");
                throw;
            }
        }
    }
    
    public async Task ExecutePostBuildHooksAsync(BuildContext context)
    {
        foreach (var hook in _postBuildHooks)
        {
            try
            {
                AnsiConsole.MarkupLine($"[dim]🪝 Executing post-build hook: {hook.Name}[/]");
                await hook.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Post-build hook '{hook.Name}' failed: {ex.Message}[/]");
                // Post-build hook failures are non-fatal
            }
        }
    }
}

// Built-in hooks
public class CleanOutputHook : IBuildHook
{
    public string Name => "Clean Output Directory";
    public int Priority => 10;
    
    public Task ExecuteAsync(BuildContext context)
    {
        if (Directory.Exists(context.OutputDirectory))
        {
            Directory.Delete(context.OutputDirectory, true);
        }
        Directory.CreateDirectory(context.OutputDirectory);
        return Task.CompletedTask;
    }
}

public class ValidateEnvironmentHook : IBuildHook
{
    public string Name => "Validate Environment";
    public int Priority => 5;
    
    public async Task ExecuteAsync(BuildContext context)
    {
        // Check .NET SDK
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(".NET SDK not found or not working");
        }
    }
}

public class BuildSummaryHook : IBuildHook
{
    public string Name => "Build Summary";
    public int Priority => 100;
    
    public Task ExecuteAsync(BuildContext context)
    {
        AnsiConsole.MarkupLine($"📊 [blue]Build completed for {context.Platform}[/]");
        
        if (Directory.Exists(context.OutputDirectory))
        {
            var files = Directory.GetFiles(context.OutputDirectory, "*", SearchOption.AllDirectories);
            var totalSize = files.Sum(f => new FileInfo(f).Length);
            
            AnsiConsole.MarkupLine($"📁 [dim]Output: {context.OutputDirectory}[/]");
            AnsiConsole.MarkupLine($"📄 [dim]Files: {files.Length}, Size: {FormatBytes(totalSize)}[/]");
        }
        
        return Task.CompletedTask;
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}