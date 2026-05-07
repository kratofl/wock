using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wock.Abstractions;
using Wock.Data;
using Wock.Features.Plugins;
using Wock.Models;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginRegistryServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _pluginStoragePath = Path.Combine(AppContext.BaseDirectory, "plugin-registry-storage", Guid.NewGuid().ToString("N"));
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task InstallFromFolderAsync_copies_plugin_validates_load_and_persists_manifest_metadata()
    {
        using var folder = TestPluginFolder.Create();
        folder.WriteValidPluginManifest<TestBookingConnectorPlugin>();
        var service = CreateService();

        var installed = await service.InstallFromFolderAsync(folder.Path);

        Assert.Equal("example.booking", installed.PluginId);
        Assert.Equal("Example Booking", installed.Name);
        Assert.Equal("1.2.3", installed.Version);
        Assert.Equal("Exports bookings", installed.Description);
        Assert.StartsWith(Path.GetFullPath(_pluginStoragePath), installed.AssemblyPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(installed.AssemblyPath));
        Assert.Equal(typeof(TestBookingConnectorPlugin).FullName, installed.TypeName);
        Assert.False(installed.IsEnabled);
        Assert.Equal(PluginLoadStatus.NotLoaded, installed.LastLoadStatus);
        Assert.Null(installed.LastLoadError);
        Assert.Equal(DateTimeKind.Utc, installed.InstalledAt.Kind);
        Assert.Equal(DateTimeKind.Utc, installed.UpdatedAt.Kind);
    }

    [Fact]
    public async Task InstallFromFolderAsync_persists_failed_status_when_load_validation_fails()
    {
        using var folder = TestPluginFolder.Create();
        folder.WriteManifestWithType("Example.Booking.DoesNotExist");
        var service = CreateService();

        var installed = await service.InstallFromFolderAsync(folder.Path);

        Assert.False(installed.IsEnabled);
        Assert.Equal(PluginLoadStatus.Failed, installed.LastLoadStatus);
        Assert.Contains("type", installed.LastLoadError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetEnabledAsync_validates_plugin_before_enabling()
    {
        var installed = await AddInstalledPluginAsync(
            assemblyPath: typeof(TestBookingConnectorPlugin).Assembly.Location,
            typeName: typeof(TestBookingConnectorPlugin).FullName!);
        var service = CreateService();

        await service.SetEnabledAsync(installed.Id, true);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.InstalledPlugins.SingleAsync();
        Assert.True(saved.IsEnabled);
        Assert.Equal(PluginLoadStatus.Loaded, saved.LastLoadStatus);
        Assert.Null(saved.LastLoadError);
    }

    [Fact]
    public async Task SetEnabledAsync_keeps_plugin_disabled_and_persists_error_when_validation_fails()
    {
        var installed = await AddInstalledPluginAsync(
            assemblyPath: Path.Combine(AppContext.BaseDirectory, "missing-plugin.dll"),
            typeName: "Example.Booking.Plugin");
        var service = CreateService();

        await service.SetEnabledAsync(installed.Id, true);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.InstalledPlugins.SingleAsync();
        Assert.False(saved.IsEnabled);
        Assert.Equal(PluginLoadStatus.Failed, saved.LastLoadStatus);
        Assert.Contains("assembly", saved.LastLoadError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetEnabledAsync_disables_installed_plugin_without_deleting_files()
    {
        var installed = await AddInstalledPluginAsync(
            isEnabled: true,
            loadStatus: PluginLoadStatus.Loaded,
            assemblyPath: typeof(TestBookingConnectorPlugin).Assembly.Location,
            typeName: typeof(TestBookingConnectorPlugin).FullName!);
        var service = CreateService();

        await service.SetEnabledAsync(installed.Id, false);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.InstalledPlugins.SingleAsync();
        Assert.False(saved.IsEnabled);
        Assert.Equal(PluginLoadStatus.Disabled, saved.LastLoadStatus);
    }

    [Fact]
    public async Task GetEnabledBookingConnectorsAsync_returns_enabled_connector_metadata()
    {
        await AddInstalledPluginAsync(
            isEnabled: true,
            loadStatus: PluginLoadStatus.Loaded,
            assemblyPath: typeof(TestBookingConnectorPlugin).Assembly.Location,
            typeName: typeof(TestBookingConnectorPlugin).FullName!);
        await AddInstalledPluginAsync(
            pluginId: "disabled.booking",
            isEnabled: false,
            loadStatus: PluginLoadStatus.Disabled,
            assemblyPath: typeof(TestBookingConnectorPlugin).Assembly.Location,
            typeName: typeof(TestBookingConnectorPlugin).FullName!);
        var service = CreateService();

        var connectors = await service.GetEnabledBookingConnectorsAsync();

        var connector = Assert.Single(connectors);
        Assert.Equal("example.booking", connector.Metadata.Id);
        Assert.Equal("Example Booking", connector.Name);
    }

    [Fact]
    public async Task UpdateLoadStatusAsync_persists_status_and_error()
    {
        var installed = await AddInstalledPluginAsync();
        var service = CreateService();

        await service.UpdateLoadStatusAsync(installed.Id, PluginLoadStatus.Failed, "Could not load assembly.");

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.InstalledPlugins.SingleAsync();
        Assert.Equal(PluginLoadStatus.Failed, saved.LastLoadStatus);
        Assert.Equal("Could not load assembly.", saved.LastLoadError);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _factory = new TestDbContextFactory(_connection);
        await using var context = await _factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (Directory.Exists(_pluginStoragePath))
        {
            Directory.Delete(_pluginStoragePath, recursive: true);
        }
    }

    private PluginRegistryService CreateService()
    {
        var loader = new PluginLoader();
        var installService = new PluginInstallService(
            Options.Create(new PluginInstallOptions { StoragePath = _pluginStoragePath }),
            loader);
        return new PluginRegistryService(_factory, installService, loader);
    }

    private async Task<InstalledPlugin> AddInstalledPluginAsync(
        string pluginId = "example.booking",
        bool isEnabled = false,
        string loadStatus = PluginLoadStatus.NotLoaded,
        string assemblyPath = "Example.Booking.dll",
        string typeName = "Example.Booking.Plugin")
    {
        await using var context = await _factory.CreateDbContextAsync();
        var installed = new InstalledPlugin
        {
            PluginId = pluginId,
            Name = "Example Booking",
            Version = "1.2.3",
            AssemblyPath = assemblyPath,
            TypeName = typeName,
            IsEnabled = isEnabled,
            LastLoadStatus = loadStatus
        };
        context.InstalledPlugins.Add(installed);
        await context.SaveChangesAsync();
        return installed;
    }

    private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class TestPluginFolder : IDisposable
    {
        private TestPluginFolder(string path)
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public static TestPluginFolder Create()
        {
            return new TestPluginFolder(System.IO.Path.Combine(AppContext.BaseDirectory, "plugin-registry-tests", Guid.NewGuid().ToString("N")));
        }

        public void WriteManifest(string content)
        {
            File.WriteAllText(System.IO.Path.Combine(Path, PluginManifest.FileName), content);
        }

        public void WriteValidPluginManifest<TPlugin>()
        {
            WriteManifestWithType(typeof(TPlugin).FullName!);
        }

        public void WriteManifestWithType(string typeName)
        {
            var assemblyName = System.IO.Path.GetFileName(typeof(TestBookingConnectorPlugin).Assembly.Location);
            File.Copy(
                typeof(TestBookingConnectorPlugin).Assembly.Location,
                System.IO.Path.Combine(Path, assemblyName),
                overwrite: true);
            WriteManifest($$"""
                {
                  "id": "example.booking",
                  "name": "Example Booking",
                  "version": "1.2.3",
                  "assembly": "{{assemblyName}}",
                  "type": "{{typeName}}",
                  "description": "Exports bookings",
                  "author": "Example Ltd"
                }
                """);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    public sealed class TestBookingConnectorPlugin : IWockPlugin, IBookingConnector
    {
        public WockPluginMetadata Metadata { get; } = new("example.booking", "Example Booking", "1.2.3");

        public string Name => "Example Booking";

        public Task InitializeAsync(IWockPluginHost host, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<BookingExportResult> ValidateAsync(BookingExportRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BookingExportResult(true));
        }

        public Task<BookingExportResult> ExportAsync(BookingExportRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BookingExportResult(true, "TEST-1"));
        }
    }
}
