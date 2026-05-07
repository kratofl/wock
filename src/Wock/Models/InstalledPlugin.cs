using System.ComponentModel.DataAnnotations;

namespace Wock.Models;

public class InstalledPlugin
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string PluginId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Version { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    public string AssemblyPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string TypeName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [Required]
    [MaxLength(20)]
    public string LastLoadStatus { get; set; } = PluginLoadStatus.NotLoaded;

    [MaxLength(4000)]
    public string? LastLoadError { get; set; }

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
