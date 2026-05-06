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
            IsActive = true,
            BookingTargets =
            [
                new BookingTarget
                {
                    Name = "Project Alpha",
                    BookingSoftware = "Jira",
                    BookingTicketId = "ALPHA-123",
                    IsActive = true
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
