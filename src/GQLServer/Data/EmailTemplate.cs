using System;
using System.ComponentModel.DataAnnotations;

namespace GQLServer.Data;

public class EmailTemplate
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "welcome_email", "password_reset"
    
    [Required]
    [MaxLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty; // With placeholders like {{userName}}
    
    public string? HtmlBodyTemplate { get; set; }
    
    public string? TextBodyTemplate { get; set; }
    
    [MaxLength(255)]
    public string? DefaultFromEmail { get; set; }
    
    [MaxLength(255)]
    public string? DefaultFromName { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(255)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public ICollection<EmailOutbox> EmailOutboxes { get; set; } = new List<EmailOutbox>();
}