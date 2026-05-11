using Wock.Common.Domain;

namespace Wock.Models;

public class WorkEntry : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public int? BookingTargetId { get; set; }

    public BookingTarget? BookingTarget { get; set; }

    public string? ExternalTicketId { get; set; }

    public string? Description { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? StoppedAt { get; set; }

    public int TotalPausedSeconds { get; set; }

    public WorkEntryStatus Status { get; set; }

    public ICollection<WorkEntryPause> Pauses { get; set; } = [];
}
