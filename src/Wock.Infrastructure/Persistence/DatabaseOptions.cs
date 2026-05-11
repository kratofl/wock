namespace Wock.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = DatabaseOptionsExtensions.DefaultProviderName;

    public string? ConnectionString { get; set; }
}
