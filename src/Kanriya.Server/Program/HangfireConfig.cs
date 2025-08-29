using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;

namespace Kanriya.Server.Program;

/// <summary>
/// Configuration for Hangfire background job processing
/// </summary>
public static class HangfireConfig
{
    /// <summary>
    /// Configures Hangfire services for dependency injection
    /// </summary>
    public static void ConfigureHangfireServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Shared.EnvironmentConfig.Database.GetConnectionString();
        
        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            }, new PostgreSqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15),
                InvisibilityTimeout = TimeSpan.FromMinutes(5),
                DistributedLockTimeout = TimeSpan.FromMinutes(5),
                TransactionSynchronisationTimeout = TimeSpan.FromMilliseconds(500),
                PrepareSchemaIfNecessary = true,
                EnableTransactionScopeEnlistment = true,
                DeleteExpiredBatchSize = 1000,
                SchemaName = "hangfire" // Use separate schema for Hangfire tables
            }));

        // Add the Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "emails", "critical", "default" };
            options.ServerName = $"{Environment.MachineName}:EmailServer";
        });
    }

    /// <summary>
    /// Configures the Hangfire middleware pipeline
    /// </summary>
    public static void ConfigureHangfireMiddleware(WebApplication app)
    {
        // Map Hangfire Dashboard
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DashboardTitle = "Email Queue Dashboard",
            DisplayStorageConnectionString = false,
            DarkModeEnabled = true
        });

        // Register recurring jobs
        RegisterRecurringJobs();
    }

    /// <summary>
    /// Registers all recurring background jobs
    /// </summary>
    private static void RegisterRecurringJobs()
    {
        // Process email queue every minute
        RecurringJob.AddOrUpdate<IMailProcessor>(
            "process-email-queue",
            processor => processor.ProcessPendingEmails(),
            "*/1 * * * *"); // Every minute

        // Clean up old email logs daily
        RecurringJob.AddOrUpdate<IMailProcessor>(
            "cleanup-old-emails",
            processor => processor.CleanupOldEmails(30), // Keep 30 days
            "0 2 * * *"); // Daily at 2 AM

        // Reset daily email limits at midnight
        RecurringJob.AddOrUpdate<IMailProcessor>(
            "reset-daily-limits",
            processor => processor.ResetDailyLimits(),
            "0 0 * * *"); // Daily at midnight
    }
}

/// <summary>
/// Custom authorization filter for Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For now, allow all access (TODO: implement proper authentication)
        return true;
        
        // var httpContext = context.GetHttpContext();
        // // In production, require authentication and SuperAdmin role
        // return httpContext.User.Identity?.IsAuthenticated == true &&
        //        httpContext.User.IsInRole("SuperAdmin");
    }
}

/// <summary>
/// Interface for mail processing background jobs
/// </summary>
public interface IMailProcessor
{
    Task ProcessPendingEmails();
    Task ProcessSpecificEmail(Guid emailId);
    Task CleanupOldEmails(int daysToKeep);
    Task ResetDailyLimits();
}