using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wock.Features.Plugins;

public sealed record PluginManifest(
    string Id,
    string Name,
    string Version,
    string Assembly,
    string Type,
    string? Description,
    string? Author)
{
    public const string FileName = "wock-plugin.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<PluginManifest> LoadFromFileAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(manifestPath);
        var dto = await JsonSerializer.DeserializeAsync<PluginManifestDto>(
            stream,
            SerializerOptions,
            cancellationToken);

        if (dto is null)
        {
            throw new PluginManifestException("Plugin manifest is empty or invalid JSON.");
        }

        ValidateRequired(nameof(dto.Id), dto.Id, 200);
        ValidateRequired(nameof(dto.Name), dto.Name, 200);
        ValidateRequired(nameof(dto.Version), dto.Version, 100);
        ValidateRequired(nameof(dto.Assembly), dto.Assembly, 1000);
        ValidateRequired(nameof(dto.Type), dto.Type, 500);
        ValidateOptional(nameof(dto.Description), dto.Description, 2000);
        ValidateOptional(nameof(dto.Author), dto.Author, 200);
        ValidateAssemblyPath(dto.Assembly!);

        return new PluginManifest(
            dto.Id!.Trim(),
            dto.Name!.Trim(),
            dto.Version!.Trim(),
            dto.Assembly!.Trim(),
            dto.Type!.Trim(),
            TrimOrNull(dto.Description),
            TrimOrNull(dto.Author));
    }

    private static void ValidateRequired(string fieldName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PluginManifestException($"Plugin manifest field '{fieldName}' is required.");
        }

        ValidateOptional(fieldName, value, maxLength);
    }

    private static void ValidateOptional(string fieldName, string? value, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            throw new PluginManifestException($"Plugin manifest field '{fieldName}' must be {maxLength} characters or fewer.");
        }
    }

    private static void ValidateAssemblyPath(string assemblyPath)
    {
        var trimmed = assemblyPath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            throw new PluginManifestException("Plugin manifest assembly path must be relative.");
        }

        var parts = trimmed.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => part == ".."))
        {
            throw new PluginManifestException("Plugin manifest assembly path must not contain path traversal.");
        }
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class PluginManifestDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("assembly")]
        public string? Assembly { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }
    }
}

public sealed class PluginManifestException(string message) : InvalidOperationException(message);
