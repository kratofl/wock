using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.Plugins;
using Wock.Models;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginRegistryServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task InstallAsync_persists_manifest_metadata_and_resolved_assembly_path()
    {
        using var folder = TestPluginFolder.Create();
        folder.WriteManifest("""
            {
              "id": "example.booking",
              "name": "Example Booking",
              "version": "1.2.3",
              "assembly": "Example.Booking.dll",
              "type": "Example.Booking.Plugin",
              "description": "Exports bookings",
              "author": "Example Ltd"
            }
            """);
        var service = CreateService();

        var installed = await service.InstallAsync(folder.Path);

        Assert.Equal("example.booking", installed.PluginId);
        Assert.Equal("Example Booking", installed.Name);
        Assert.Equal("1.2.3", installed.Version);
        Assert.Equal("Exports bookings", installed.Description);
        Assert.Equal(Path.GetFullPath(Path.Combine(folder.Path, "Example.Booking.dll")), installed.AssemblyPath);
        Assert.Equal("Example.Booking.Plugin", installed.TypeName);
        Assert.False(installed.IsEnabled);
        Assert.Equal(PluginLoadStatus.NotLoaded, installed.LastLoadStatus);
        Assert.Null(installed.LastLoadError);
        Assert.Equal(DateTimeKind.Utc, installed.InstalledAt.Kind);
        Assert.Equal(DateTimeKind.Utc, installed.UpdatedAt.Kind);
    }

    [Fact]
    public async Task SetEnabledAsync_enables_and_disables_installed_plugin()
    {
        var installed = await AddInstalledPluginAsync();
        var service = CreateService();

        await service.SetEnabledAsync(installed.Id, true);
        await service.SetEnabledAsync(installed.Id, false);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.InstalledPlugins.SingleAsync();
        Assert.False(saved.IsEnabled);
        Assert.Equal(PluginLoadStatus.Disabled, saved.LastLoadStatus);
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
    }

    private PluginRegistryService CreateService()
    {
        return new PluginRegistryService(_factory);
    }

    private async Task<InstalledPlugin> AddInstalledPluginAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var installed = new InstalledPlugin
        {
            PluginId = "example.booking",
            Name = "Example Booking",
            Version = "1.2.3",
            AssemblyPath = "Example.Booking.dll",
            TypeName = "Example.Booking.Plugin"
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

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
