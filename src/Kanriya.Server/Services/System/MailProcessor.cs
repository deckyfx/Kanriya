using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kanriya.Server.Data;
using Kanriya.Server.Program;
using Kanriya.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kanriya.Server.Services.System;

/// <summary>
/// Background job processor for email queue
/// </summary>
public class MailProcessor : IMailProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MailProcessor> _logger;

    public MailProcessor(IServiceProvider serviceProvider, ILogger<MailProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Process all pending emails in the queue
    /// </summary>
    public async Task ProcessPendingEmails()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Get pending emails ordered by priority and creation date
            var pendingEmails = await dbContext.EmailOutboxes
                .Where(e => e.Status == EmailStatus.Pending && 
                           (e.ScheduledFor == null || e.ScheduledFor <= DateTime.UtcNow) &&
                           e.Attempts < e.MaxAttempts)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.CreatedAt)
                .Take(Shared.EnvironmentConfig.Mail.BatchSize) // Process batch of emails at a time
                .ToListAsync();

            if (!pendingEmails.Any())
            {
                _logger.LogDebug("No pending emails to process");
                return;
            }

            _logger.LogInformation("Processing {Count} pending emails (batch size: {BatchSize})", 
                pendingEmails.Count, Shared.EnvironmentConfig.Mail.BatchSize);

            foreach (var email in pendingEmails)
            {
                await ProcessEmailAsync(email, dbContext, scope.ServiceProvider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending emails");
        }
    }

    /// <summary>
    /// Process a specific email by ID
    /// </summary>
    public async Task ProcessSpecificEmail(Guid emailId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var email = await dbContext.EmailOutboxes.FindAsync(emailId);
            
            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found", emailId);
                return;
            }

            if (email.Status != EmailStatus.Pending)
            {
                _logger.LogDebug("Email {EmailId} is not pending (status: {Status})", emailId, email.Status);
                return;
            }

            await ProcessEmailAsync(email, dbContext, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email {EmailId}", emailId);
        }
    }

    /// <summary>
    /// Clean up old emails from the database
    /// </summary>
    public async Task CleanupOldEmails(int daysToKeep)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

            // Delete old sent emails
            var oldEmails = await dbContext.EmailOutboxes
                .Where(e => e.Status == EmailStatus.Sent && e.SentAt < cutoffDate)
                .ToListAsync();

            if (oldEmails.Any())
            {
                dbContext.EmailOutboxes.RemoveRange(oldEmails);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Deleted {Count} old emails older than {Days} days", 
                    oldEmails.Count, daysToKeep);
            }

            // Also clean up old logs
            var oldLogs = await dbContext.EmailLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                dbContext.EmailLogs.RemoveRange(oldLogs);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Deleted {Count} old email logs", oldLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old emails");
        }
    }

    /// <summary>
    /// Reset daily email limits for all users
    /// </summary>
    public async Task ResetDailyLimits()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var userSettings = await dbContext.UserMailSettings
                .Where(s => s.SentToday > 0)
                .ToListAsync();

            foreach (var setting in userSettings)
            {
                setting.SentToday = 0;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Reset daily email limits for {Count} users", userSettings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting daily email limits");
        }
    }

    private async Task ProcessEmailAsync(EmailOutbox email, AppDbContext dbContext, IServiceProvider serviceProvider)
    {
        try
        {
            // Mark as processing
            email.Status = EmailStatus.Processing;
            email.LastAttemptAt = DateTime.UtcNow;
            email.Attempts++;
            await dbContext.SaveChangesAsync();

            // Log processing start
            await LogEmailActionAsync(dbContext, email.Id, EmailAction.Processing, 
                $"Processing attempt {email.Attempts} of {email.MaxAttempts}");

            // Get appropriate mail driver
            var mailDriver = await GetMailDriverAsync(email, serviceProvider, dbContext);
            
            if (mailDriver == null)
            {
                throw new InvalidOperationException($"No mail driver available for {email.MailDriver}");
            }

            // Check rate limits if user email
            if (email.SenderType == SenderType.User && !string.IsNullOrEmpty(email.SenderId))
            {
                var canSend = await CheckRateLimitAsync(email.SenderId, dbContext);
                if (!canSend)
                {
                    throw new InvalidOperationException("Daily email limit exceeded");
                }
            }

            // Create mail message
            var mailMessage = new MailMessage
            {
                ToEmail = email.ToEmail,
                CcEmail = email.CcEmail,
                BccEmail = email.BccEmail,
                FromEmail = email.FromEmail,
                FromName = email.FromName,
                Subject = email.Subject,
                HtmlBody = email.HtmlBody,
                TextBody = email.TextBody
            };

            // Send email
            var result = await mailDriver.SendAsync(mailMessage);

            if (result.Success)
            {
                // Mark as sent
                email.Status = EmailStatus.Sent;
                email.SentAt = result.SentAt ?? DateTime.UtcNow;
                email.UpdatedAt = DateTime.UtcNow;
                
                // Update user's sent count
                if (email.SenderType == SenderType.User && !string.IsNullOrEmpty(email.SenderId))
                {
                    await IncrementSentCountAsync(email.SenderId, dbContext);
                }

                await dbContext.SaveChangesAsync();
                await LogEmailActionAsync(dbContext, email.Id, EmailAction.Sent, 
                    $"Email sent successfully. Message ID: {result.MessageId}");

                _logger.LogInformation("Email {EmailId} sent successfully to {ToEmail}", 
                    email.Id, email.ToEmail);
            }
            else
            {
                // Check if we should retry
                if (email.Attempts >= email.MaxAttempts)
                {
                    // Mark as failed
                    email.Status = EmailStatus.Failed;
                    email.FailedReason = result.ErrorMessage;
                    email.UpdatedAt = DateTime.UtcNow;
                    
                    await dbContext.SaveChangesAsync();
                    await LogEmailActionAsync(dbContext, email.Id, EmailAction.Failed, 
                        $"Email failed after {email.Attempts} attempts. Error: {result.ErrorMessage}");

                    _logger.LogError("Email {EmailId} failed permanently after {Attempts} attempts. Error: {Error}", 
                        email.Id, email.Attempts, result.ErrorMessage);
                }
                else
                {
                    // Keep as pending for retry
                    email.Status = EmailStatus.Pending;
                    email.FailedReason = result.ErrorMessage;
                    email.UpdatedAt = DateTime.UtcNow;
                    
                    await dbContext.SaveChangesAsync();
                    await LogEmailActionAsync(dbContext, email.Id, EmailAction.Retried, 
                        $"Email failed, will retry. Attempt {email.Attempts} of {email.MaxAttempts}. Error: {result.ErrorMessage}");

                    _logger.LogWarning("Email {EmailId} failed, will retry. Attempt {Attempts}/{MaxAttempts}. Error: {Error}", 
                        email.Id, email.Attempts, email.MaxAttempts, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            // Mark as failed or pending based on attempts
            if (email.Attempts >= email.MaxAttempts)
            {
                email.Status = EmailStatus.Failed;
                email.FailedReason = ex.Message;
            }
            else
            {
                email.Status = EmailStatus.Pending;
                email.FailedReason = ex.Message;
            }
            
            email.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            await LogEmailActionAsync(dbContext, email.Id, EmailAction.Failed, 
                $"Error processing email: {ex.Message}");

            _logger.LogError(ex, "Error processing email {EmailId}", email.Id);
        }
    }

    private async Task<IMailDriver?> GetMailDriverAsync(EmailOutbox email, IServiceProvider serviceProvider, AppDbContext dbContext)
    {
        switch (email.MailDriver)
        {
            case MailDriver.SystemSmtp:
                return serviceProvider.GetService<SystemSmtpMailDriver>();
                
            case MailDriver.UserSmtp:
                // TODO: Implement UserSmtpMailDriver that uses user's SMTP settings
                // For now, fallback to system SMTP
                return serviceProvider.GetService<SystemSmtpMailDriver>();
                
            case MailDriver.SendGrid:
                // TODO: Implement SendGridMailDriver
                return null;
                
            case MailDriver.Mailgun:
                // TODO: Implement MailgunMailDriver
                return null;
                
            case MailDriver.AwsSes:
                // TODO: Implement AwsSesMailDriver
                return null;
                
            default:
                return serviceProvider.GetService<SystemSmtpMailDriver>();
        }
    }

    private async Task<bool> CheckRateLimitAsync(string userId, AppDbContext dbContext)
    {
        var userSettings = await dbContext.UserMailSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null || !userSettings.IsEnabled)
        {
            return false;
        }

        if (userSettings.DailyLimit.HasValue && userSettings.SentToday >= userSettings.DailyLimit.Value)
        {
            _logger.LogWarning("User {UserId} exceeded daily limit of {Limit} emails", 
                userId, userSettings.DailyLimit.Value);
            return false;
        }

        return true;
    }

    private async Task IncrementSentCountAsync(string userId, AppDbContext dbContext)
    {
        var userSettings = await dbContext.UserMailSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings != null)
        {
            userSettings.SentToday++;
            userSettings.LastSentAt = DateTime.UtcNow;
            userSettings.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task LogEmailActionAsync(AppDbContext dbContext, Guid emailId, EmailAction action, string? details)
    {
        try
        {
            var log = new EmailLog
            {
                Id = Guid.NewGuid(),
                EmailOutboxId = emailId,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.EmailLogs.Add(log);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email action for {EmailId}", emailId);
            // Don't throw - logging failure shouldn't stop email processing
        }
    }
}