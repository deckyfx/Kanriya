using System;
using System.ComponentModel.DataAnnotations;

namespace Kanriya.Server.Data;

public enum SmtpEncryption
{
    None,
    SSL,
    TLS
}

public class UserMailSettings
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;
    
    public MailDriver MailDriver { get; set; } = MailDriver.UserSmtp;
    
    public bool IsEnabled { get; set; } = true;
    
    // SMTP Settings (encrypted)
    [MaxLength(255)]
    public string? SmtpHost { get; set; }
    
    public int? SmtpPort { get; set; }
    
    [MaxLength(255)]
    public string? SmtpUsername { get; set; }
    
    public string? SmtpPassword { get; set; } // Will be encrypted
    
    public SmtpEncryption? SmtpEncryption { get; set; }
    
    [MaxLength(255)]
    public string? SmtpFromEmail { get; set; }
    
    [MaxLength(255)]
    public string? SmtpFromName { get; set; }
    
    // API Settings (encrypted)
    public string? ApiKey { get; set; } // Will be encrypted
    
    public string? ApiSecret { get; set; } // Will be encrypted
    
    [MaxLength(255)]
    public string? ApiDomain { get; set; } // For Mailgun
    
    [MaxLength(100)]
    public string? ApiRegion { get; set; } // For AWS SES
    
    // Rate limiting
    public int? DailyLimit { get; set; }
    
    public int SentToday { get; set; } = 0;
    
    public DateTime? LastSentAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}