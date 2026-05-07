namespace Wock.Abstractions;

public interface IWockPlugin
{
    WockPluginMetadata Metadata { get; }

    Task InitializeAsync(IWockPluginHost host, CancellationToken cancellationToken = default);
}

public interface IWockPluginHost
{
    IServiceProvider Services { get; }
}
