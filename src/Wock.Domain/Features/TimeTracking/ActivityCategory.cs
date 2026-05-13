using Wock.Common.Domain;

namespace Wock.Models;

public class ActivityCategory : BaseEntity
{
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<ProjectTask> ProjectTasks { get; set; } = [];

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
