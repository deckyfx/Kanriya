using System.Text.Json.Serialization;

namespace Kanriya.Shared.Services;

/// <summary>
/// JSON serialization context for localization to support AOT/trimming
/// </summary>
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(string))]
internal partial class LocalizationJsonContext : JsonSerializerContext
{
}