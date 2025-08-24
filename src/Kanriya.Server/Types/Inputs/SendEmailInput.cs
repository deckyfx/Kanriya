using System;
using System.Collections.Generic;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input type for sending an email
/// </summary>
public class SendEmailInput
{
    /// <summary>
    /// Recipient email address (required)
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// CC email addresses (comma-separated)
    /// </summary>
    public string? CcEmail { get; set; }
    
    /// <summary>
    /// BCC email addresses (comma-separated)
    /// </summary>
    public string? BccEmail { get; set; }
    
    /// <summary>
    /// From email address (uses system default if not provided)
    /// </summary>
    public string? FromEmail { get; set; }
    
    /// <summary>
    /// From display name
    /// </summary>
    public string? FromName { get; set; }
    
    /// <summary>
    /// Email subject (required)
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// HTML body of the email
    /// </summary>
    public string? HtmlBody { get; set; }
    
    /// <summary>
    /// Plain text body of the email
    /// </summary>
    public string? TextBody { get; set; }
    
    /// <summary>
    /// Priority (1-10, 1 being highest, default is 5)
    /// </summary>
    public int? Priority { get; set; }
    
    /// <summary>
    /// Schedule the email for later delivery
    /// </summary>
    public DateTime? ScheduledFor { get; set; }
}