using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.Customers;

public sealed class ProjectService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<Project>> ListForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: false, cancellationToken);

        return await dbContext.Projects
            .Include(project => project.Tasks.OrderBy(task => task.Title))
                .ThenInclude(task => task.ActivityCategory)
            .Where(project => project.CustomerId == customerId && project.Status != ProjectStatus.Archived)
            .OrderBy(project => project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(
        int customerId,
        string? name,
        string? description,
        decimal? budgetHours,
        decimal? budgetAmount,
        BillingModel billingModel,
        decimal? defaultHourlyRate,
        ProjectStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: true, cancellationToken);

        var project = new Project
        {
            CustomerId = customerId,
            Name = NormalizeRequired(name, "Project name is required."),
            Description = NormalizeOptional(description),
            BudgetHours = NormalizeNonNegative(budgetHours, nameof(budgetHours)),
            BudgetAmount = NormalizeNonNegative(budgetAmount, nameof(budgetAmount)),
            BillingModel = billingModel,
            DefaultHourlyRate = NormalizeNonNegative(defaultHourlyRate, nameof(defaultHourlyRate)),
            Status = status is ProjectStatus.Archived ? ProjectStatus.Active : status
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<Project> UpdateProjectAsync(
        int projectId,
        string? name,
        string? description,
        decimal? budgetHours,
        decimal? budgetAmount,
        BillingModel billingModel,
        decimal? defaultHourlyRate,
        ProjectStatus status,
        CancellationToken cancellationToken = default)
    {
        if (projectId <= 0)
        {
            throw new ArgumentException("Project ID must be greater than zero.", nameof(projectId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var project = await dbContext.Projects
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} was not found.");

        project.Name = NormalizeRequired(name, "Project name is required.");
        project.Description = NormalizeOptional(description);
        project.BudgetHours = NormalizeNonNegative(budgetHours, nameof(budgetHours));
        project.BudgetAmount = NormalizeNonNegative(budgetAmount, nameof(budgetAmount));
        project.BillingModel = billingModel;
        project.DefaultHourlyRate = NormalizeNonNegative(defaultHourlyRate, nameof(defaultHourlyRate));
        project.Status = status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task ArchiveProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        if (projectId <= 0)
        {
            throw new ArgumentException("Project ID must be greater than zero.", nameof(projectId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var project = await dbContext.Projects
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} was not found.");

        project.Status = ProjectStatus.Archived;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectTask> CreateTaskAsync(
        int projectId,
        string? title,
        string? description,
        int? activityCategoryId,
        ProjectTaskStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectExistsAsync(dbContext, projectId, requireAvailable: true, cancellationToken);
        await EnsureActivityCategoryExistsAsync(dbContext, activityCategoryId, cancellationToken);

        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = NormalizeRequired(title, "Task title is required."),
            Description = NormalizeOptional(description),
            ActivityCategoryId = activityCategoryId,
            Status = status is ProjectTaskStatus.Archived ? ProjectTaskStatus.Open : status
        };

        dbContext.ProjectTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<ProjectTask> UpdateTaskAsync(
        int taskId,
        string? title,
        string? description,
        int? activityCategoryId,
        ProjectTaskStatus status,
        CancellationToken cancellationToken = default)
    {
        if (taskId <= 0)
        {
            throw new ArgumentException("Task ID must be greater than zero.", nameof(taskId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var task = await dbContext.ProjectTasks
            .SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task {taskId} was not found.");
        await EnsureActivityCategoryExistsAsync(dbContext, activityCategoryId, cancellationToken);

        task.Title = NormalizeRequired(title, "Task title is required.");
        task.Description = NormalizeOptional(description);
        task.ActivityCategoryId = activityCategoryId;
        task.Status = status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task ArchiveTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        if (taskId <= 0)
        {
            throw new ArgumentException("Task ID must be greater than zero.", nameof(taskId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var task = await dbContext.ProjectTasks
            .SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task {taskId} was not found.");

        task.Status = ProjectTaskStatus.Archived;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCustomerExistsAsync(
        AppDbContext dbContext,
        int customerId,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customerId} was not found.");

        if (requireActive && !customer.IsActive)
        {
            throw new InvalidOperationException($"Customer {customerId} is inactive.");
        }
    }

    private static async Task EnsureProjectExistsAsync(
        AppDbContext dbContext,
        int projectId,
        bool requireAvailable,
        CancellationToken cancellationToken)
    {
        if (projectId <= 0)
        {
            throw new ArgumentException("Project ID must be greater than zero.", nameof(projectId));
        }

        var project = await dbContext.Projects
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} was not found.");

        if (requireAvailable && project.Status is ProjectStatus.Archived)
        {
            throw new InvalidOperationException($"Project {projectId} is archived.");
        }
    }

    private static async Task EnsureActivityCategoryExistsAsync(
        AppDbContext dbContext,
        int? activityCategoryId,
        CancellationToken cancellationToken)
    {
        if (activityCategoryId is null)
        {
            return;
        }

        var exists = await dbContext.ActivityCategories
            .AnyAsync(category => category.Id == activityCategoryId.Value && category.IsActive, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"Activity category {activityCategoryId.Value} was not found.");
        }
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(errorMessage, nameof(value));
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static decimal? NormalizeNonNegative(decimal? value, string argumentName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(argumentName, "Value must not be negative.");
        }

        return value;
    }
}
