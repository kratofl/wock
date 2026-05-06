namespace Wock.Models;

public class BookingTarget
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public required string Name { get; set; }

    public required string BookingSoftware { get; set; }

    public required string BookingTicketId { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
