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

    public Task<BookingConnectorLoadResult> LoadBookingConnectorAsync(
        InstalledPlugin installedPlugin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validation = ValidateAsync(installedPlugin, cancellationToken).GetAwaiter().GetResult();
            if (!validation.Success)
            {
                return Task.FromResult(BookingConnectorLoadResult.Fail(validation.Error!));
            }

            var assembly = Assembly.LoadFrom(installedPlugin.AssemblyPath);
            var pluginType = assembly.GetType(installedPlugin.TypeName, throwOnError: false)!;
            if (!typeof(IBookingConnector).IsAssignableFrom(pluginType))
            {
                return Task.FromResult(BookingConnectorLoadResult.Fail($"Plugin type does not implement {nameof(IBookingConnector)}: {installedPlugin.TypeName}"));
            }

            if (Activator.CreateInstance(pluginType) is not IBookingConnector connector)
            {
                return Task.FromResult(BookingConnectorLoadResult.Fail($"Plugin type could not be created: {installedPlugin.TypeName}"));
            }

            return Task.FromResult(BookingConnectorLoadResult.Ok(connector));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Task.FromResult(BookingConnectorLoadResult.Fail(exception.Message));
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

public sealed record BookingConnectorLoadResult(bool Success, IBookingConnector? Connector, string? Error)
{
    public static BookingConnectorLoadResult Ok(IBookingConnector connector)
    {
        return new BookingConnectorLoadResult(true, connector, null);
    }

    public static BookingConnectorLoadResult Fail(string error)
    {
        return new BookingConnectorLoadResult(false, null, error);
    }
}
