using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kanriya.Server.Data;
using Kanriya.Server.Program;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kanriya.Server.Services.System;

/// <summary>
/// Service for managing email operations
/// </summary>
public class MailerService : IMailerService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MailerService> _logger;
    private readonly IMailDriver _systemMailDriver;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public MailerService(
        AppDbContext dbContext,
        ILogger<MailerService> logger,
        IMailDriver systemMailDriver,
        IBackgroundJobClient backgroundJobClient)
    {
        _dbContext = dbContext;
        _logger = logger;
        _systemMailDriver = systemMailDriver;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<EmailQueuedResponse> QueueEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.ToEmail))
            {
                throw new ArgumentException("Recipient email is required");
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                throw new ArgumentException("Email subject is required");
            }

            if (string.IsNullOrWhiteSpace(request.HtmlBody) && string.IsNullOrWhiteSpace(request.TextBody))
            {
                throw new ArgumentException("Email body (HTML or text) is required");
            }

            // Create email outbox entry
            var emailOutbox = new EmailOutbox
            {
                Id = Guid.NewGuid(),
                ToEmail = request.ToEmail,
                CcEmail = request.CcEmail,
                BccEmail = request.BccEmail,
                FromEmail = request.FromEmail ?? "noreply@example.com",
                FromName = request.FromName,
                Subject = request.Subject,
                HtmlBody = request.HtmlBody,
                TextBody = request.TextBody,
                Priority = request.Priority,
                ScheduledFor = request.ScheduledFor,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                SenderId = request.UserId,
                SenderType = string.IsNullOrEmpty(request.UserId) ? SenderType.System : SenderType.User,
                MailDriver = string.IsNullOrEmpty(request.UserId) ? MailDriver.SystemSmtp : MailDriver.UserSmtp,
                Status = EmailStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            _dbContext.EmailOutboxes.Add(emailOutbox);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Log the queued email
            await LogEmailActionAsync(emailOutbox.Id, EmailAction.Queued, "Email queued for processing", cancellationToken);

            // Schedule immediate processing if not scheduled for later
            if (!request.ScheduledFor.HasValue || request.ScheduledFor.Value <= DateTime.UtcNow)
            {
                // Queue for immediate processing
                _backgroundJobClient.Enqueue<IMailProcessor>(processor => 
                    processor.ProcessSpecificEmail(emailOutbox.Id));
            }
            else
            {
                // Schedule for later
                _backgroundJobClient.Schedule<IMailProcessor>(
                    processor => processor.ProcessSpecificEmail(emailOutbox.Id),
                    request.ScheduledFor.Value);
            }

            _logger.LogInformation("Email {EmailId} queued successfully for {ToEmail}", 
                emailOutbox.Id, request.ToEmail);

            return new EmailQueuedResponse
            {
                EmailId = emailOutbox.Id,
                Status = emailOutbox.Status,
                ScheduledFor = emailOutbox.ScheduledFor,
                Message = emailOutbox.ScheduledFor.HasValue 
                    ? $"Email scheduled for {emailOutbox.ScheduledFor.Value:yyyy-MM-dd HH:mm:ss} UTC"
                    : "Email queued for immediate processing"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email for {ToEmail}", request.ToEmail);
            throw;
        }
    }

    public async Task<EmailQueuedResponse> QueueTemplatedEmailAsync(SendTemplatedEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Process template
            var (subject, htmlBody, textBody) = await ProcessTemplateAsync(
                request.TemplateName, 
                request.TemplateData, 
                cancellationToken);

            // Create regular email request
            var emailRequest = new SendEmailRequest
            {
                ToEmail = request.ToEmail,
                CcEmail = request.CcEmail,
                BccEmail = request.BccEmail,
                FromEmail = request.FromEmail,
                FromName = request.FromName,
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                Priority = request.Priority,
                ScheduledFor = request.ScheduledFor,
                Metadata = request.Metadata,
                UserId = request.UserId
            };

            // Queue the email
            return await QueueEmailAsync(emailRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue templated email {TemplateName} for {ToEmail}", 
                request.TemplateName, request.ToEmail);
            throw;
        }
    }

    public async Task<MailSendResult> SendImmediateAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create mail message
            var mailMessage = new MailMessage
            {
                ToEmail = request.ToEmail,
                CcEmail = request.CcEmail,
                BccEmail = request.BccEmail,
                FromEmail = request.FromEmail ?? "noreply@example.com",
                FromName = request.FromName,
                Subject = request.Subject,
                HtmlBody = request.HtmlBody,
                TextBody = request.TextBody
            };

            // Send immediately using system driver
            var result = await _systemMailDriver.SendAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Email sent immediately to {ToEmail} with result: {Success}", 
                request.ToEmail, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send immediate email to {ToEmail}", request.ToEmail);
            throw;
        }
    }

    public async Task<bool> CancelScheduledEmailAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _dbContext.EmailOutboxes.FindAsync(new object[] { emailId }, cancellationToken);
            
            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found for cancellation", emailId);
                return false;
            }

            if (email.Status != EmailStatus.Pending)
            {
                _logger.LogWarning("Cannot cancel email {EmailId} with status {Status}", emailId, email.Status);
                return false;
            }

            // Update status
            email.Status = EmailStatus.Cancelled;
            email.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await LogEmailActionAsync(emailId, EmailAction.Cancelled, "Email cancelled by user", cancellationToken);

            _logger.LogInformation("Email {EmailId} cancelled successfully", emailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel email {EmailId}", emailId);
            throw;
        }
    }

    public async Task<bool> RetryFailedEmailAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _dbContext.EmailOutboxes.FindAsync(new object[] { emailId }, cancellationToken);
            
            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found for retry", emailId);
                return false;
            }

            if (email.Status != EmailStatus.Failed)
            {
                _logger.LogWarning("Cannot retry email {EmailId} with status {Status}", emailId, email.Status);
                return false;
            }

            // Reset status and increment attempts
            email.Status = EmailStatus.Pending;
            email.Attempts = 0; // Reset attempts for manual retry
            email.UpdatedAt = DateTime.UtcNow;
            email.FailedReason = null;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await LogEmailActionAsync(emailId, EmailAction.Retried, "Email manually retried", cancellationToken);

            // Queue for immediate processing
            _backgroundJobClient.Enqueue<IMailProcessor>(processor => 
                processor.ProcessSpecificEmail(emailId));

            _logger.LogInformation("Email {EmailId} queued for retry", emailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry email {EmailId}", emailId);
            throw;
        }
    }

    public async Task<EmailOutbox?> GetEmailStatusAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.EmailOutboxes
                .Include(e => e.Template)
                .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for email {EmailId}", emailId);
            throw;
        }
    }

    public async Task<List<EmailOutbox>> GetEmailHistoryAsync(string userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.EmailOutboxes
                .Where(e => e.SenderId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(string subject, string? htmlBody, string? textBody)> ProcessTemplateAsync(
        string templateName, 
        Dictionary<string, object> data, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get template from database
            var template = await _dbContext.EmailTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, cancellationToken);

            if (template == null)
            {
                throw new InvalidOperationException($"Email template '{templateName}' not found or inactive");
            }

            // Process template variables (simple replacement for now)
            var subject = ProcessTemplateVariables(template.SubjectTemplate, data);
            var htmlBody = template.HtmlBodyTemplate != null 
                ? ProcessTemplateVariables(template.HtmlBodyTemplate, data) 
                : null;
            var textBody = template.TextBodyTemplate != null 
                ? ProcessTemplateVariables(template.TextBodyTemplate, data) 
                : null;

            return (subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process template {TemplateName}", templateName);
            throw;
        }
    }

    private string ProcessTemplateVariables(string template, Dictionary<string, object> data)
    {
        var result = template;

        // Simple variable replacement using {{variable}} syntax
        foreach (var kvp in data)
        {
            var pattern = $@"{{{{{kvp.Key}}}}}";
            var value = kvp.Value?.ToString() ?? "";
            result = Regex.Replace(result, pattern, value, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private async Task LogEmailActionAsync(Guid emailId, EmailAction action, string? details, CancellationToken cancellationToken)
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

            _dbContext.EmailLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email action for {EmailId}", emailId);
            // Don't throw - logging failure shouldn't stop email processing
        }
    }
    
    public async Task<EmailQueuedResponse> SendPasswordResetEmailAsync(
        string email, 
        string resetToken, 
        string resetLink, 
        CancellationToken cancellationToken = default)
    {
        // Use the templated email
        var templateData = new Dictionary<string, object>
        {
            { "userName", email }, // Use email as username since we might not have the name
            { "appName", "Kanriya" },
            { "resetUrl", resetLink },
            { "resetToken", resetToken },
            { "expiryHours", "1" },
            { "year", DateTime.UtcNow.Year.ToString() }
        };
        
        var request = new SendTemplatedEmailRequest
        {
            ToEmail = email,
            TemplateName = "password_reset",
            TemplateData = templateData,
            Priority = 1 // High priority
        };
        
        return await QueueTemplatedEmailAsync(request, cancellationToken);
    }
    
    public async Task<EmailQueuedResponse> SendVerificationEmailAsync(
        string email, 
        string verificationToken, 
        string verificationLink, 
        CancellationToken cancellationToken = default)
    {
        // Use the templated email
        var templateData = new Dictionary<string, object>
        {
            { "userName", email }, // Use email as username since we might not have the name
            { "appName", "Kanriya" },
            { "activationUrl", verificationLink },
            { "verificationToken", verificationToken },
            { "year", DateTime.UtcNow.Year.ToString() }
        };
        
        var request = new SendTemplatedEmailRequest
        {
            ToEmail = email,
            TemplateName = "user_activation",
            TemplateData = templateData,
            Priority = 1 // High priority
        };
        
        return await QueueTemplatedEmailAsync(request, cancellationToken);
    }
}