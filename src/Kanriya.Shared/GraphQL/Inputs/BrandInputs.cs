namespace Kanriya.Shared.GraphQL.Inputs;

public class CreateBrandInput
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

public class UpdateBrandInput
{
    public string BrandId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public bool? IsActive { get; set; }
}

public class DeleteBrandInput
{
    public string BrandId { get; set; } = string.Empty;
}