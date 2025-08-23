using System;
using System.ComponentModel.DataAnnotations;

namespace GQLServer.Data;

public enum EmailAction
{
    Queued,
    Processing,
    Sent,
    Failed,
    Retried,
    Cancelled
}

public class EmailLog
{
    public Guid Id { get; set; }
    
    public Guid EmailOutboxId { get; set; }
    public EmailOutbox EmailOutbox { get; set; } = null!;
    
    public EmailAction Action { get; set; }
    
    public string? Details { get; set; }
    
    public DateTime CreatedAt { get; set; }
}