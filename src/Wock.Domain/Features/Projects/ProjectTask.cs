using Wock.Common.Domain;
using Wock.Features.Users.Models;

namespace Wock.Models;

public class ProjectTask : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public required string Title { get; set; }

    public string? Description { get; set; }

    public int? ActivityCategoryId { get; set; }

    public ActivityCategory? ActivityCategory { get; set; }

    public string? AssignedUserId { get; set; }

    public ApplicationUser? AssignedUser { get; set; }

    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Open;

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
