using Microsoft.EntityFrameworkCore;
using Wock.Abstractions;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.Plugins;

public sealed class PluginRegistryService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    PluginInstallService installService,
    PluginLoader loader)
{
    public async Task<IReadOnlyList<InstalledPlugin>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.InstalledPlugins
            .OrderBy(plugin => plugin.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<InstalledPlugin> InstallAsync(
        string pluginFolderPath,
        CancellationToken cancellationToken = default)
    {
        return InstallFromFolderAsync(pluginFolderPath, cancellationToken);
    }

    public async Task<InstalledPlugin> InstallFromFolderAsync(
        string pluginFolderPath,
        CancellationToken cancellationToken = default)
    {
        var package = await installService.InstallFromFolderAsync(pluginFolderPath, cancellationToken);
        return await SaveInstalledPackageAsync(package, cancellationToken);
    }

    public async Task<InstalledPlugin> InstallFromZipAsync(
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        var package = await installService.InstallFromZipAsync(zipPath, cancellationToken);
        return await SaveInstalledPackageAsync(package, cancellationToken);
    }

    public async Task<IReadOnlyList<IBookingConnector>> GetEnabledBookingConnectorsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var installedPlugins = await dbContext.InstalledPlugins
            .AsNoTracking()
            .Where(plugin => plugin.IsEnabled)
            .OrderBy(plugin => plugin.Name)
            .ToListAsync(cancellationToken);

        var connectors = new List<IBookingConnector>();
        foreach (var installedPlugin in installedPlugins)
        {
            var result = await loader.LoadBookingConnectorAsync(installedPlugin, cancellationToken);
            if (result.Success)
            {
                connectors.Add(result.Connector!);
            }
            else
            {
                await UpdateLoadStatusAsync(installedPlugin.Id, PluginLoadStatus.Failed, result.Error, cancellationToken);
            }
        }

        return connectors;
    }

    private async Task<InstalledPlugin> SaveInstalledPackageAsync(
        InstalledPluginPackage package,
        CancellationToken cancellationToken)
    {
        var manifest = package.Manifest;
        var now = DateTime.UtcNow;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var installed = await dbContext.InstalledPlugins
            .SingleOrDefaultAsync(plugin => plugin.PluginId == manifest.Id, cancellationToken);

        if (installed is null)
        {
            installed = new InstalledPlugin
            {
                PluginId = manifest.Id,
                InstalledAt = now,
                IsEnabled = false,
                LastLoadStatus = PluginLoadStatus.NotLoaded
            };
            dbContext.InstalledPlugins.Add(installed);
        }

        installed.Name = manifest.Name;
        installed.Version = manifest.Version;
        installed.Description = manifest.Description;
        installed.AssemblyPath = package.AssemblyPath;
        installed.TypeName = manifest.Type;
        installed.IsEnabled = false;
        installed.LastLoadStatus = package.Validation.Success ? PluginLoadStatus.NotLoaded : PluginLoadStatus.Failed;
        installed.LastLoadError = package.Validation.Error;

        await dbContext.SaveChangesAsync(cancellationToken);
        return installed;
    }

    public async Task<InstalledPlugin> SetEnabledAsync(
        int installedPluginId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var installed = await dbContext.InstalledPlugins
            .SingleAsync(plugin => plugin.Id == installedPluginId, cancellationToken);

        if (isEnabled)
        {
            var validation = await loader.ValidateAsync(installed, cancellationToken);
            installed.IsEnabled = validation.Success;
            installed.LastLoadStatus = validation.Success ? PluginLoadStatus.Loaded : PluginLoadStatus.Failed;
            installed.LastLoadError = validation.Error;
        }
        else
        {
            installed.IsEnabled = false;
            installed.LastLoadStatus = PluginLoadStatus.Disabled;
            installed.LastLoadError = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return installed;
    }

    public async Task UpdateLoadStatusAsync(
        int installedPluginId,
        string status,
        string? error,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var installed = await dbContext.InstalledPlugins
            .SingleAsync(plugin => plugin.Id == installedPluginId, cancellationToken);

        installed.LastLoadStatus = status;
        installed.LastLoadError = error;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
