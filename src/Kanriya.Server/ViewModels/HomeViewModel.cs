namespace Kanriya.Server.ViewModels;

/// <summary>
/// View model for the home page
/// </summary>
public class HomeViewModel
{
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string GraphQLEndpoint { get; set; } = "";
    public string SwaggerEndpoint { get; set; } = "";
    public Feature[] Features { get; set; } = Array.Empty<Feature>();
    public EndpointInfo[] Endpoints { get; set; } = Array.Empty<EndpointInfo>();
}

public class Feature
{
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
}

public class EndpointInfo
{
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
}