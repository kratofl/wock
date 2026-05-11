using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.Customers;
using Wock.Models;

namespace Wock.Tests.Features.Customers;

public sealed class CustomerServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task CreateAsync_persists_active_customer_with_trimmed_fields()
    {
        var service = CreateService();

        var customer = await service.CreateAsync("  Example Customer  ", "  Important notes  ");

        Assert.True(customer.Id > 0);
        Assert.Equal("Example Customer", customer.Name);
        Assert.Equal("Important notes", customer.Notes);
        Assert.True(customer.IsActive);
        Assert.Equal(DateTimeKind.Utc, customer.CreatedAt.Kind);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.Customers.SingleAsync();
        Assert.Equal("Example Customer", saved.Name);
        Assert.Equal("Important notes", saved.Notes);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task CreateAsync_rejects_blank_name()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync("   ", "Notes"));

        Assert.Contains("Customer name is required", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_rejects_null_name_with_clear_argument_error()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(null!, "Notes"));

        Assert.Contains("Customer name is required", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_updates_existing_customer_and_rejects_missing_customer()
    {
        var existing = await AddCustomerAsync("Old Name", "Old notes");
        var service = CreateService();

        var updated = await service.UpdateAsync(existing.Id, "  New Name  ", "  New notes  ");

        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New notes", updated.Notes);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(999, "Missing", null));
        Assert.Contains("Customer 999 was not found", exception.Message);
    }

    [Fact]
    public async Task DeactivateAsync_marks_customer_inactive_without_deleting()
    {
        var existing = await AddCustomerAsync("Example Customer", null);
        var service = CreateService();

        await service.DeactivateAsync(existing.Id);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.Customers.SingleAsync(customer => customer.Id == existing.Id);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task ListActiveAsync_excludes_inactive_customers_and_ListAllAsync_includes_them()
    {
        await AddCustomerAsync("Active Customer", null);
        await AddCustomerAsync("Inactive Customer", null, isActive: false);
        var service = CreateService();

        var active = await service.ListActiveAsync();
        var all = await service.ListAllAsync();

        var activeCustomer = Assert.Single(active);
        Assert.Equal("Active Customer", activeCustomer.Name);
        Assert.Equal(["Active Customer", "Inactive Customer"], all.Select(customer => customer.Name).ToArray());
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

    private CustomerService CreateService()
    {
        return new CustomerService(_factory);
    }

    private async Task<Customer> AddCustomerAsync(string name, string? notes, bool isActive = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer
        {
            Name = name,
            Notes = notes,
            IsActive = isActive
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
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
