using Wock.Features.Plugins;
using Wock.Models;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginLoaderTests
{
    [Fact]
    public async Task ValidateAsync_returns_failure_when_assembly_is_missing()
    {
        var plugin = new InstalledPlugin
        {
            PluginId = "example.booking",
            Name = "Example Booking",
            Version = "1.2.3",
            AssemblyPath = Path.Combine(AppContext.BaseDirectory, "missing-plugin.dll"),
            TypeName = "Example.Booking.Plugin"
        };
        var loader = new PluginLoader();

        var result = await loader.ValidateAsync(plugin);

        Assert.False(result.Success);
        Assert.Contains("assembly", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_returns_failure_when_type_does_not_exist()
    {
        var plugin = new InstalledPlugin
        {
            PluginId = "example.booking",
            Name = "Example Booking",
            Version = "1.2.3",
            AssemblyPath = typeof(PluginLoader).Assembly.Location,
            TypeName = "Example.Booking.Plugin"
        };
        var loader = new PluginLoader();

        var result = await loader.ValidateAsync(plugin);

        Assert.False(result.Success);
        Assert.Contains("type", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
