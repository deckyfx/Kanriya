using System;
using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

public enum SenderType
{
    System,
    User
}

public enum MailDriver
{
    SystemSmtp,
    UserSmtp,
    SendGrid,
    Mailgun,
    AwsSes
}

public enum EmailStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Cancelled
}

public class EmailOutbox
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ToEmail { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? CcEmail { get; set; }
    
    [MaxLength(255)]
    public string? BccEmail { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FromEmail { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? FromName { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    public string? HtmlBody { get; set; }
    
    public string? TextBody { get; set; }
    
    public Guid? TemplateId { get; set; }
    public EmailTemplate? Template { get; set; }
    
    public string? TemplateData { get; set; } // JSON data
    
    public SenderType SenderType { get; set; }
    
    [MaxLength(255)]
    public string? SenderId { get; set; } // User ID if sender_type is User
    
    public MailDriver MailDriver { get; set; }
    
    public int Priority { get; set; } = 5; // 1-10, 1 being highest
    
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    
    public int Attempts { get; set; } = 0;
    
    public int MaxAttempts { get; set; } = 3;
    
    public DateTime? LastAttemptAt { get; set; }
    
    public DateTime? SentAt { get; set; }
    
    public string? FailedReason { get; set; }
    
    public DateTime? ScheduledFor { get; set; }
    
    public string? Metadata { get; set; } // JSON data
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}