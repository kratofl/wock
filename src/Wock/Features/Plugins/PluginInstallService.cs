using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace Wock.Features.Plugins;

public sealed class PluginInstallOptions
{
    public string StoragePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "plugins");
}

public sealed record InstalledPluginPackage(
    PluginManifest Manifest,
    string InstallDirectory,
    string AssemblyPath,
    PluginLoadResult Validation);

public sealed class PluginInstallService(IOptions<PluginInstallOptions> options, PluginLoader loader)
{
    public async Task<InstalledPluginPackage> InstallFromFolderAsync(
        string pluginFolderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginFolderPath))
        {
            throw new InvalidOperationException("Plugin folder path is required.");
        }

        var sourcePath = Path.GetFullPath(pluginFolderPath);
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Plugin folder was not found: {sourcePath}");
        }

        var manifest = await PluginManifest.LoadFromFileAsync(
            Path.Combine(sourcePath, PluginManifest.FileName),
            cancellationToken);
        var stagingPath = CreateStagingDirectory();

        try
        {
            CopyDirectory(sourcePath, stagingPath, cancellationToken);
            return await CompleteInstallAsync(stagingPath, manifest, cancellationToken);
        }
        catch
        {
            DeleteDirectoryIfExists(stagingPath);
            throw;
        }
    }

    public async Task<InstalledPluginPackage> InstallFromZipAsync(
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new InvalidOperationException("Plugin ZIP path is required.");
        }

        var fullZipPath = Path.GetFullPath(zipPath);
        if (!File.Exists(fullZipPath))
        {
            throw new FileNotFoundException($"Plugin ZIP was not found: {fullZipPath}", fullZipPath);
        }

        var stagingPath = CreateStagingDirectory();
        try
        {
            ExtractZipSafely(fullZipPath, stagingPath, cancellationToken);
            var manifest = await PluginManifest.LoadFromFileAsync(
                Path.Combine(stagingPath, PluginManifest.FileName),
                cancellationToken);
            return await CompleteInstallAsync(stagingPath, manifest, cancellationToken);
        }
        catch
        {
            DeleteDirectoryIfExists(stagingPath);
            throw;
        }
    }

    private async Task<InstalledPluginPackage> CompleteInstallAsync(
        string stagingPath,
        PluginManifest manifest,
        CancellationToken cancellationToken)
    {
        ValidateSafePluginId(manifest.Id);

        var storageRoot = GetStorageRoot();
        var installDirectory = GetSafeInstallDirectory(storageRoot, manifest.Id);
        DeleteDirectoryIfExists(installDirectory);
        Directory.Move(stagingPath, installDirectory);

        var assemblyPath = Path.GetFullPath(Path.Combine(installDirectory, manifest.Assembly));
        var installedPlugin = new Models.InstalledPlugin
        {
            PluginId = manifest.Id,
            Name = manifest.Name,
            Version = manifest.Version,
            Description = manifest.Description,
            AssemblyPath = assemblyPath,
            TypeName = manifest.Type
        };
        var validation = await loader.ValidateAsync(installedPlugin, cancellationToken);

        return new InstalledPluginPackage(manifest, installDirectory, assemblyPath, validation);
    }

    private string CreateStagingDirectory()
    {
        var storageRoot = GetStorageRoot();
        var stagingPath = Path.Combine(storageRoot, $".install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingPath);
        return stagingPath;
    }

    private string GetStorageRoot()
    {
        var storageRoot = Path.GetFullPath(options.Value.StoragePath);
        Directory.CreateDirectory(storageRoot);
        return storageRoot;
    }

    private static void CopyDirectory(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var directory in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourcePath, directory);
            Directory.CreateDirectory(Path.Combine(destinationPath, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var destinationFile = Path.Combine(destinationPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }

    private static void ExtractZipSafely(string zipPath, string destinationPath, CancellationToken cancellationToken)
    {
        var destinationRoot = Path.GetFullPath(destinationPath);
        var destinationRootWithSeparator = EnsureTrailingSeparator(destinationRoot);

        using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destinationFile = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
            if (!destinationFile.StartsWith(destinationRootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(destinationFile, destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Plugin ZIP contains an entry with path traversal.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationFile);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            entry.ExtractToFile(destinationFile, overwrite: true);
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    private static void ValidateSafePluginId(string pluginId)
    {
        if (pluginId is "." or ".." ||
            pluginId.Contains("..", StringComparison.Ordinal) ||
            pluginId.Contains('\\', StringComparison.Ordinal) ||
            pluginId.Contains('/', StringComparison.Ordinal) ||
            pluginId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Plugin id contains unsafe characters.");
        }
    }

    private static string GetSafeInstallDirectory(string storageRoot, string pluginId)
    {
        ValidateSafePluginId(pluginId);

        var fullStorageRoot = Path.GetFullPath(storageRoot);
        var installDirectory = Path.GetFullPath(Path.Combine(fullStorageRoot, pluginId));
        if (!installDirectory.StartsWith(EnsureTrailingSeparator(fullStorageRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Plugin id resolves to an unsafe install path.");
        }

        return installDirectory;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
