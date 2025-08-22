namespace GQLServer.Types.Inputs;

// INPUT TYPE - Used as parameters for mutations
// ==============================================
// Input types group related parameters together

public class CreateMessageInput
{
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool IsImportant { get; set; }
}