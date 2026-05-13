using Wock.Common.Domain;

namespace Wock.Models;

public class Project : BaseAuditableEntity, IUserOwnedEntity
{
    public string? OwnerUserId { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    public decimal? BudgetHours { get; set; }

    public decimal? BudgetAmount { get; set; }

    public BillingModel BillingModel { get; set; } = BillingModel.Hourly;

    public decimal? DefaultHourlyRate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public ICollection<ProjectTask> Tasks { get; set; } = [];

    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}
