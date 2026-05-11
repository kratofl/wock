using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.BookingTargets;

public sealed class BookingTargetService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<BookingTarget>> ListActiveForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException($"Customer {customerId} was not found.");
        }

        return await dbContext.BookingTargets
            .Include(target => target.Customer)
            .Where(target => target.CustomerId == customerId && target.Customer.IsActive && target.IsActive)
            .OrderBy(target => target.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingTarget>> ListAllForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: false, cancellationToken);

        return await dbContext.BookingTargets
            .Where(target => target.CustomerId == customerId)
            .OrderBy(target => target.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingTarget> CreateAsync(
        int customerId,
        string? name,
        string? bookingSoftware,
        string? bookingTicketId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: true, cancellationToken);

        var target = new BookingTarget
        {
            CustomerId = customerId,
            Name = NormalizeRequired(name, "Task name is required."),
            BookingSoftware = NormalizeRequired(bookingSoftware, "Booking system is required."),
            BookingTicketId = NormalizeRequired(bookingTicketId, "Booking reference is required."),
            Notes = NormalizeOptional(notes),
            IsActive = true
        };

        dbContext.BookingTargets.Add(target);
        await dbContext.SaveChangesAsync(cancellationToken);
        return target;
    }

    public async Task<BookingTarget> UpdateAsync(
        int customerId,
        int bookingTargetId,
        string? name,
        string? bookingSoftware,
        string? bookingTicketId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (bookingTargetId <= 0)
        {
            throw new ArgumentException("Task ID must be greater than zero.", nameof(bookingTargetId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: true, cancellationToken);

        var target = await dbContext.BookingTargets
            .SingleOrDefaultAsync(
                target => target.Id == bookingTargetId && target.CustomerId == customerId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Task {bookingTargetId} was not found for customer {customerId}.");

        target.Name = NormalizeRequired(name, "Task name is required.");
        target.BookingSoftware = NormalizeRequired(bookingSoftware, "Booking system is required.");
        target.BookingTicketId = NormalizeRequired(bookingTicketId, "Booking reference is required.");
        target.Notes = NormalizeOptional(notes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return target;
    }

    public async Task DeactivateAsync(
        int customerId,
        int bookingTargetId,
        CancellationToken cancellationToken = default)
    {
        if (bookingTargetId <= 0)
        {
            throw new ArgumentException("Task ID must be greater than zero.", nameof(bookingTargetId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(dbContext, customerId, requireActive: true, cancellationToken);

        var target = await dbContext.BookingTargets
            .SingleOrDefaultAsync(
                target => target.Id == bookingTargetId && target.CustomerId == customerId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Task {bookingTargetId} was not found for customer {customerId}.");

        if (!target.IsActive)
        {
            throw new InvalidOperationException($"Task {bookingTargetId} is already inactive.");
        }

        target.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCustomerExistsAsync(
        AppDbContext dbContext,
        int customerId,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        }

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customerId} was not found.");

        if (requireActive && !customer.IsActive)
        {
            throw new InvalidOperationException($"Customer {customerId} is inactive.");
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
}
