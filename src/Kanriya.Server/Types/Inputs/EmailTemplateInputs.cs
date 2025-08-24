namespace Kanriya.Server.Types.Inputs;

/// <summary>
/// Input for creating a new email template
/// </summary>
public class CreateEmailTemplateInput
{
    /// <summary>
    /// Unique name for the template
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Email subject template (supports variables)
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// HTML body template (supports variables)
    /// </summary>
    public string? HtmlBody { get; set; }
    
    /// <summary>
    /// Plain text body template (supports variables)
    /// </summary>
    public string? TextBody { get; set; }
    
    /// <summary>
    /// Template category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// JSON string of required variables
    /// </summary>
    public string? Variables { get; set; }
    
    /// <summary>
    /// Whether the template is active
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Input for updating an email template
/// </summary>
public class UpdateEmailTemplateInput
{
    /// <summary>
    /// Template ID to update
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Updated template name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Updated email subject template
    /// </summary>
    public string? Subject { get; set; }
    
    /// <summary>
    /// Updated HTML body template
    /// </summary>
    public string? HtmlBody { get; set; }
    
    /// <summary>
    /// Updated plain text body template
    /// </summary>
    public string? TextBody { get; set; }
    
    /// <summary>
    /// Updated category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Updated variables JSON
    /// </summary>
    public string? Variables { get; set; }
    
    /// <summary>
    /// Updated active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Input for cloning an email template
/// </summary>
public class CloneEmailTemplateInput
{
    /// <summary>
    /// Source template ID to clone from
    /// </summary>
    public Guid SourceId { get; set; }
    
    /// <summary>
    /// Name for the cloned template
    /// </summary>
    public string NewName { get; set; } = string.Empty;
}

/// <summary>
/// Input for testing an email template
/// </summary>
public class TestEmailTemplateInput
{
    /// <summary>
    /// Template ID to test
    /// </summary>
    public Guid TemplateId { get; set; }
    
    /// <summary>
    /// Email address to send test to
    /// </summary>
    public string TestEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Test variables to use in the template
    /// </summary>
    public Dictionary<string, string>? TestVariables { get; set; }
}