using Wock.Common.Domain;

namespace Wock.Models;

public class Customer : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public required string Name { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookingTarget> BookingTargets { get; set; } = [];

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
