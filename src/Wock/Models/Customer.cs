namespace Wock.Models;

public class Customer
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<BookingTarget> BookingTargets { get; set; } = [];

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
