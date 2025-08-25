namespace Kanriya.Shared.GraphQL.Types;

public class UserType
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}