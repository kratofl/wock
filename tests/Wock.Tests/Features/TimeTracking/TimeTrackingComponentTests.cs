using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wock.Data;
using Wock.Features.TimeTracking;
using Wock.Features.TimeTracking.Components;
using Wock.Models;

namespace Wock.Tests.Features.TimeTracking;

public sealed class TimeTrackingComponentTests : BunitContext, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestDbContextFactory _factory;

    public TimeTrackingComponentTests()
    {
        _connection.Open();
        _factory = new TestDbContextFactory(_connection);
        Services.AddSingleton<IDbContextFactory<AppDbContext>>(_factory);
        Services.AddSingleton<ISystemClock>(new FakeClock { UtcNow = new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc) });
        Services.AddScoped<TimeTrackingService>();
        using var context = _factory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public void ActiveTimerCard_displays_active_entry_customer_status_and_duration()
    {
        var entry = new WorkEntry
        {
            CustomerId = 1,
            Customer = new Customer { Id = 1, Name = "Acme", CreatedAt = DateTime.UtcNow },
            ExternalTicketId = "ABC-123",
            StartedAt = DateTime.UtcNow.AddMinutes(-42),
            Status = WorkEntryStatus.Running,
            CreatedAt = DateTime.UtcNow.AddMinutes(-42),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-42)
        };

        var cut = Render<ActiveTimerCard>(parameters => parameters
            .Add(component => component.Entry, entry)
            .Add(component => component.NetDuration, TimeSpan.FromMinutes(42)));

        Assert.Contains("Acme", cut.Markup);
        Assert.Contains("ABC-123", cut.Markup);
        Assert.Contains("Running", cut.Markup);
        Assert.Contains("00:42:00", cut.Markup);
    }

    [Fact]
    public void WorkEntryForm_renders_start_fields()
    {
        var customers = new[] { new Customer { Id = 1, Name = "Acme", CreatedAt = DateTime.UtcNow } };

        var cut = Render<WorkEntryForm>(parameters => parameters
            .Add(component => component.Customers, customers)
            .Add(component => component.BookingTargets, Array.Empty<BookingTarget>()));

        Assert.Contains("Customer", cut.Markup);
        Assert.Contains("External ticket", cut.Markup);
        Assert.Contains("Description", cut.Markup);
        Assert.Contains("Start tracking", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_renders_time_tracking_heading()
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Customers.Add(new Customer { Name = "Acme", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();

        Assert.Contains("Time Tracking", cut.Markup);
        Assert.Contains("Start tracking", cut.Markup);
    }

    public new void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTime UtcNow { get; set; }
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

