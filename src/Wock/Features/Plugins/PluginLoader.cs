using System.Reflection;
using Wock.Abstractions;
using Wock.Models;

namespace Wock.Features.Plugins;

public sealed class PluginLoader
{
    public Task<PluginLoadResult> ValidateAsync(
        InstalledPlugin installedPlugin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(installedPlugin.AssemblyPath))
            {
                return Task.FromResult(PluginLoadResult.Fail($"Plugin assembly was not found: {installedPlugin.AssemblyPath}"));
            }

            var assembly = Assembly.LoadFrom(installedPlugin.AssemblyPath);
            var pluginType = assembly.GetType(installedPlugin.TypeName, throwOnError: false);
            if (pluginType is null)
            {
                return Task.FromResult(PluginLoadResult.Fail($"Plugin type was not found: {installedPlugin.TypeName}"));
            }

            if (!typeof(IWockPlugin).IsAssignableFrom(pluginType))
            {
                return Task.FromResult(PluginLoadResult.Fail($"Plugin type does not implement {nameof(IWockPlugin)}: {installedPlugin.TypeName}"));
            }

            return Task.FromResult(PluginLoadResult.Ok());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Task.FromResult(PluginLoadResult.Fail(exception.Message));
        }
    }
}

public sealed record PluginLoadResult(bool Success, string? Error)
{
    public static PluginLoadResult Ok()
    {
        return new PluginLoadResult(true, null);
    }

    public static PluginLoadResult Fail(string error)
    {
        return new PluginLoadResult(false, error);
    }
}
