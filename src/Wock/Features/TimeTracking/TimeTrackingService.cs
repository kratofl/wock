using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Common.Time;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.TimeTracking;

public sealed record StartWorkEntryRequest(
    int CustomerId,
    int? BookingTargetId = null,
    string? ExternalTicketId = null,
    string? Description = null);

public sealed class TimeTrackingService(IDbContextFactory<AppDbContext> dbContextFactory, ISystemClock clock)
{
    public async Task<WorkEntry?> GetActiveEntryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ActiveEntries(context)
            .Include(entry => entry.Customer)
            .Include(entry => entry.BookingTarget)
            .Include(entry => entry.Pauses)
            .OrderBy(entry => entry.StartedAt)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkEntry> StartAsync(
        int customerId,
        int? bookingTargetId = null,
        string? externalTicketId = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureNoActiveEntryAsync(context, cancellationToken);

        var customerExists = await context.Customers
            .AnyAsync(customer => customer.Id == customerId && customer.IsActive, cancellationToken);
        if (!customerExists)
        {
            throw new ArgumentException("An active customer is required to start time tracking.", nameof(customerId));
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

        var entry = CreateRunningEntry(customerId, bookingTargetId, externalTicketId, description, clock.UtcNow);

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
        DateTime startedAt)
    {
        return new WorkEntry
        {
            CustomerId = customerId,
            BookingTargetId = bookingTargetId,
            ExternalTicketId = Normalize(externalTicketId),
            Description = Normalize(description),
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
        if (exception.InnerException is not SqliteException sqliteException || sqliteException.SqliteErrorCode != 19)
        {
            return false;
        }

        return sqliteException.Message.Contains("IX_WorkEntries_OneActiveEntry", StringComparison.OrdinalIgnoreCase)
            || sqliteException.Message.Contains("WorkEntries.ActiveSlot", StringComparison.OrdinalIgnoreCase);
    }
}
