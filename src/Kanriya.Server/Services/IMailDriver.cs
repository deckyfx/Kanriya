using System.Threading.Tasks;

namespace Kanriya.Server.Services;

/// <summary>
/// Represents the result of sending an email
/// </summary>
public class MailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Represents an email message to be sent
/// </summary>
public class MailMessage
{
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public List<MailAttachment>? Attachments { get; set; }
}

/// <summary>
/// Represents an email attachment
/// </summary>
public class MailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Interface for mail drivers that handle actual email sending
/// </summary>
public interface IMailDriver
{
    /// <summary>
    /// Driver name for identification
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Send an email message
    /// </summary>
    Task<MailSendResult> SendAsync(MailMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate if the driver is properly configured
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test connection to the mail service
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}