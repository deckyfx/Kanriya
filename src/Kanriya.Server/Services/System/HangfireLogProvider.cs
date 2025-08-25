using Hangfire.Logging;
using LogLevel = Hangfire.Logging.LogLevel;
using SerilogLogger = Serilog.ILogger;

namespace Kanriya.Server.Services.System;

/// <summary>
/// Custom log provider for Hangfire to integrate with Serilog and add tags
/// </summary>
public class HangfireLogProvider : ILogProvider
{
    public ILog GetLogger(string name)
    {
        return new HangfireSerilogAdapter(LogService.Logger.ForContext("SourceContext", name));
    }
}

/// <summary>
/// Adapter to bridge Hangfire logging to Serilog with tags
/// </summary>
public class HangfireSerilogAdapter : ILog
{
    private readonly SerilogLogger _logger;

    public HangfireSerilogAdapter(SerilogLogger logger)
    {
        _logger = logger.ForContext("Tag", "Hangfire");
    }

    public bool Log(LogLevel logLevel, Func<string>? messageFunc, Exception? exception = null)
    {
        if (messageFunc == null)
            return IsLogLevelEnabled(logLevel);

        if (!IsLogLevelEnabled(logLevel))
            return false;

        var message = $"[Hangfire] {messageFunc()}";

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                if (exception != null)
                    _logger.Debug(exception, message);
                else
                    _logger.Debug(message);
                break;
                
            case LogLevel.Info:
                if (exception != null)
                    _logger.Information(exception, message);
                else
                    _logger.Information(message);
                break;
                
            case LogLevel.Warn:
                if (exception != null)
                    _logger.Warning(exception, message);
                else
                    _logger.Warning(message);
                break;
                
            case LogLevel.Error:
                if (exception != null)
                    _logger.Error(exception, message);
                else
                    _logger.Error(message);
                break;
                
            case LogLevel.Fatal:
                if (exception != null)
                    _logger.Fatal(exception, message);
                else
                    _logger.Fatal(message);
                break;
        }

        return true;
    }

    private bool IsLogLevelEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => _logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose),
            LogLevel.Debug => _logger.IsEnabled(Serilog.Events.LogEventLevel.Debug),
            LogLevel.Info => _logger.IsEnabled(Serilog.Events.LogEventLevel.Information),
            LogLevel.Warn => _logger.IsEnabled(Serilog.Events.LogEventLevel.Warning),
            LogLevel.Error => _logger.IsEnabled(Serilog.Events.LogEventLevel.Error),
            LogLevel.Fatal => _logger.IsEnabled(Serilog.Events.LogEventLevel.Fatal),
            _ => false
        };
    }
}

/// <summary>
/// Hangfire job filter to add logging for job execution
/// </summary>
public class HangfireJobLoggingFilter : 
    Hangfire.Server.IServerFilter,
    Hangfire.Client.IClientFilter,
    Hangfire.States.IElectStateFilter,
    Hangfire.States.IApplyStateFilter
{
    private static readonly SerilogLogger Logger = LogService.Logger.ForContext("Tag", "Hangfire");

    public void OnCreating(Hangfire.Client.CreatingContext filterContext)
    {
        Logger.Information("[Hangfire] Creating job: {JobType}.{Method}", 
            filterContext.Job.Type.Name, 
            filterContext.Job.Method.Name);
    }

    public void OnCreated(Hangfire.Client.CreatedContext filterContext)
    {
        if (filterContext.BackgroundJob != null)
        {
            Logger.Information("[Hangfire] Job created with ID: {JobId} | Type: {JobType}.{Method}", 
                filterContext.BackgroundJob.Id,
                filterContext.Job.Type.Name,
                filterContext.Job.Method.Name);
        }
    }

    public void OnPerforming(Hangfire.Server.PerformingContext filterContext)
    {
        Logger.Information("[Hangfire] Job starting | ID: {JobId} | Type: {JobType}.{Method} | Attempt: {Attempt}",
            filterContext.BackgroundJob.Id,
            filterContext.BackgroundJob.Job.Type.Name,
            filterContext.BackgroundJob.Job.Method.Name,
            filterContext.GetJobParameter<int>("RetryCount") + 1);
    }

    public void OnPerformed(Hangfire.Server.PerformedContext filterContext)
    {
        if (filterContext.Exception != null)
        {
            Logger.Error(filterContext.Exception, 
                "[Hangfire] Job failed | ID: {JobId} | Type: {JobType}.{Method} | Duration: {Duration}ms",
                filterContext.BackgroundJob.Id,
                filterContext.BackgroundJob.Job.Type.Name,
                filterContext.BackgroundJob.Job.Method.Name,
                filterContext.Items.ContainsKey("Duration") ? filterContext.Items["Duration"] : "Unknown");
        }
        else
        {
            Logger.Information("[Hangfire] Job completed | ID: {JobId} | Type: {JobType}.{Method} | Duration: {Duration}ms",
                filterContext.BackgroundJob.Id,
                filterContext.BackgroundJob.Job.Type.Name,
                filterContext.BackgroundJob.Job.Method.Name,
                filterContext.Items.ContainsKey("Duration") ? filterContext.Items["Duration"] : "Unknown");
        }
    }

    public void OnStateElection(Hangfire.States.ElectStateContext context)
    {
        // Log state transitions
        if (context.CandidateState != null)
        {
            Logger.Debug("[Hangfire] Job state election | ID: {JobId} | NewState: {State}",
                context.BackgroundJob.Id,
                context.CandidateState.Name);
        }
    }

    public void OnStateApplied(Hangfire.States.ApplyStateContext context, Hangfire.Storage.IWriteOnlyTransaction transaction)
    {
        // Log when state is applied
        Logger.Debug("[Hangfire] Job state applied | ID: {JobId} | State: {State}",
            context.BackgroundJob.Id,
            context.NewState?.Name ?? "Unknown");
    }

    public void OnStateUnapplied(Hangfire.States.ApplyStateContext context, Hangfire.Storage.IWriteOnlyTransaction transaction)
    {
        // Log when state is unapplied
        Logger.Debug("[Hangfire] Job state unapplied | ID: {JobId} | State: {State}",
            context.BackgroundJob.Id,
            context.OldStateName);
    }
}