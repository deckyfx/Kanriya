using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using Kanriya.Server.Services.Data;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Kanriya.Server.Program;

/// <summary>
/// Configuration for mail services
/// </summary>
public static class MailServiceConfig
{
    /// <summary>
    /// Configures mail services for dependency injection
    /// </summary>
    public static void ConfigureMailServices(IServiceCollection services)
    {
        // Register mail drivers
        services.AddScoped<SystemSmtpMailDriver>();
        services.AddScoped<IMailDriver, SystemSmtpMailDriver>();
        
        // Register mail services
        services.AddScoped<IMailerService, MailerService>();
        services.AddScoped<IMailProcessor, MailProcessor>();
        
        // Register multi-tenant services
        services.AddSingleton<IPostgreSQLManagementService, PostgreSQLManagementService>();
        services.AddSingleton<IBrandConnectionService, BrandConnectionService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IApiCredentialService, ApiCredentialService>();
        services.AddScoped<IOutletService, OutletService>();
        
        // Hangfire's IBackgroundJobClient is registered by Hangfire itself
        // Don't need to register it here - removed circular dependency
    }
    
    /// <summary>
    /// Initialize and validate mail services at startup
    /// This ensures SMTP configuration is loaded during boot, not on first request
    /// </summary>
    public static async Task InitializeMailServicesAsync(IServiceProvider serviceProvider)
    {
        try
        {
            LogService.LogInfo("Initializing mail services...");
            
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            
            // Resolve and initialize the SystemSmtpMailDriver
            // This will trigger the constructor and load SMTP settings
            var mailDriver = scope.ServiceProvider.GetRequiredService<IMailDriver>();
            
            // Validate the configuration
            var isValid = await mailDriver.ValidateConfigurationAsync();
            
            if (isValid)
            {
                LogService.LogSuccess($"Mail driver '{mailDriver.Name}' initialized and validated successfully");
                
                // Test connection if possible
                var canConnect = await mailDriver.TestConnectionAsync();
                if (canConnect)
                {
                    LogService.LogSuccess("SMTP connection test successful");
                }
                else
                {
                    LogService.LogWarning("SMTP connection test failed - emails may not be sent");
                }
            }
            else
            {
                LogService.LogWarning("Mail configuration is invalid - emails will not be sent");
            }
            
            // Also initialize the MailerService to ensure all dependencies are ready
            var mailerService = scope.ServiceProvider.GetRequiredService<IMailerService>();
            LogService.LogInfo("MailerService initialized successfully");
            
        }
        catch (Exception ex)
        {
            LogService.LogError("Failed to initialize mail services", ex);
            // Don't throw - let the application start even if mail isn't working
            LogService.LogWarning("Application will continue without email functionality");
        }
    }
}