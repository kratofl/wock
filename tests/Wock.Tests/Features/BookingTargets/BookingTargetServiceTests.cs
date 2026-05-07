using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.BookingTargets;
using Wock.Models;

namespace Wock.Tests.Features.BookingTargets;

public sealed class BookingTargetServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task CreateAsync_persists_active_booking_target_for_active_customer()
    {
        var customer = await AddCustomerAsync("Example Customer");
        var service = CreateService();

        var target = await service.CreateAsync(
            customer.Id,
            "  Project Alpha  ",
            "  Jira  ",
            "  ALPHA-123  ",
            "  Notes  ");

        Assert.True(target.Id > 0);
        Assert.Equal(customer.Id, target.CustomerId);
        Assert.Equal("Project Alpha", target.Name);
        Assert.Equal("Jira", target.BookingSoftware);
        Assert.Equal("ALPHA-123", target.BookingTicketId);
        Assert.Equal("Notes", target.Notes);
        Assert.True(target.IsActive);
    }

    [Theory]
    [InlineData("", "Jira", "ALPHA-123", "Booking target name is required")]
    [InlineData("Project Alpha", "", "ALPHA-123", "Booking software is required")]
    [InlineData("Project Alpha", "Jira", "", "Booking ticket ID is required")]
    public async Task CreateAsync_rejects_required_booking_fields(
        string name,
        string bookingSoftware,
        string bookingTicketId,
        string expectedMessage)
    {
        var customer = await AddCustomerAsync("Example Customer");
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(customer.Id, name, bookingSoftware, bookingTicketId, null));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task CreateAsync_rejects_null_required_booking_fields_with_clear_argument_errors()
    {
        var customer = await AddCustomerAsync("Example Customer");
        var service = CreateService();

        var missingName = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(customer.Id, null!, "Jira", "ALPHA-123", null));
        var missingSoftware = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(customer.Id, "Project Alpha", null!, "ALPHA-123", null));
        var missingTicket = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(customer.Id, "Project Alpha", "Jira", null!, null));

        Assert.Contains("Booking target name is required", missingName.Message);
        Assert.Contains("Booking software is required", missingSoftware.Message);
        Assert.Contains("Booking ticket ID is required", missingTicket.Message);
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_or_inactive_customer()
    {
        var inactiveCustomer = await AddCustomerAsync("Inactive Customer", isActive: false);
        var service = CreateService();

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(999, "Project Alpha", "Jira", "ALPHA-123", null));
        Assert.Contains("Customer 999 was not found", missing.Message);

        var inactive = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(inactiveCustomer.Id, "Project Alpha", "Jira", "ALPHA-123", null));
        Assert.Contains($"Customer {inactiveCustomer.Id} is inactive", inactive.Message);
    }

    [Fact]
    public async Task UpdateAsync_updates_existing_booking_target_and_rejects_missing_target()
    {
        var customer = await AddCustomerAsync("Example Customer");
        var existing = await AddBookingTargetAsync(customer.Id, "Old Project", "Old Software", "OLD-1", "Old notes");
        var service = CreateService();

        var updated = await service.UpdateAsync(customer.Id, existing.Id, "  New Project  ", "  Linear  ", "  NEW-2  ", "  New notes  ");

        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal("New Project", updated.Name);
        Assert.Equal("Linear", updated.BookingSoftware);
        Assert.Equal("NEW-2", updated.BookingTicketId);
        Assert.Equal("New notes", updated.Notes);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(customer.Id, 999, "Missing", "Jira", "MISS-1", null));
        Assert.Contains("Booking target 999 was not found", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_rejects_booking_target_for_different_customer()
    {
        var selectedCustomer = await AddCustomerAsync("Selected Customer");
        var otherCustomer = await AddCustomerAsync("Other Customer");
        var otherTarget = await AddBookingTargetAsync(otherCustomer.Id, "Other Project", "Jira", "OTHER-1", null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(selectedCustomer.Id, otherTarget.Id, "Changed", "Jira", "OTHER-2", null));

        Assert.Contains($"Booking target {otherTarget.Id} was not found for customer {selectedCustomer.Id}", exception.Message);
    }

    [Fact]
    public async Task DeactivateAsync_marks_booking_target_inactive_without_deleting()
    {
        var customer = await AddCustomerAsync("Example Customer");
        var existing = await AddBookingTargetAsync(customer.Id, "Project Alpha", "Jira", "ALPHA-123", null);
        var service = CreateService();

        await service.DeactivateAsync(customer.Id, existing.Id);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.BookingTargets.SingleAsync(target => target.Id == existing.Id);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_rejects_booking_target_for_different_customer()
    {
        var selectedCustomer = await AddCustomerAsync("Selected Customer");
        var otherCustomer = await AddCustomerAsync("Other Customer");
        var otherTarget = await AddBookingTargetAsync(otherCustomer.Id, "Other Project", "Jira", "OTHER-1", null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeactivateAsync(selectedCustomer.Id, otherTarget.Id));

        Assert.Contains($"Booking target {otherTarget.Id} was not found for customer {selectedCustomer.Id}", exception.Message);
    }

    [Fact]
    public async Task ListActiveForCustomerAsync_excludes_inactive_targets_and_targets_for_inactive_customers()
    {
        var activeCustomer = await AddCustomerAsync("Active Customer");
        var inactiveCustomer = await AddCustomerAsync("Inactive Customer", isActive: false);
        await AddBookingTargetAsync(activeCustomer.Id, "Active Target", "Jira", "ACT-1", null);
        await AddBookingTargetAsync(activeCustomer.Id, "Inactive Target", "Jira", "INACT-1", null, isActive: false);
        await AddBookingTargetAsync(inactiveCustomer.Id, "Inactive Customer Target", "Jira", "CUST-1", null);
        var service = CreateService();

        var active = await service.ListActiveForCustomerAsync(activeCustomer.Id);
        var inactiveCustomerTargets = await service.ListActiveForCustomerAsync(inactiveCustomer.Id);
        var all = await service.ListAllForCustomerAsync(activeCustomer.Id);

        var activeTarget = Assert.Single(active);
        Assert.Equal("Active Target", activeTarget.Name);
        Assert.Empty(inactiveCustomerTargets);
        Assert.Equal(["Active Target", "Inactive Target"], all.Select(target => target.Name).ToArray());
    }

    [Fact]
    public async Task ListActiveForCustomerAsync_rejects_invalid_customer_id()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ListActiveForCustomerAsync(0));

        Assert.Contains("Customer ID must be greater than zero", exception.Message);
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

    private BookingTargetService CreateService()
    {
        return new BookingTargetService(_factory);
    }

    private async Task<Customer> AddCustomerAsync(string name, bool isActive = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer
        {
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private async Task<BookingTarget> AddBookingTargetAsync(
        int customerId,
        string name,
        string bookingSoftware,
        string bookingTicketId,
        string? notes,
        bool isActive = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var target = new BookingTarget
        {
            CustomerId = customerId,
            Name = name,
            BookingSoftware = bookingSoftware,
            BookingTicketId = bookingTicketId,
            Notes = notes,
            IsActive = isActive
        };
        context.BookingTargets.Add(target);
        await context.SaveChangesAsync();
        return target;
    }

    private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
