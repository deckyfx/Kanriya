using System;
using System.Collections.Generic;

namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input type for sending a templated email
/// </summary>
public class SendTemplatedEmailInput
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
    /// Template name to use (required)
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Template data for variable substitution
    /// </summary>
    public Dictionary<string, object>? TemplateData { get; set; }
    
    /// <summary>
    /// From email address (uses template default if not provided)
    /// </summary>
    public string? FromEmail { get; set; }
    
    /// <summary>
    /// From display name (uses template default if not provided)
    /// </summary>
    public string? FromName { get; set; }
    
    /// <summary>
    /// Priority (1-10, 1 being highest, default is 5)
    /// </summary>
    public int? Priority { get; set; }
    
    /// <summary>
    /// Schedule the email for later delivery
    /// </summary>
    public DateTime? ScheduledFor { get; set; }
}