using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.Plugins;

public sealed class PluginRegistryService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<InstalledPlugin>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.InstalledPlugins
            .OrderBy(plugin => plugin.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<InstalledPlugin> InstallAsync(
        string pluginFolderPath,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.GetFullPath(pluginFolderPath);
        var manifestPath = Path.Combine(folderPath, PluginManifest.FileName);
        var manifest = await PluginManifest.LoadFromFileAsync(manifestPath, cancellationToken);
        var assemblyPath = Path.GetFullPath(Path.Combine(folderPath, manifest.Assembly));
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
        installed.AssemblyPath = assemblyPath;
        installed.TypeName = manifest.Type;
        installed.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return installed;
    }

    public async Task SetEnabledAsync(
        int installedPluginId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var installed = await dbContext.InstalledPlugins
            .SingleAsync(plugin => plugin.Id == installedPluginId, cancellationToken);

        installed.IsEnabled = isEnabled;
        installed.LastLoadStatus = isEnabled ? PluginLoadStatus.NotLoaded : PluginLoadStatus.Disabled;
        installed.LastLoadError = null;
        installed.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
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
        installed.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
