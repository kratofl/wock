using System.Text.Json;
using Wock.Features.Plugins;

namespace Wock.Tests.Features.Plugins;

public sealed class PluginManifestTests
{
    [Fact]
    public async Task LoadFromFileAsync_reads_valid_manifest()
    {
        using var folder = TestFolder.Create();
        var manifestPath = folder.WriteJson("wock-plugin.json", new
        {
            id = "example.booking",
            name = "Example Booking",
            version = "1.2.3",
            assembly = "Example.Booking.dll",
            type = "Example.Booking.Plugin",
            description = "Exports bookings",
            author = "Example Ltd"
        });

        var manifest = await PluginManifest.LoadFromFileAsync(manifestPath);

        Assert.Equal("example.booking", manifest.Id);
        Assert.Equal("Example Booking", manifest.Name);
        Assert.Equal("1.2.3", manifest.Version);
        Assert.Equal("Example.Booking.dll", manifest.Assembly);
        Assert.Equal("Example.Booking.Plugin", manifest.Type);
        Assert.Equal("Exports bookings", manifest.Description);
        Assert.Equal("Example Ltd", manifest.Author);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("version")]
    [InlineData("assembly")]
    [InlineData("type")]
    public async Task LoadFromFileAsync_rejects_missing_required_fields(string missingField)
    {
        using var folder = TestFolder.Create();
        var values = new Dictionary<string, object?>
        {
            ["id"] = "example.booking",
            ["name"] = "Example Booking",
            ["version"] = "1.2.3",
            ["assembly"] = "Example.Booking.dll",
            ["type"] = "Example.Booking.Plugin"
        };
        values.Remove(missingField);
        var manifestPath = folder.WriteJson("wock-plugin.json", values);

        var exception = await Assert.ThrowsAsync<PluginManifestException>(() =>
            PluginManifest.LoadFromFileAsync(manifestPath));

        Assert.Contains(missingField, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("..\\Example.Booking.dll")]
    [InlineData("plugins\\..\\Example.Booking.dll")]
    [InlineData("C:\\plugins\\Example.Booking.dll")]
    public async Task LoadFromFileAsync_rejects_unsafe_assembly_paths(string assembly)
    {
        using var folder = TestFolder.Create();
        var manifestPath = folder.WriteJson("wock-plugin.json", new
        {
            id = "example.booking",
            name = "Example Booking",
            version = "1.2.3",
            assembly,
            type = "Example.Booking.Plugin"
        });

        var exception = await Assert.ThrowsAsync<PluginManifestException>(() =>
            PluginManifest.LoadFromFileAsync(manifestPath));

        Assert.Contains("assembly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestFolder : IDisposable
    {
        private readonly string _path;

        private TestFolder(string path)
        {
            _path = path;
            Directory.CreateDirectory(_path);
        }

        public static TestFolder Create()
        {
            return new TestFolder(Path.Combine(AppContext.BaseDirectory, "plugin-manifest-tests", Guid.NewGuid().ToString("N")));
        }

        public string WriteJson(string fileName, object value)
        {
            var path = Path.Combine(_path, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(value));
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }
}
