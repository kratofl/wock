using Wock.Common.Domain;

namespace Wock.Models;

public class WorkEntryPause : BaseEntity
{
    public int WorkEntryId { get; set; }

    public WorkEntry WorkEntry { get; set; } = null!;

    public DateTime PausedAt { get; set; }

    public DateTime? ResumedAt { get; set; }
}
