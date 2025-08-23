using GQLServer.Data;

namespace GQLServer.Types.Outputs;

/// <summary>
/// Response for email template operations
/// </summary>
public class EmailTemplateResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The email template (if applicable)
    /// </summary>
    public EmailTemplate? Template { get; set; }
}

/// <summary>
/// Response for email template testing
/// </summary>
public class EmailTestResponse
{
    /// <summary>
    /// Whether the test was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the queued test email
    /// </summary>
    public Guid? EmailId { get; set; }
    
    /// <summary>
    /// Position in the email queue
    /// </summary>
    public int? QueuePosition { get; set; }
}