namespace GQLServer.Types.Outputs;

// OUTPUT TYPE - Used as return type from queries/mutations
// =========================================================
// Output types represent data that GraphQL returns to clients

public class SimpleResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}