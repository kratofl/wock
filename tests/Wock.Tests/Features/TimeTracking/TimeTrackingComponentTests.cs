using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wock.Common.Time;
using Wock.Data;
using Wock.Features.TimeTracking;
using Wock.Features.TimeTracking.Components;
using Wock.Models;

namespace Wock.Tests.Features.TimeTracking;

public sealed class TimeTrackingComponentTests : BunitContext, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestDbContextFactory _factory;
    private readonly FakeClock _clock = new() { UtcNow = new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc) };

    public TimeTrackingComponentTests()
    {
        _connection.Open();
        _factory = new TestDbContextFactory(_connection);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(config => config.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddSingleton<IDbContextFactory<AppDbContext>>(_factory);
        Services.AddSingleton<ISystemClock>(_clock);
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
            CreatedAt = DateTime.UtcNow.AddMinutes(-42)
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
        Assert.Contains("Start custom tracking", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_renders_quick_switch_empty_state()
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Customers.Add(new Customer { Name = "Acme" });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();

        Assert.Contains("Tasks", cut.Markup);
        Assert.Contains("Time Tracking", cut.Markup);
        Assert.Contains("Week trend", cut.Markup);
        Assert.Contains("Minutes per day", cut.Markup);
        Assert.Contains("Monday - Sunday", cut.Markup);
        Assert.Contains("No tasks ready yet", cut.Markup);
        Assert.Contains("Create tasks", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_renders_quick_switch_tasks()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer { Name = "Acme" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        context.BookingTargets.Add(new BookingTarget
        {
            CustomerId = customer.Id,
            Name = "Feature work",
            BookingSoftware = "Jira",
            BookingTicketId = "ABC-123"
        });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();

        Assert.Contains("Feature work", cut.Markup);
        Assert.Contains("Acme - Jira ABC-123", cut.Markup);
        Assert.Contains("Start", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_orders_quick_switch_tasks_by_recent_usage()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer { Name = "Acme" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var olderTarget = new BookingTarget
        {
            CustomerId = customer.Id,
            Name = "Older task",
            BookingSoftware = "Jira",
            BookingTicketId = "OLD-1"
        };
        var recentTarget = new BookingTarget
        {
            CustomerId = customer.Id,
            Name = "Recent task",
            BookingSoftware = "Jira",
            BookingTicketId = "REC-1"
        };
        context.BookingTargets.AddRange(olderTarget, recentTarget);
        await context.SaveChangesAsync();
        context.WorkEntries.AddRange(
            new WorkEntry
            {
                CustomerId = customer.Id,
                BookingTargetId = olderTarget.Id,
                StartedAt = new DateTime(2026, 5, 7, 7, 0, 0, DateTimeKind.Utc),
                StoppedAt = new DateTime(2026, 5, 7, 7, 15, 0, DateTimeKind.Utc),
                Status = WorkEntryStatus.Stopped
            },
            new WorkEntry
            {
                CustomerId = customer.Id,
                BookingTargetId = recentTarget.Id,
                StartedAt = new DateTime(2026, 5, 7, 7, 30, 0, DateTimeKind.Utc),
                StoppedAt = new DateTime(2026, 5, 7, 7, 45, 0, DateTimeKind.Utc),
                Status = WorkEntryStatus.Stopped
            });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();

        Assert.True(
            cut.Markup.IndexOf("Recent task", StringComparison.Ordinal) < cut.Markup.IndexOf("Older task", StringComparison.Ordinal),
            "Recently used tasks should appear before older tasks.");
    }

    [Fact]
    public async Task Dashboard_week_trend_starts_on_monday()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer { Name = "Acme" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        context.WorkEntries.AddRange(
            new WorkEntry
            {
                CustomerId = customer.Id,
                StartedAt = new DateTime(2026, 5, 3, 10, 0, 0, DateTimeKind.Utc),
                StoppedAt = new DateTime(2026, 5, 3, 10, 30, 0, DateTimeKind.Utc),
                Status = WorkEntryStatus.Stopped
            },
            new WorkEntry
            {
                CustomerId = customer.Id,
                StartedAt = new DateTime(2026, 5, 4, 10, 0, 0, DateTimeKind.Utc),
                StoppedAt = new DateTime(2026, 5, 4, 10, 20, 0, DateTimeKind.Utc),
                Status = WorkEntryStatus.Stopped
            });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();

        Assert.Contains("00:20:00 this week", cut.Markup);
        Assert.DoesNotContain("00:50:00 this week", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_active_timer_updates_live()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer { Name = "Acme" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        context.WorkEntries.Add(new WorkEntry
        {
            CustomerId = customer.Id,
            StartedAt = new DateTime(2026, 5, 7, 7, 59, 58, DateTimeKind.Utc),
            Status = WorkEntryStatus.Running
        });
        await context.SaveChangesAsync();

        var cut = Render<Dashboard>();
        Assert.Contains("00:00:02", cut.Markup);

        _clock.UtcNow = new DateTime(2026, 5, 7, 8, 0, 3, DateTimeKind.Utc);

        cut.WaitForAssertion(() => Assert.Contains("00:00:05", cut.Markup), TimeSpan.FromSeconds(2));
    }

    public new void Dispose()
    {
        base.DisposeAsync().AsTask().GetAwaiter().GetResult();
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

            return new AppDbContext(options, AnonymousCurrentUserContext.Instance, new SystemClock());
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}

