using System.Net;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public class WebBuilder : BasePlatformBuilder
{
    public override string PlatformName => "Web (WASM)";
    public override string[] Aliases => new[] { "web", "browser", "wasm" };
    
    public override async Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null)
    {
        AnsiConsole.MarkupLine("🔨 [bold]Building Web (WASM)...[/]");
        task?.Increment(20);
        
        var outputDir = Path.Combine(PublishDir, "web");
        
        await RunDotnetCommand($"publish \"{Path.Combine(ClientProject, "Kanriya.Client.Avalonia.Browser", "Kanriya.Client.Avalonia.Browser.csproj")}\" " +
            $"-c Release -o \"{outputDir}\"");
        
        task?.Increment(60);
        
        if (!skipZip)
        {
            await CreateZip("web", outputDir);
            task?.Increment(15);
        }
        
        AnsiConsole.MarkupLine("✅ [green]Web build complete![/]");
        AnsiConsole.MarkupLine("[yellow]📡 Note: WASM requires HTTP server due to CORS.[/]");
        
        task?.Increment(100 - (task?.Value ?? 0));
    }
    
    public override async Task LaunchAsync(bool publish = false)
    {
        var webPath = Path.Combine(PublishDir, "web", "wwwroot");
        
        if (!Directory.Exists(webPath))
        {
            AnsiConsole.MarkupLine("[red]❌ Web build not found. Build web platform first.[/]");
            return;
        }
        
        if (AnsiConsole.Confirm("🌐 [bold]Start HTTP server now?[/]", true))
        {
            var port = AnsiConsole.Prompt(
                new TextPrompt<int>("Port:")
                    .DefaultValue(8080));
            
            await StartHttpServer(port, webPath, true);
        }
    }
    
    private static async Task StartHttpServer(int port, string path, bool openBrowser)
    {
        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]❌ Directory not found: {path}[/]");
            return;
        }
        
        var listener = new HttpListener();
        var prefix = $"http://localhost:{port}/";
        listener.Prefixes.Add(prefix);
        
        try
        {
            listener.Start();
            AnsiConsole.MarkupLine($"🌐 [green]HTTP Server started at {prefix}[/]");
            AnsiConsole.MarkupLine($"📁 [blue]Serving: {path}[/]");
            AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");
            
            if (openBrowser)
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = prefix, UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        System.Diagnostics.Process.Start("open", prefix);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        System.Diagnostics.Process.Start("xdg-open", prefix);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠️ Could not open browser: {ex.Message}[/]");
                    AnsiConsole.MarkupLine($"[blue]Please open {prefix} manually[/]");
                }
            }
            
            // Handle requests
            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleHttpRequest(context, path));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ HTTP Server error: {ex.Message}[/]");
        }
        finally
        {
            listener.Stop();
        }
    }
    
    private static async Task HandleHttpRequest(HttpListenerContext context, string basePath)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            
            var requestedPath = request.Url?.LocalPath?.TrimStart('/') ?? "index.html";
            if (string.IsNullOrEmpty(requestedPath) || requestedPath == "/")
            {
                requestedPath = "index.html";
            }
            
            var fullPath = Path.Combine(basePath, requestedPath);
            
            if (File.Exists(fullPath))
            {
                var fileExtension = Path.GetExtension(fullPath).ToLowerInvariant();
                var contentType = GetContentType(fileExtension);
                
                response.ContentType = contentType;
                response.StatusCode = 200;
                
                var fileContent = await File.ReadAllBytesAsync(fullPath);
                response.ContentLength64 = fileContent.Length;
                
                await response.OutputStream.WriteAsync(fileContent);
            }
            else
            {
                response.StatusCode = 404;
                var notFoundMessage = "File not found"u8.ToArray();
                response.ContentLength64 = notFoundMessage.Length;
                await response.OutputStream.WriteAsync(notFoundMessage);
            }
            
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                var errorMessage = System.Text.Encoding.UTF8.GetBytes($"Server Error: {ex.Message}");
                context.Response.ContentLength64 = errorMessage.Length;
                await context.Response.OutputStream.WriteAsync(errorMessage);
                context.Response.OutputStream.Close();
            }
            catch
            {
                // Ignore errors while handling errors
            }
        }
    }
    
    private static string GetContentType(string fileExtension)
    {
        return fileExtension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css", 
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".wasm" => "application/wasm",
            ".woff" or ".woff2" => "font/woff",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            _ => "text/plain"
        };
    }
}