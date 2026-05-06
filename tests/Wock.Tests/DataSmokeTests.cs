using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Models;

namespace Wock.Tests;

public class DataSmokeTests
{
    [Fact]
    public async Task Can_create_sqlite_schema_and_insert_customer_with_booking_target()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var customer = new Customer
        {
            Name = "Example Customer",
            CreatedAt = DateTime.UtcNow,
            BookingTargets =
            [
                new BookingTarget
                {
                    Name = "Project Alpha",
                    BookingSoftware = "Jira",
                    BookingTicketId = "ALPHA-123"
                }
            ]
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var savedCustomer = await context.Customers
            .Include(c => c.BookingTargets)
            .SingleAsync();

        Assert.Equal("Example Customer", savedCustomer.Name);
        var bookingTarget = Assert.Single(savedCustomer.BookingTargets);
        Assert.Equal(customer.Id, bookingTarget.CustomerId);
        Assert.Equal("ALPHA-123", bookingTarget.BookingTicketId);
    }

    [Fact]
    public void New_customer_defaults_to_active()
    {
        var customer = new Customer
        {
            Name = "Example Customer",
            CreatedAt = DateTime.UtcNow
        };

        Assert.True(customer.IsActive);
    }

    [Fact]
    public void New_booking_target_defaults_to_active()
    {
        var bookingTarget = new BookingTarget
        {
            Name = "Project Alpha",
            BookingSoftware = "Jira",
            BookingTicketId = "ALPHA-123"
        };

        Assert.True(bookingTarget.IsActive);
    }

    [Fact]
    public async Task Work_entry_rejects_negative_total_paused_seconds()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var customer = new Customer
        {
            Name = "Example Customer",
            CreatedAt = DateTime.UtcNow
        };

        context.WorkEntries.Add(new WorkEntry
        {
            Customer = customer,
            StartedAt = DateTime.UtcNow,
            TotalPausedSeconds = -1,
            Status = WorkEntryStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        return context;
    }
}
