namespace Wock.Abstractions;

public sealed record WockPluginMetadata(
    string Id,
    string Name,
    string Version,
    string? Description = null,
    string? Author = null);
