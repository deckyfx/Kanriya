using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kanriya.Server.Data;
using Kanriya.Server.Types;
using Kanriya.Server.Services;
using Kanriya.Server.Services.System;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Kanriya.Server.Queries;

/// <summary>
/// GraphQL queries for email operations
/// </summary>
[QueryType]
public class EmailQueries
{
    /// <summary>
    /// Get email status by ID
    /// </summary>
    [Authorize]
    public async Task<EmailOutbox?> GetEmailStatus(
        Guid emailId,
        [Service] IMailerService mailerService,
        [GlobalState] CurrentUser currentUser)
    {
        // TODO: Add authorization check to ensure user can only see their own emails
        return await mailerService.GetEmailStatusAsync(emailId);
    }

    /// <summary>
    /// Get email history for the current user
    /// </summary>
    [Authorize]
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<EmailOutbox> GetMyEmailHistory(
        [Service] AppDbContext dbContext,
        [GlobalState] CurrentUser currentUser)
    {
        var userId = currentUser.User?.Id;
        return dbContext.EmailOutboxes
            .Where(e => e.SenderId == userId)
            .OrderByDescending(e => e.CreatedAt);
    }

    /// <summary>
    /// Get all emails in the system - Admin only
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<EmailOutbox> GetAllEmails([Service] AppDbContext dbContext)
    {
        return dbContext.EmailOutboxes
            .OrderByDescending(e => e.CreatedAt);
    }

    /// <summary>
    /// Get email templates
    /// </summary>
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<EmailTemplate> GetEmailTemplates([Service] AppDbContext dbContext)
    {
        return dbContext.EmailTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name);
    }

    /// <summary>
    /// Get email logs for a specific email - Admin only
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<List<EmailLog>> GetEmailLogs(
        Guid emailId,
        [Service] AppDbContext dbContext)
    {
        return await dbContext.EmailLogs
            .Where(l => l.EmailOutboxId == emailId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get email statistics for the current user
    /// </summary>
    [Authorize]
    public async Task<EmailStatistics> GetMyEmailStatistics(
        [Service] AppDbContext dbContext,
        [GlobalState] CurrentUser currentUser)
    {
        var userId = currentUser.User?.Id;
        
        var emails = await dbContext.EmailOutboxes
            .Where(e => e.SenderId == userId)
            .ToListAsync();

        var userSettings = await dbContext.UserMailSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return new EmailStatistics
        {
            TotalSent = emails.Count(e => e.Status == EmailStatus.Sent),
            TotalPending = emails.Count(e => e.Status == EmailStatus.Pending),
            TotalFailed = emails.Count(e => e.Status == EmailStatus.Failed),
            TotalCancelled = emails.Count(e => e.Status == EmailStatus.Cancelled),
            SentToday = userSettings?.SentToday ?? 0,
            DailyLimit = userSettings?.DailyLimit ?? 0,
            LastSentAt = userSettings?.LastSentAt
        };
    }

    /// <summary>
    /// Get system-wide email statistics - Admin only
    /// </summary>
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<SystemEmailStatistics> GetSystemEmailStatistics(
        [Service] AppDbContext dbContext)
    {
        var emails = await dbContext.EmailOutboxes.ToListAsync();
        var last24Hours = DateTime.UtcNow.AddHours(-24);
        var last7Days = DateTime.UtcNow.AddDays(-7);
        var last30Days = DateTime.UtcNow.AddDays(-30);

        return new SystemEmailStatistics
        {
            TotalEmails = emails.Count,
            TotalSent = emails.Count(e => e.Status == EmailStatus.Sent),
            TotalPending = emails.Count(e => e.Status == EmailStatus.Pending),
            TotalProcessing = emails.Count(e => e.Status == EmailStatus.Processing),
            TotalFailed = emails.Count(e => e.Status == EmailStatus.Failed),
            TotalCancelled = emails.Count(e => e.Status == EmailStatus.Cancelled),
            SentLast24Hours = emails.Count(e => e.Status == EmailStatus.Sent && e.SentAt >= last24Hours),
            SentLast7Days = emails.Count(e => e.Status == EmailStatus.Sent && e.SentAt >= last7Days),
            SentLast30Days = emails.Count(e => e.Status == EmailStatus.Sent && e.SentAt >= last30Days),
            AverageProcessingTime = CalculateAverageProcessingTime(emails),
            SuccessRate = CalculateSuccessRate(emails)
        };
    }

    private static TimeSpan? CalculateAverageProcessingTime(List<EmailOutbox> emails)
    {
        var sentEmails = emails
            .Where(e => e.Status == EmailStatus.Sent && e.SentAt.HasValue)
            .Select(e => (e.SentAt!.Value - e.CreatedAt).TotalSeconds)
            .ToList();

        if (!sentEmails.Any())
            return null;

        return TimeSpan.FromSeconds(sentEmails.Average());
    }

    private static double CalculateSuccessRate(List<EmailOutbox> emails)
    {
        var completed = emails.Count(e => e.Status == EmailStatus.Sent || e.Status == EmailStatus.Failed);
        if (completed == 0)
            return 0;

        var successful = emails.Count(e => e.Status == EmailStatus.Sent);
        return (double)successful / completed * 100;
    }
}

/// <summary>
/// Email statistics for a user
/// </summary>
public class EmailStatistics
{
    public int TotalSent { get; set; }
    public int TotalPending { get; set; }
    public int TotalFailed { get; set; }
    public int TotalCancelled { get; set; }
    public int SentToday { get; set; }
    public int DailyLimit { get; set; }
    public DateTime? LastSentAt { get; set; }
}

/// <summary>
/// System-wide email statistics
/// </summary>
public class SystemEmailStatistics
{
    public int TotalEmails { get; set; }
    public int TotalSent { get; set; }
    public int TotalPending { get; set; }
    public int TotalProcessing { get; set; }
    public int TotalFailed { get; set; }
    public int TotalCancelled { get; set; }
    public int SentLast24Hours { get; set; }
    public int SentLast7Days { get; set; }
    public int SentLast30Days { get; set; }
    public TimeSpan? AverageProcessingTime { get; set; }
    public double SuccessRate { get; set; }
}