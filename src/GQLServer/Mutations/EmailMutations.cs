using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GQLServer.Data;
using GQLServer.Types;
using GQLServer.Services;
using GQLServer.Types.Inputs;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;

namespace GQLServer.Mutations;

/// <summary>
/// GraphQL mutations for email operations
/// </summary>
[MutationType]
public class EmailMutations
{
    private readonly IMailerService _mailerService;
    private readonly ILogger<EmailMutations> _logger;

    public EmailMutations(IMailerService mailerService, ILogger<EmailMutations> logger)
    {
        _mailerService = mailerService;
        _logger = logger;
    }

    /// <summary>
    /// Send an email (queues it for processing)
    /// </summary>
    [Authorize]
    public async Task<EmailQueuedResponse> SendEmail(
        SendEmailInput input,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("User {UserId} is sending email to {ToEmail}", 
            currentUser.User?.Id, input.ToEmail);

        var request = new SendEmailRequest
        {
            ToEmail = input.ToEmail,
            CcEmail = input.CcEmail,
            BccEmail = input.BccEmail,
            FromEmail = input.FromEmail,
            FromName = input.FromName,
            Subject = input.Subject,
            HtmlBody = input.HtmlBody,
            TextBody = input.TextBody,
            Priority = input.Priority ?? 5,
            ScheduledFor = input.ScheduledFor,
            UserId = currentUser.User?.Id
        };

        return await _mailerService.QueueEmailAsync(request);
    }

    /// <summary>
    /// Send a templated email
    /// </summary>
    [Authorize]
    public async Task<EmailQueuedResponse> SendTemplatedEmail(
        SendTemplatedEmailInput input,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("User {UserId} is sending templated email {Template} to {ToEmail}", 
            currentUser.User?.Id, input.TemplateName, input.ToEmail);

        var request = new SendTemplatedEmailRequest
        {
            ToEmail = input.ToEmail,
            CcEmail = input.CcEmail,
            BccEmail = input.BccEmail,
            TemplateName = input.TemplateName,
            TemplateData = input.TemplateData ?? new Dictionary<string, object>(),
            Priority = input.Priority ?? 5,
            ScheduledFor = input.ScheduledFor,
            FromEmail = input.FromEmail,
            FromName = input.FromName,
            UserId = currentUser.User?.Id
        };

        return await _mailerService.QueueTemplatedEmailAsync(request);
    }

    /// <summary>
    /// Send an email immediately (bypasses queue) - Admin only
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<MailSendResult> SendImmediateEmail(
        SendEmailInput input,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("Admin {UserId} is sending immediate email to {ToEmail}", 
            currentUser.User?.Id, input.ToEmail);

        var request = new SendEmailRequest
        {
            ToEmail = input.ToEmail,
            CcEmail = input.CcEmail,
            BccEmail = input.BccEmail,
            FromEmail = input.FromEmail,
            FromName = input.FromName,
            Subject = input.Subject,
            HtmlBody = input.HtmlBody,
            TextBody = input.TextBody,
            UserId = currentUser.User?.Id
        };

        return await _mailerService.SendImmediateAsync(request);
    }

    /// <summary>
    /// Cancel a scheduled email
    /// </summary>
    [Authorize]
    public async Task<bool> CancelEmail(
        Guid emailId,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("User {UserId} is cancelling email {EmailId}", 
            currentUser.User?.Id, emailId);

        // TODO: Add authorization check to ensure user can only cancel their own emails
        return await _mailerService.CancelScheduledEmailAsync(emailId);
    }

    /// <summary>
    /// Retry a failed email
    /// </summary>
    [Authorize]
    public async Task<bool> RetryEmail(
        Guid emailId,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("User {UserId} is retrying email {EmailId}", 
            currentUser.User?.Id, emailId);

        // TODO: Add authorization check to ensure user can only retry their own emails
        return await _mailerService.RetryFailedEmailAsync(emailId);
    }

    /// <summary>
    /// Send a test email - Admin only
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<EmailQueuedResponse> SendTestEmail(
        string toEmail,
        [GlobalState] CurrentUser currentUser)
    {
        _logger.LogInformation("Admin {UserId} is sending test email to {ToEmail}", 
            currentUser.User?.Id, toEmail);

        var request = new SendEmailRequest
        {
            ToEmail = toEmail,
            Subject = "Test Email from GQL Server",
            HtmlBody = $"""
                <html>
                <body>
                    <h2>Test Email</h2>
                    <p>This is a test email sent from the GQL Server.</p>
                    <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p><strong>Sent by:</strong> {currentUser.User?.Email}</p>
                    <hr>
                    <p style="color: #666; font-size: 12px;">
                        If you received this email, your email configuration is working correctly.
                    </p>
                </body>
                </html>
                """,
            TextBody = $"This is a test email sent from the GQL Server at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            Priority = 1,
            UserId = currentUser.User?.Id
        };

        return await _mailerService.QueueEmailAsync(request);
    }
}