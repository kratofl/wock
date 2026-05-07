using System.IO.Compression;
using Microsoft.Extensions.Options;
using Wock.Abstractions;
using Wock.Features.Plugins;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginInstallServiceTests : IDisposable
{
    private readonly string _storagePath = Path.Combine(AppContext.BaseDirectory, "plugin-install-storage", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InstallFromFolderAsync_copies_plugin_to_configured_storage_and_validates_load()
    {
        using var source = TestPluginPackage.CreateValid<TestPlugin>();
        var service = CreateService();

        var package = await service.InstallFromFolderAsync(source.Path);

        Assert.Equal("test.booking", package.Manifest.Id);
        Assert.Equal(Path.Combine(_storagePath, "test.booking"), package.InstallDirectory);
        Assert.True(File.Exists(Path.Combine(package.InstallDirectory, PluginManifest.FileName)));
        Assert.True(File.Exists(package.AssemblyPath));
        Assert.True(package.Validation.Success);
    }

    [Fact]
    public async Task InstallFromZipAsync_extracts_plugin_to_configured_storage_and_validates_load()
    {
        using var source = TestPluginPackage.CreateValid<TestPlugin>();
        var zipPath = Path.Combine(AppContext.BaseDirectory, "plugin-install-zips", $"{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        ZipFile.CreateFromDirectory(source.Path, zipPath);
        var service = CreateService();

        var package = await service.InstallFromZipAsync(zipPath);

        Assert.Equal("test.booking", package.Manifest.Id);
        Assert.True(File.Exists(Path.Combine(package.InstallDirectory, PluginManifest.FileName)));
        Assert.True(File.Exists(package.AssemblyPath));
        Assert.True(package.Validation.Success);
    }

    [Fact]
    public async Task InstallFromZipAsync_rejects_entries_that_escape_extraction_directory()
    {
        var zipPath = Path.Combine(AppContext.BaseDirectory, "plugin-install-zips", $"{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("..\\evil.txt");
        }
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InstallFromZipAsync(zipPath));

        Assert.Contains("path traversal", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InstallFromFolderAsync_rejects_plugin_id_that_resolves_to_storage_root()
    {
        using var source = TestPluginPackage.CreateValid<TestPlugin>(pluginId: ".");
        Directory.CreateDirectory(_storagePath);
        var markerPath = Path.Combine(_storagePath, "existing-plugin.txt");
        await File.WriteAllTextAsync(markerPath, "do not delete");
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InstallFromFolderAsync(source.Path));

        Assert.Contains("unsafe", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(markerPath));
    }

    public void Dispose()
    {
        DeleteIfExists(_storagePath);
        DeleteIfExists(Path.Combine(AppContext.BaseDirectory, "plugin-install-zips"));
    }

    private PluginInstallService CreateService()
    {
        return new PluginInstallService(
            Options.Create(new PluginInstallOptions { StoragePath = _storagePath }),
            new PluginLoader());
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class TestPluginPackage : IDisposable
    {
        private TestPluginPackage(string path)
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public static TestPluginPackage CreateValid<TPlugin>(string pluginId = "test.booking")
        {
            var package = new TestPluginPackage(System.IO.Path.Combine(AppContext.BaseDirectory, "plugin-install-sources", Guid.NewGuid().ToString("N")));
            var assemblyName = System.IO.Path.GetFileName(typeof(TPlugin).Assembly.Location);
            File.Copy(typeof(TPlugin).Assembly.Location, System.IO.Path.Combine(package.Path, assemblyName), overwrite: true);
            File.WriteAllText(System.IO.Path.Combine(package.Path, PluginManifest.FileName), $$"""
                {
                  "id": "{{pluginId}}",
                  "name": "Test Booking",
                  "version": "1.0.0",
                  "assembly": "{{assemblyName}}",
                  "type": "{{typeof(TPlugin).FullName}}"
                }
                """);
            return package;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    public sealed class TestPlugin : IWockPlugin
    {
        public WockPluginMetadata Metadata { get; } = new("test.booking", "Test Booking", "1.0.0");

        public Task InitializeAsync(IWockPluginHost host, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
