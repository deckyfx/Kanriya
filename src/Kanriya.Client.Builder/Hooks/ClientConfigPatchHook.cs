using System.Text.RegularExpressions;
using Kanriya.Shared;
using Spectre.Console;

namespace Kanriya.Client.Builder.Hooks;

/// <summary>
/// Pre-build hook that patches client csproj files with server configuration from EnvironmentConfig
/// This allows clients to be built with dynamic server URLs and configuration from the .env file
/// </summary>
public class ClientConfigPatchHook : IBuildHook
{
    public string Name => "Client Config Patcher";
    public int Priority => 5; // Run before other pre-build hooks

    // Shared library csproj that contains server configuration for all platforms
    private const string SharedClientCsproj = "Kanriya.Client.Avalonia/Kanriya.Client.Avalonia.csproj";

    public async Task ExecuteAsync(BuildContext context)
    {
        AnsiConsole.MarkupLine("[blue]🔧 Patching client configuration from EnvironmentConfig...[/]");
        
        try
        {
            // Force load environment variables from .env file
            EnvironmentConfig.LoadEnvironment(debug: true);
            
            AnsiConsole.MarkupLine($"[dim]🎯 Platform: {context.Platform} → patching shared library[/]");

            // Get the project root directory
            var projectRoot = GetProjectRoot();
            var clientAvaloniaRoot = Path.Combine(projectRoot, "src", "Kanriya.Client.Avalonia");
            var csprojPath = Path.Combine(clientAvaloniaRoot, SharedClientCsproj);

            if (!File.Exists(csprojPath))
            {
                AnsiConsole.MarkupLine($"[red]❌ Csproj file not found: {csprojPath}[/]");
                return;
            }


            // Read current csproj content
            var content = await File.ReadAllTextAsync(csprojPath);

            // Create configuration values from EnvironmentConfig
            var serverUrl = EnvironmentConfig.Server.BaseUrl;
            var publicUrl = EnvironmentConfig.Server.PublicUrl;
            var urls = EnvironmentConfig.Server.Urls;
            var graphqlUrl = $"{serverUrl}/graphql";
            var apiBaseUrl = $"{serverUrl}/api";
            var webSocketUrl = serverUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/graphql";
            var appEnvironment = "Development"; // Could be made configurable
            var debugMode = "true"; // Could be made configurable

            AnsiConsole.MarkupLine($"[dim]📡 Server URL: {serverUrl}[/]");

            // Replace configuration values between markers
            var updatedContent = PatchConfigurationSection(content, new Dictionary<string, string>
            {
                { "ServerUrl", serverUrl },
                { "GraphQLUrl", graphqlUrl },
                { "ApiBaseUrl", apiBaseUrl },
                { "WebSocketUrl", webSocketUrl },
                { "AppEnvironment", appEnvironment },
                { "DebugMode", debugMode }
            });

            // Write the updated content back
            await File.WriteAllTextAsync(csprojPath, updatedContent);

            AnsiConsole.MarkupLine($"[green]✅ Patched shared library with server configuration[/]");
            
            // Force clean to ensure fresh build with new metadata
            var cleanResult = await CleanProject(csprojPath);
            if (!cleanResult)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ Clean failed, but continuing with build[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to patch client configuration: {ex.Message}[/]");
            throw;
        }
    }

    private static string PatchConfigurationSection(string content, Dictionary<string, string> values)
    {
        // Find the configuration section between markers
        const string startMarker = "<!-- KANRIYA_CLIENT_CONFIG_START -->";
        const string endMarker = "<!-- KANRIYA_CLIENT_CONFIG_END -->";

        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);

        if (startIndex == -1 || endIndex == -1)
        {
            throw new InvalidOperationException("Configuration markers not found in csproj file");
        }

        // Extract the configuration section
        var beforeSection = content[..startIndex];
        var afterSection = content[(endIndex + endMarker.Length)..];
        
        // Build new configuration section
        var newSection = startMarker + Environment.NewLine +
                        "    <!-- Client Environment Configuration (can be overridden by build scripts) -->" + Environment.NewLine;

        foreach (var (key, value) in values)
        {
            newSection += $"    <{key}>{value}</{key}>" + Environment.NewLine;
        }

        newSection += "    " + endMarker;

        return beforeSection + newSection + afterSection;
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

    private static async Task<bool> CleanProject(string csprojPath)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"clean \"{csprojPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}