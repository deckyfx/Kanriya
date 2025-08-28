namespace Kanriya.Shared.GraphQL.Payloads;

using Kanriya.Shared.GraphQL.Types;

public class CreateBrandOutput
{
    public bool Success { get; set; }
    public BrandType? Brand { get; set; }
    public string? Message { get; set; }
}

public class UpdateBrandOutput
{
    public bool Success { get; set; }
    public BrandType? Brand { get; set; }
    public string? Message { get; set; }
}

public class DeleteBrandOutput
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}