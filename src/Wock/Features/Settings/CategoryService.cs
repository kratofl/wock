using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Features.Settings;

public sealed class CategoryService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<ActivityCategory>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ActivityCategories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityCategory> CreateAsync(
        string? name,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var category = new ActivityCategory
        {
            Name = NormalizeRequired(name, "Category name is required."),
            SortOrder = sortOrder,
            IsActive = true
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ActivityCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<ActivityCategory> UpdateAsync(
        int categoryId,
        string? name,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await dbContext.ActivityCategories
            .SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Category {categoryId} was not found.");

        category.Name = NormalizeRequired(name, "Category name is required.");
        category.SortOrder = sortOrder;
        category.IsActive = isActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task SetActiveAsync(
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await dbContext.ActivityCategories
            .SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Category {categoryId} was not found.");

        category.IsActive = isActive;
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
}
