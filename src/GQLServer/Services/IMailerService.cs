using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GQLServer.Data;

namespace GQLServer.Services;

/// <summary>
/// Request to send an email
/// </summary>
public class SendEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public int Priority { get; set; } = 5;
    public DateTime? ScheduledFor { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? UserId { get; set; } // For user-specific mail settings
}

/// <summary>
/// Request to send a templated email
/// </summary>
public class SendTemplatedEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public int Priority { get; set; } = 5;
    public DateTime? ScheduledFor { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? UserId { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

/// <summary>
/// Response when an email is queued
/// </summary>
public class EmailQueuedResponse
{
    public Guid EmailId { get; set; }
    public EmailStatus Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing email operations
/// </summary>
public interface IMailerService
{
    /// <summary>
    /// Queue an email for sending
    /// </summary>
    Task<EmailQueuedResponse> QueueEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Queue a templated email for sending
    /// </summary>
    Task<EmailQueuedResponse> QueueTemplatedEmailAsync(SendTemplatedEmailRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send an email immediately (bypasses queue)
    /// </summary>
    Task<MailSendResult> SendImmediateAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a scheduled email
    /// </summary>
    Task<bool> CancelScheduledEmailAsync(Guid emailId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retry a failed email
    /// </summary>
    Task<bool> RetryFailedEmailAsync(Guid emailId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get email status
    /// </summary>
    Task<EmailOutbox?> GetEmailStatusAsync(Guid emailId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get email history for a user
    /// </summary>
    Task<List<EmailOutbox>> GetEmailHistoryAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process template with data
    /// </summary>
    Task<(string subject, string? htmlBody, string? textBody)> ProcessTemplateAsync(
        string templateName, 
        Dictionary<string, object> data, 
        CancellationToken cancellationToken = default);
}