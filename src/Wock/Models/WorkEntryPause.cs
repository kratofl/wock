namespace Wock.Models;

public class WorkEntryPause
{
    public int Id { get; set; }

    public int WorkEntryId { get; set; }

    public WorkEntry WorkEntry { get; set; } = null!;

    public DateTime PausedAt { get; set; }

    public DateTime? ResumedAt { get; set; }
}
