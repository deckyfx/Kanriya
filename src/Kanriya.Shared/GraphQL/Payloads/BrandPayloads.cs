namespace Kanriya.Shared.GraphQL.Payloads;

using Kanriya.Shared.GraphQL.Types;

public class CreateBrandPayload
{
    public bool Success { get; set; }
    public BrandType? Brand { get; set; }
    public string? Message { get; set; }
}

public class UpdateBrandPayload
{
    public bool Success { get; set; }
    public BrandType? Brand { get; set; }
    public string? Message { get; set; }
}

public class DeleteBrandPayload
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}