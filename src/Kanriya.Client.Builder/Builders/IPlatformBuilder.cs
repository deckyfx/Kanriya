using Spectre.Console;

namespace Kanriya.Client.Builder.Builders;

public interface IPlatformBuilder
{
    string PlatformName { get; }
    string[] Aliases { get; }
    Task<bool> CanBuildAsync();
    Task BuildAsync(bool launch, bool skipZip, ProgressTask? task = null);
    Task<bool> CanLaunchAsync();
    Task LaunchAsync(bool publish = false);
}