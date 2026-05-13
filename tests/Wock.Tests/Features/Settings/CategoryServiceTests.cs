using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.Settings;
using Wock.Models;

namespace Wock.Tests.Features.Settings;

public sealed class CategoryServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task CreateAsync_persists_active_trimmed_category()
    {
        var service = CreateService();

        var category = await service.CreateAsync(" Discovery ", 15);

        Assert.True(category.Id > 0);
        Assert.Equal("Discovery", category.Name);
        Assert.Equal(15, category.SortOrder);
        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_sort_and_active_state()
    {
        var category = await AddCategoryAsync("Discovery", 10);
        var service = CreateService();

        var updated = await service.UpdateAsync(category.Id, " Consulting ", 20, false);

        Assert.Equal("Consulting", updated.Name);
        Assert.Equal(20, updated.SortOrder);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ListAllAsync_returns_inactive_categories_for_settings()
    {
        await AddCategoryAsync("Inactive", 20, isActive: false);
        await AddCategoryAsync("Active", 10);
        var service = CreateService();

        var categories = await service.ListAllAsync();

        var orderedCategories = categories.ToList();
        Assert.True(
            orderedCategories.FindIndex(category => category.Name == "Active") < orderedCategories.FindIndex(category => category.Name == "Inactive"));
        Assert.Contains(categories, category => !category.IsActive);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _factory = new TestDbContextFactory(_connection);
        await using var context = await _factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private CategoryService CreateService()
    {
        return new CategoryService(_factory);
    }

    private async Task<ActivityCategory> AddCategoryAsync(string name, int sortOrder, bool isActive = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var category = new ActivityCategory
        {
            Name = name,
            SortOrder = sortOrder,
            IsActive = isActive
        };
        context.ActivityCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new AppDbContext(options, AnonymousCurrentUserContext.Instance, new SystemClock());
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
