using Wock.Common.Domain;

namespace Wock.Models;

public class Customer : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public required string Name { get; set; }

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? BillingAddress { get; set; }

    public decimal? DefaultHourlyRate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Project> Projects { get; set; } = [];

    public ICollection<BookingTarget> BookingTargets { get; set; } = [];

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
