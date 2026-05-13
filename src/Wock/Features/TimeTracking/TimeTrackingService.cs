using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Common.Security;
using Wock.Common.Time;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.TimeTracking;

public sealed record StartWorkEntryRequest(
    int CustomerId,
    int? BookingTargetId = null,
    string? ExternalTicketId = null,
    string? Description = null,
    int? ProjectId = null,
    int? ProjectTaskId = null,
    int? ActivityCategoryId = null,
    bool IsBillable = true);

public sealed record ManualWorkEntryRequest(
    int CustomerId,
    DateTime StartedAt,
    DateTime StoppedAt,
    int? BookingTargetId = null,
    string? ExternalTicketId = null,
    string? Description = null,
    int? ProjectId = null,
    int? ProjectTaskId = null,
    int? ActivityCategoryId = null,
    bool IsBillable = true);

public sealed class TimeTrackingService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISystemClock clock,
    ICurrentUserContext currentUserContext)
{
    public async Task<WorkEntry?> GetActiveEntryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ActiveEntries(context)
            .Include(entry => entry.Customer)
            .Include(entry => entry.BookingTarget)
            .Include(entry => entry.Project)
            .Include(entry => entry.ProjectTask)
            .Include(entry => entry.ActivityCategory)
            .Include(entry => entry.Pauses)
            .OrderBy(entry => entry.StartedAt)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkEntry> StartAsync(
        int customerId,
        int? bookingTargetId = null,
        string? externalTicketId = null,
        string? description = null,
        int? projectId = null,
        int? projectTaskId = null,
        int? activityCategoryId = null,
        bool isBillable = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureNoActiveEntryAsync(context, cancellationToken);

        projectId = await ValidateWorkAssignmentAsync(
            context,
            customerId,
            bookingTargetId,
            projectId,
            projectTaskId,
            activityCategoryId,
            cancellationToken);

        var entry = CreateRunningEntry(
            customerId,
            bookingTargetId,
            externalTicketId,
            description,
            clock.UtcNow,
            projectId,
            projectTaskId,
            activityCategoryId,
            isBillable);

        context.WorkEntries.Add(entry);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsSingleActiveEntryConstraintViolation(exception))
        {
            throw new InvalidOperationException("A work entry is already active.", exception);
        }

        return entry;
    }

    public async Task<WorkEntry> CreateManualAsync(
        ManualWorkEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.StoppedAt <= request.StartedAt)
        {
            throw new ArgumentException("The manual work entry must end after it starts.", nameof(request));
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var projectId = await ValidateWorkAssignmentAsync(
            context,
            request.CustomerId,
            request.BookingTargetId,
            request.ProjectId,
            request.ProjectTaskId,
            request.ActivityCategoryId,
            cancellationToken);

        var entry = new WorkEntry
        {
            CustomerId = request.CustomerId,
            BookingTargetId = request.BookingTargetId,
            ProjectId = projectId,
            ProjectTaskId = request.ProjectTaskId,
            ActivityCategoryId = request.ActivityCategoryId,
            ExternalTicketId = Normalize(request.ExternalTicketId),
            Description = Normalize(request.Description),
            IsBillable = request.IsBillable,
            ReviewStatus = TimeEntryReviewStatus.Draft,
            StartedAt = request.StartedAt,
            StoppedAt = request.StoppedAt,
            TotalPausedSeconds = 0,
            Status = WorkEntryStatus.Stopped
        };

        context.WorkEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task<WorkEntry> SubmitAsync(int workEntryId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await context.WorkEntries
            .Include(workEntry => workEntry.Pauses)
            .SingleOrDefaultAsync(workEntry => workEntry.Id == workEntryId, cancellationToken);
        if (entry is null)
        {
            throw new ArgumentException("The work entry does not exist.", nameof(workEntryId));
        }

        if (entry.ReviewStatus is not TimeEntryReviewStatus.Draft and not TimeEntryReviewStatus.Rejected)
        {
            throw new InvalidOperationException("Only draft or rejected work entries can be submitted.");
        }

        if (entry.Status != WorkEntryStatus.Stopped || entry.StoppedAt is null || GetNetDuration(entry) <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Only completed work entries with a positive duration can be submitted.");
        }

        if (entry.ProjectId is null)
        {
            throw new InvalidOperationException("A project is required before submitting a work entry.");
        }

        if (entry.ActivityCategoryId is null)
        {
            throw new InvalidOperationException("An activity category is required before submitting a work entry.");
        }

        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            throw new InvalidOperationException("A description is required before submitting a work entry.");
        }

        entry.ReviewStatus = TimeEntryReviewStatus.Submitted;
        entry.ApprovedAt = null;
        entry.ApprovedByUserId = null;
        entry.RejectionReason = null;
        await context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task<WorkEntry> ApproveAsync(int workEntryId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await GetReviewableEntryAsync(context, workEntryId, cancellationToken);

        entry.ReviewStatus = TimeEntryReviewStatus.Approved;
        entry.ApprovedAt = clock.UtcNow;
        entry.ApprovedByUserId = currentUserContext.UserId;
        entry.RejectionReason = null;

        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<WorkEntry> RejectAsync(
        int workEntryId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var normalizedReason = Normalize(rejectionReason);
        if (normalizedReason is null)
        {
            throw new ArgumentException("A rejection reason is required.", nameof(rejectionReason));
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await GetReviewableEntryAsync(context, workEntryId, cancellationToken);

        entry.ReviewStatus = TimeEntryReviewStatus.Rejected;
        entry.ApprovedAt = null;
        entry.ApprovedByUserId = null;
        entry.RejectionReason = normalizedReason;

        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<WorkEntry> SwitchToBookingTargetAsync(
        int bookingTargetId,
        string? externalTicketId = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var bookingTarget = await GetActiveBookingTargetAsync(context, bookingTargetId, cancellationToken);
        var activeEntry = await ActiveEntries(context)
            .Include(entry => entry.Pauses)
            .SingleOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;

        if (activeEntry is null)
        {
            var entry = CreateRunningEntry(
                bookingTarget.CustomerId,
                bookingTarget.Id,
                externalTicketId,
                description,
                now);
            context.WorkEntries.Add(entry);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsSingleActiveEntryConstraintViolation(exception))
            {
                throw new InvalidOperationException("A work entry is already active.", exception);
            }

            return entry;
        }

        if (activeEntry.BookingTargetId == bookingTarget.Id)
        {
            if (activeEntry.Status == WorkEntryStatus.Paused)
            {
                ResumeEntry(activeEntry, now);
                await context.SaveChangesAsync(cancellationToken);
            }

            return activeEntry;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            StopEntry(activeEntry, now);
            await context.SaveChangesAsync(cancellationToken);

            var entry = CreateRunningEntry(
                bookingTarget.CustomerId,
                bookingTarget.Id,
                externalTicketId,
                description,
                now);
            context.WorkEntries.Add(entry);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return entry;
        }
        catch (DbUpdateException exception) when (IsSingleActiveEntryConstraintViolation(exception))
        {
            throw new InvalidOperationException("A work entry is already active.", exception);
        }
    }

    public async Task<WorkEntry> PauseAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await GetActiveEntryForUpdateAsync(context, cancellationToken);
        if (entry.Status != WorkEntryStatus.Running)
        {
            throw new InvalidOperationException("Only a running work entry can be paused.");
        }

        if (entry.Pauses.Any(pause => pause.ResumedAt is null))
        {
            throw new InvalidOperationException("The running work entry already has an open pause.");
        }

        var now = clock.UtcNow;
        entry.Status = WorkEntryStatus.Paused;
        entry.Pauses.Add(new WorkEntryPause { PausedAt = now });

        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<WorkEntry> ResumeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await GetActiveEntryForUpdateAsync(context, cancellationToken);

        ResumeEntry(entry, clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<WorkEntry> StopAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await GetActiveEntryForUpdateAsync(context, cancellationToken);
        var now = clock.UtcNow;

        StopEntry(entry, now);

        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public TimeSpan GetNetDuration(WorkEntry entry, DateTime? now = null)
    {
        var end = entry.StoppedAt ?? now ?? clock.UtcNow;
        var grossSeconds = Math.Max(0, (end - entry.StartedAt).TotalSeconds);
        var openPauseSeconds = entry.Pauses
            .Where(pause => pause.ResumedAt is null)
            .Sum(pause => ElapsedSeconds(pause.PausedAt, end));
        var netSeconds = grossSeconds - entry.TotalPausedSeconds - openPauseSeconds;

        return TimeSpan.FromSeconds(Math.Max(0, netSeconds));
    }

    private static WorkEntry CreateRunningEntry(
        int customerId,
        int? bookingTargetId,
        string? externalTicketId,
        string? description,
        DateTime startedAt,
        int? projectId = null,
        int? projectTaskId = null,
        int? activityCategoryId = null,
        bool isBillable = true)
    {
        return new WorkEntry
        {
            CustomerId = customerId,
            BookingTargetId = bookingTargetId,
            ProjectId = projectId,
            ProjectTaskId = projectTaskId,
            ActivityCategoryId = activityCategoryId,
            ExternalTicketId = Normalize(externalTicketId),
            Description = Normalize(description),
            IsBillable = isBillable,
            ReviewStatus = TimeEntryReviewStatus.Draft,
            StartedAt = startedAt,
            TotalPausedSeconds = 0,
            Status = WorkEntryStatus.Running
        };
    }

    private static IQueryable<WorkEntry> ActiveEntries(AppDbContext context)
    {
        return context.WorkEntries.Where(entry => entry.Status == WorkEntryStatus.Running || entry.Status == WorkEntryStatus.Paused);
    }

    private static async Task<BookingTarget> GetActiveBookingTargetAsync(
        AppDbContext context,
        int bookingTargetId,
        CancellationToken cancellationToken)
    {
        if (bookingTargetId <= 0)
        {
            throw new ArgumentException("Task ID must be greater than zero.", nameof(bookingTargetId));
        }

        var bookingTarget = await context.BookingTargets
            .Include(target => target.Customer)
            .SingleOrDefaultAsync(target => target.Id == bookingTargetId, cancellationToken);
        if (bookingTarget is null || !bookingTarget.IsActive)
        {
            throw new ArgumentException("The task must exist and be active.", nameof(bookingTargetId));
        }

        if (!bookingTarget.Customer.IsActive)
        {
            throw new ArgumentException("An active customer is required to start time tracking.", nameof(bookingTargetId));
        }

        return bookingTarget;
    }

    private static async Task<int?> ValidateWorkAssignmentAsync(
        AppDbContext context,
        int customerId,
        int? bookingTargetId,
        int? projectId,
        int? projectTaskId,
        int? activityCategoryId,
        CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers
            .AnyAsync(customer => customer.Id == customerId && customer.IsActive, cancellationToken);
        if (!customerExists)
        {
            throw new ArgumentException("An active customer is required for time tracking.", nameof(customerId));
        }

        if (bookingTargetId.HasValue)
        {
            var bookingTarget = await context.BookingTargets
                .SingleOrDefaultAsync(target => target.Id == bookingTargetId.Value, cancellationToken);
            if (bookingTarget is null || !bookingTarget.IsActive)
            {
                throw new ArgumentException("The task must exist and be active.", nameof(bookingTargetId));
            }

            if (bookingTarget.CustomerId != customerId)
            {
                throw new ArgumentException("The task must belong to the selected customer.", nameof(bookingTargetId));
            }
        }

        if (projectId.HasValue)
        {
            var project = await context.Projects
                .SingleOrDefaultAsync(project => project.Id == projectId.Value, cancellationToken);
            if (project is null || project.Status is ProjectStatus.Archived)
            {
                throw new ArgumentException("The project must exist and be available.", nameof(projectId));
            }

            if (project.CustomerId != customerId)
            {
                throw new ArgumentException("The project must belong to the selected customer.", nameof(projectId));
            }
        }

        if (projectTaskId.HasValue)
        {
            var projectTask = await context.ProjectTasks
                .Include(task => task.Project)
                .SingleOrDefaultAsync(task => task.Id == projectTaskId.Value, cancellationToken);
            if (projectTask is null || projectTask.Status is ProjectTaskStatus.Archived)
            {
                throw new ArgumentException("The project task must exist and be available.", nameof(projectTaskId));
            }

            if (projectTask.Project.CustomerId != customerId)
            {
                throw new ArgumentException("The project task must belong to the selected customer.", nameof(projectTaskId));
            }

            if (projectId.HasValue && projectTask.ProjectId != projectId.Value)
            {
                throw new ArgumentException("The project task must belong to the selected project.", nameof(projectTaskId));
            }

            projectId ??= projectTask.ProjectId;
        }

        if (activityCategoryId.HasValue)
        {
            var categoryExists = await context.ActivityCategories
                .AnyAsync(category => category.Id == activityCategoryId.Value && category.IsActive, cancellationToken);
            if (!categoryExists)
            {
                throw new ArgumentException("The activity category must exist and be active.", nameof(activityCategoryId));
            }
        }

        return projectId;
    }

    private static async Task EnsureNoActiveEntryAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await ActiveEntries(context).AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("A work entry is already active.");
        }
    }

    private static async Task<WorkEntry> GetActiveEntryForUpdateAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var entry = await ActiveEntries(context)
            .Include(workEntry => workEntry.Pauses)
            .SingleOrDefaultAsync(cancellationToken);

        return entry ?? throw new InvalidOperationException("No active work entry was found.");
    }

    private static async Task<WorkEntry> GetReviewableEntryAsync(
        AppDbContext context,
        int workEntryId,
        CancellationToken cancellationToken)
    {
        var entry = await context.WorkEntries
            .SingleOrDefaultAsync(workEntry => workEntry.Id == workEntryId, cancellationToken);
        if (entry is null)
        {
            throw new ArgumentException("The work entry does not exist.", nameof(workEntryId));
        }

        if (entry.ReviewStatus is not TimeEntryReviewStatus.Submitted and not TimeEntryReviewStatus.InReview)
        {
            throw new InvalidOperationException("Only submitted or in-review work entries can be reviewed.");
        }

        return entry;
    }

    private static WorkEntryPause GetSingleOpenPause(WorkEntry entry)
    {
        var openPauses = entry.Pauses.Where(pause => pause.ResumedAt is null).ToList();
        if (openPauses.Count != 1)
        {
            throw new InvalidOperationException("The paused work entry must have exactly one open pause.");
        }

        return openPauses[0];
    }

    private static void ResumeEntry(WorkEntry entry, DateTime now)
    {
        if (entry.Status != WorkEntryStatus.Paused)
        {
            throw new InvalidOperationException("Only a paused work entry can be resumed.");
        }

        var pause = GetSingleOpenPause(entry);
        pause.ResumedAt = now;
        entry.TotalPausedSeconds += ElapsedSeconds(pause.PausedAt, now);
        entry.Status = WorkEntryStatus.Running;
    }

    private static void StopEntry(WorkEntry entry, DateTime now)
    {
        if (entry.Status == WorkEntryStatus.Paused)
        {
            var pause = GetSingleOpenPause(entry);
            pause.ResumedAt = now;
            entry.TotalPausedSeconds += ElapsedSeconds(pause.PausedAt, now);
        }
        else if (entry.Status != WorkEntryStatus.Running)
        {
            throw new InvalidOperationException("Only a running or paused work entry can be stopped.");
        }

        entry.Status = WorkEntryStatus.Stopped;
        entry.StoppedAt = now;
    }

    private static int ElapsedSeconds(DateTime start, DateTime end)
    {
        return (int)Math.Max(0, Math.Floor((end - start).TotalSeconds));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsSingleActiveEntryConstraintViolation(DbUpdateException exception)
    {
        var sqliteException = FindSqliteException(exception);
        if (sqliteException is null || sqliteException.SqliteErrorCode != 19)
        {
            return false;
        }

        return sqliteException.Message.Contains("IX_WorkEntries_OneActiveEntry", StringComparison.OrdinalIgnoreCase)
            || sqliteException.Message.Contains("WorkEntries.ActiveOwnerSlot", StringComparison.OrdinalIgnoreCase)
            || sqliteException.Message.Contains("WorkEntries.ActiveSlot", StringComparison.OrdinalIgnoreCase);
    }

    private static SqliteException? FindSqliteException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqliteException)
            {
                return sqliteException;
            }
        }

        return null;
    }
}
