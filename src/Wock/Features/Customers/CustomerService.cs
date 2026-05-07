using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.Customers;

public sealed class CustomerService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<Customer>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Customers
            .Where(customer => customer.IsActive)
            .OrderBy(customer => customer.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Customers
            .OrderBy(customer => customer.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer> CreateAsync(
        string? name,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = NormalizeRequired(name, "Customer name is required."),
            Notes = NormalizeOptional(notes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(
        int customerId,
        string? name,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customerId} was not found.");

        customer.Name = NormalizeRequired(name, "Customer name is required.");
        customer.Notes = NormalizeOptional(notes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task DeactivateAsync(int customerId, CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customerId} was not found.");

        if (!customer.IsActive)
        {
            throw new InvalidOperationException($"Customer {customerId} is already inactive.");
        }

        customer.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
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
