namespace Kanriya.Client.Builder.Hooks;

public interface IBuildHook
{
    string Name { get; }
    int Priority { get; } // Lower numbers run first
    Task ExecuteAsync(BuildContext context);
}

public class BuildContext
{
    public string Platform { get; set; } = string.Empty;
    public bool Launch { get; set; }
    public bool SkipZip { get; set; }
    public bool Publish { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}