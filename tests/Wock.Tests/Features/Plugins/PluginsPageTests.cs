using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wock.Data;
using Wock.Features.Plugins;
using Wock.Models;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginsPageTests : BunitContext, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestDbContextFactory _factory;
    private readonly string _pluginStoragePath = Path.Combine(AppContext.BaseDirectory, "plugins-page-storage", Guid.NewGuid().ToString("N"));

    public PluginsPageTests()
    {
        _connection.Open();
        _factory = new TestDbContextFactory(_connection);
        Services.AddSingleton<IDbContextFactory<AppDbContext>>(_factory);
        Services.AddSingleton(new PluginLoader());
        Services.AddSingleton(Options.Create(new PluginInstallOptions { StoragePath = _pluginStoragePath }));
        Services.AddScoped<PluginInstallService>();
        Services.AddScoped<PluginRegistryService>();
        using var context = _factory.CreateDbContext();
        context.Database.EnsureCreated();
        context.InstalledPlugins.Add(new InstalledPlugin
        {
            PluginId = "broken.booking",
            Name = "Broken Booking",
            Version = "1.0.0",
            AssemblyPath = "missing.dll",
            TypeName = "Broken.Plugin",
            LastLoadStatus = PluginLoadStatus.Failed,
            LastLoadError = "Plugin assembly was not found."
        });
        context.SaveChanges();
    }

    [Fact]
    public void PluginsPage_renders_folder_zip_install_inputs_and_load_status_errors()
    {
        var cut = Render<PluginsPage>();

        Assert.Contains("Plugin folder", cut.Markup);
        Assert.Contains("Plugin ZIP", cut.Markup);
        Assert.Contains("Broken Booking", cut.Markup);
        Assert.Contains(PluginLoadStatus.Failed, cut.Markup);
        Assert.Contains("Plugin assembly was not found.", cut.Markup);
    }

    [Fact]
    public async Task PluginsPage_shows_failure_message_when_enable_validation_fails()
    {
        var cut = Render<PluginsPage>();
        cut.WaitForState(() => cut.Markup.Contains("Broken Booking", StringComparison.Ordinal));
        await using var context = await _factory.CreateDbContextAsync();
        var pluginId = await context.InstalledPlugins
            .Where(plugin => plugin.PluginId == "broken.booking")
            .Select(plugin => plugin.Id)
            .SingleAsync();
        var setEnabled = typeof(PluginsPage).GetMethod(
            "SetEnabledAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        await cut.InvokeAsync(async () =>
            await (Task)setEnabled.Invoke(cut.Instance, [pluginId, true])!);
        cut.Render();

        Assert.DoesNotContain("Plugin enabled.", cut.Markup);
        Assert.Contains("Plugin could not be enabled.", cut.Markup);
        Assert.Contains(PluginLoadStatus.Failed, cut.Markup);
        Assert.Contains("Plugin assembly was not found", cut.Markup);
    }

    public new void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_pluginStoragePath))
        {
            Directory.Delete(_pluginStoragePath, recursive: true);
        }
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
}
