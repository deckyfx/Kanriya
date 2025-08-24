using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Spectre;
using Spectre.Console;
using Kanriya.Server.Constants;
using Kanriya.Server.Program;
using IOPath = System.IO.Path;

namespace Kanriya.Server.Services;

/// <summary>
/// Unified logging service using Serilog with Spectre.Console for fancy terminal output
/// </summary>
public static class LogService
{
    private static Logger? _logger;
    private static readonly object _lockObject = new();
    
    /// <summary>
    /// Gets the global logger instance
    /// </summary>
    public static Serilog.ILogger Logger => _logger ?? throw new InvalidOperationException("Logger not initialized. Call Initialize() first.");

    /// <summary>
    /// Initialize the logging service with file rotation and console output
    /// </summary>
    public static void Initialize(LogConfiguration? config = null)
    {
        lock (_lockObject)
        {
            if (_logger != null)
            {
                _logger.Dispose();
                _logger = null;
            }

            config ??= new LogConfiguration();
            
            // Ensure logs directory exists
            ApplicationPaths.EnsureDirectoryExists(config.LogDirectory);
            
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(config.MinimumLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ThreadId", System.Threading.Thread.CurrentThread.ManagedThreadId)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("Environment", EnvironmentConfig.App.AspNetCoreEnvironment)
                .Enrich.WithProperty("Application", ApplicationPaths.ApplicationName)
                .Enrich.WithProperty("Version", ApplicationPaths.ApplicationVersion);

            // Configure console output with Spectre
            if (config.EnableConsole)
            {
                loggerConfig.WriteTo.Spectre(
                    outputTemplate: config.ConsoleOutputTemplate,
                    restrictedToMinimumLevel: config.ConsoleMinimumLevel);
            }

            // Configure file output with rotation
            if (config.EnableFile)
            {
                var logFilePath = IOPath.Combine(config.LogDirectory, config.LogFileName);
                
                loggerConfig.WriteTo.File(
                    path: logFilePath,
                    outputTemplate: config.FileOutputTemplate,
                    restrictedToMinimumLevel: config.FileMinimumLevel,
                    fileSizeLimitBytes: config.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    rollingInterval: config.RollingInterval,
                    retainedFileCountLimit: config.RetainedFileCountLimit,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }

            // Configure Seq output if enabled
            if (config.EnableSeq)
            {
                loggerConfig.WriteTo.Seq(
                    serverUrl: config.SeqServerUrl,
                    apiKey: config.SeqApiKey,
                    restrictedToMinimumLevel: config.SeqMinimumLevel,
                    batchPostingLimit: 100,
                    period: TimeSpan.FromSeconds(2));
            }

            _logger = loggerConfig.CreateLogger();
            Log.Logger = _logger;
        }
    }

    /// <summary>
    /// Shutdown the logging service and flush all pending logs
    /// </summary>
    public static void Shutdown()
    {
        lock (_lockObject)
        {
            _logger?.Dispose();
            _logger = null;
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Display a startup banner with application information
    /// </summary>
    public static void DisplayStartupBanner(string title, string? version = null)
    {
        AnsiConsole.Clear();
        
        // Display ASCII art
        var figlet = new FigletText(title)
            .Centered()
            .Color(Color.Cyan1);
        AnsiConsole.Write(figlet);
        
        if (!string.IsNullOrEmpty(version))
        {
            AnsiConsole.MarkupLine($"[dim cyan]Version: {version}[/]".PadLeft((Console.WindowWidth + version.Length + 9) / 2));
            AnsiConsole.WriteLine();
        }
        
        var rule = new Rule()
        {
            Style = Style.Parse("cyan dim")
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        
        // Display path information in a fancy panel
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow(
            new Text("Working Directory", new Style(Color.Cyan2)),
            new Text(ApplicationPaths.WorkingDirectory, new Style(Color.Grey))
        );
        grid.AddRow(
            new Text("Executable Directory", new Style(Color.Cyan2)),
            new Text(ApplicationPaths.ExecutableDirectory, new Style(Color.Grey))
        );
        grid.AddRow(
            new Text("Logs Directory", new Style(Color.Cyan2)),
            new Text(ApplicationPaths.LogsDirectory, new Style(Color.Grey))
        );
        
        var panel = new Panel(grid)
        {
            Header = new PanelHeader("[yellow]Application Paths[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(2, 0)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Display a progress spinner for long-running operations
    /// </summary>
    public static T RunWithProgress<T>(string description, Func<T> operation)
    {
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start($"[cyan]{description}[/]...", ctx => operation());
    }

    /// <summary>
    /// Display a progress spinner for long-running async operations
    /// </summary>
    public static async Task<T> RunWithProgressAsync<T>(string description, Func<Task<T>> operation)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"[cyan]{description}[/]...", async ctx => await operation());
    }

    /// <summary>
    /// Log a formatted section header
    /// </summary>
    public static void LogSection(string title)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule($"[bold yellow]{title}[/]")
        {
            Justification = Justify.Left,
            Style = Style.Parse("yellow dim")
        };
        AnsiConsole.Write(rule);
    }

    /// <summary>
    /// Log success with checkmark
    /// </summary>
    public static void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
        Logger.Information(message);
    }

    /// <summary>
    /// Log warning with warning symbol
    /// </summary>
    public static void LogWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");
        Logger.Warning(message);
    }

    /// <summary>
    /// Log error with X symbol
    /// </summary>
    public static void LogError(string message, Exception? exception = null)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");
        if (exception != null)
        {
            Logger.Error(exception, message);
            AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
        }
        else
        {
            Logger.Error(message);
        }
    }

    /// <summary>
    /// Log info with info symbol
    /// </summary>
    public static void LogInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {message}");
        Logger.Information(message);
    }
}

/// <summary>
/// Configuration for the logging service
/// </summary>
public class LogConfiguration
{
    /// <summary>
    /// Minimum log level for all sinks
    /// </summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Enable console output
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Minimum log level for console output
    /// </summary>
    public LogEventLevel ConsoleMinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Console output template
    /// </summary>
    public string ConsoleOutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Enable file output
    /// </summary>
    public bool EnableFile { get; set; } = true;

    /// <summary>
    /// Minimum log level for file output
    /// </summary>
    public LogEventLevel FileMinimumLevel { get; set; } = LogEventLevel.Debug;

    /// <summary>
    /// File output template
    /// </summary>
    public string FileOutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Log directory path
    /// </summary>
    public string LogDirectory { get; set; } = ApplicationPaths.LogsDirectory;

    /// <summary>
    /// Log file name pattern
    /// </summary>
    public string LogFileName { get; set; } = "gqlserver-.log";

    /// <summary>
    /// Maximum file size before rolling (10MB default)
    /// </summary>
    public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Rolling interval for log files
    /// </summary>
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

    /// <summary>
    /// Number of log files to retain (null = unlimited)
    /// </summary>
    public int? RetainedFileCountLimit { get; set; } = 30;
    
    /// <summary>
    /// Enable Seq centralized logging
    /// </summary>
    public bool EnableSeq { get; set; } = false;
    
    /// <summary>
    /// Seq server URL
    /// </summary>
    public string SeqServerUrl { get; set; } = "http://localhost:5341";
    
    /// <summary>
    /// Seq API key (optional)
    /// </summary>
    public string? SeqApiKey { get; set; }
    
    /// <summary>
    /// Minimum log level for Seq output
    /// </summary>
    public LogEventLevel SeqMinimumLevel { get; set; } = LogEventLevel.Information;
}