using Wock.Common.Domain;

namespace Wock.Models;

public class WorkEntry : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public int? BookingTargetId { get; set; }

    public BookingTarget? BookingTarget { get; set; }

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int? ProjectTaskId { get; set; }

    public ProjectTask? ProjectTask { get; set; }

    public int? ActivityCategoryId { get; set; }

    public ActivityCategory? ActivityCategory { get; set; }

    public string? ExternalTicketId { get; set; }

    public string? Description { get; set; }

    public bool IsBillable { get; set; } = true;

    public string? BillingCategory { get; set; }

    public decimal? HourlyRate { get; set; }

    public TimeEntryReviewStatus ReviewStatus { get; set; } = TimeEntryReviewStatus.Draft;

    public string? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? StoppedAt { get; set; }

    public int TotalPausedSeconds { get; set; }

    public WorkEntryStatus Status { get; set; }

    public ICollection<WorkEntryPause> Pauses { get; set; } = [];
}
