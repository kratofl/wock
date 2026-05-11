using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Common.Security;
using Wock.Common.Time;
using Wock.Data;
using Wock.Features.TimeTracking;
using Wock.Models;

namespace Wock.Tests.Features.TimeTracking;

public sealed class TimeTrackingServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;
    private FakeClock _clock = null!;

    [Fact]
    public async Task StartAsync_creates_running_entry_for_active_customer()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        _clock.UtcNow = new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc);
        var service = CreateService();

        var entry = await service.StartAsync(customer.Id, externalTicketId: "ABC-123", description: "Investigate timer");

        Assert.Equal(customer.Id, entry.CustomerId);
        Assert.Null(entry.BookingTargetId);
        Assert.Equal("ABC-123", entry.ExternalTicketId);
        Assert.Equal("Investigate timer", entry.Description);
        Assert.Equal(WorkEntryStatus.Running, entry.Status);
        Assert.Equal(_clock.UtcNow, entry.StartedAt);
        Assert.Equal(_clock.UtcNow, entry.CreatedAt);
        Assert.Null(entry.ModifiedAt);
        Assert.Equal(0, entry.TotalPausedSeconds);
        Assert.Null(entry.StoppedAt);

        var active = await service.GetActiveEntryAsync();
        Assert.NotNull(active);
        Assert.Equal(entry.Id, active.Id);
    }

    [Fact]
    public async Task StartAsync_rejects_second_entry_while_one_is_running()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        await service.StartAsync(customer.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(customer.Id));

        Assert.Contains("active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_rejects_second_entry_while_one_is_paused()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        await service.StartAsync(customer.Id);
        await service.PauseAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(customer.Id));

        Assert.Contains("active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_requires_active_customer()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var inactiveCustomer = await AddCustomerAsync(context, "Inactive", isActive: false);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.StartAsync(inactiveCustomer.Id));

        Assert.Contains("active customer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_requires_booking_target_to_be_active_and_belong_to_customer()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var otherCustomer = await AddCustomerAsync(context, "Other");
        var inactiveTarget = await AddBookingTargetAsync(context, customer.Id, "Inactive", isActive: false);
        var otherCustomerTarget = await AddBookingTargetAsync(context, otherCustomer.Id, "Other target");
        var service = CreateService();

        var inactiveException = await Assert.ThrowsAsync<ArgumentException>(() => service.StartAsync(customer.Id, inactiveTarget.Id));
        var ownershipException = await Assert.ThrowsAsync<ArgumentException>(() => service.StartAsync(customer.Id, otherCustomerTarget.Id));

        Assert.Contains("task", inactiveException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("customer", ownershipException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwitchToBookingTargetAsync_starts_target_and_derives_customer()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var target = await AddBookingTargetAsync(context, customer.Id, "Feature work");
        var service = CreateService();

        var entry = await service.SwitchToBookingTargetAsync(target.Id, "ABC-123", "Build quick switch");

        Assert.Equal(customer.Id, entry.CustomerId);
        Assert.Equal(target.Id, entry.BookingTargetId);
        Assert.Equal("ABC-123", entry.ExternalTicketId);
        Assert.Equal("Build quick switch", entry.Description);
        Assert.Equal(WorkEntryStatus.Running, entry.Status);
        Assert.Equal(_clock.UtcNow, entry.StartedAt);
    }

    [Fact]
    public async Task SwitchToBookingTargetAsync_stops_current_entry_and_starts_selected_target()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var firstTarget = await AddBookingTargetAsync(context, customer.Id, "First task");
        var nextTarget = await AddBookingTargetAsync(context, customer.Id, "Next task");
        var service = CreateService();
        var firstEntry = await service.SwitchToBookingTargetAsync(firstTarget.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(25);

        var nextEntry = await service.SwitchToBookingTargetAsync(nextTarget.Id);

        Assert.Equal(nextTarget.Id, nextEntry.BookingTargetId);
        Assert.Equal(WorkEntryStatus.Running, nextEntry.Status);
        await using var verificationContext = await _factory.CreateDbContextAsync();
        var savedFirstEntry = await verificationContext.WorkEntries.SingleAsync(entry => entry.Id == firstEntry.Id);
        Assert.Equal(WorkEntryStatus.Stopped, savedFirstEntry.Status);
        Assert.Equal(_clock.UtcNow, savedFirstEntry.StoppedAt);
        Assert.Equal(2, await verificationContext.WorkEntries.CountAsync());
    }

    [Fact]
    public async Task SwitchToBookingTargetAsync_resumes_paused_active_target()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var target = await AddBookingTargetAsync(context, customer.Id, "Paused task");
        var service = CreateService();
        var entry = await service.SwitchToBookingTargetAsync(target.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await service.PauseAsync();
        _clock.UtcNow = _clock.UtcNow.AddSeconds(75);

        var resumed = await service.SwitchToBookingTargetAsync(target.Id);

        Assert.Equal(entry.Id, resumed.Id);
        Assert.Equal(WorkEntryStatus.Running, resumed.Status);
        Assert.Equal(75, resumed.TotalPausedSeconds);
        Assert.All(resumed.Pauses, pause => Assert.NotNull(pause.ResumedAt));
        await using var verificationContext = await _factory.CreateDbContextAsync();
        Assert.Equal(1, await verificationContext.WorkEntries.CountAsync());
    }

    [Fact]
    public async Task SwitchToBookingTargetAsync_keeps_running_active_target_unchanged()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var target = await AddBookingTargetAsync(context, customer.Id, "Running task");
        var service = CreateService();
        var entry = await service.SwitchToBookingTargetAsync(target.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

        var sameEntry = await service.SwitchToBookingTargetAsync(target.Id);

        Assert.Equal(entry.Id, sameEntry.Id);
        Assert.Equal(WorkEntryStatus.Running, sameEntry.Status);
        Assert.Null(sameEntry.StoppedAt);
        await using var verificationContext = await _factory.CreateDbContextAsync();
        Assert.Equal(1, await verificationContext.WorkEntries.CountAsync());
    }

    [Fact]
    public async Task SwitchToBookingTargetAsync_rolls_back_current_timer_when_new_start_fails()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var currentTarget = await AddBookingTargetAsync(context, customer.Id, "Current task");
        var nextTarget = await AddBookingTargetAsync(context, customer.Id, "Next task");
        var service = CreateService();
        var currentEntry = await service.SwitchToBookingTargetAsync(currentTarget.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(10);
        var failingService = new TimeTrackingService(new FailingSwitchDbContextFactory(_connection, _clock), _clock);

        await Assert.ThrowsAsync<DbUpdateException>(() => failingService.SwitchToBookingTargetAsync(nextTarget.Id));

        await using var verificationContext = await _factory.CreateDbContextAsync();
        var activeEntry = await verificationContext.WorkEntries.SingleAsync(
            entry => entry.Status == WorkEntryStatus.Running || entry.Status == WorkEntryStatus.Paused);
        Assert.Equal(currentEntry.Id, activeEntry.Id);
        Assert.Equal(currentTarget.Id, activeEntry.BookingTargetId);
        Assert.Null(activeEntry.StoppedAt);
        Assert.Equal(1, await verificationContext.WorkEntries.CountAsync());
    }

    [Fact]
    public async Task PauseAsync_pauses_running_entry_and_creates_one_open_pause()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        var entry = await service.StartAsync(customer.Id);
        _clock.UtcNow = entry.StartedAt.AddMinutes(5);

        var paused = await service.PauseAsync();

        Assert.Equal(WorkEntryStatus.Paused, paused.Status);
        var saved = await context.WorkEntries.Include(workEntry => workEntry.Pauses).SingleAsync(workEntry => workEntry.Id == entry.Id);
        var pause = Assert.Single(saved.Pauses);
        Assert.Equal(_clock.UtcNow, pause.PausedAt);
        Assert.Null(pause.ResumedAt);
    }

    [Fact]
    public async Task ResumeAsync_closes_open_pause_and_accumulates_paused_seconds()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        await service.StartAsync(customer.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(10);
        await service.PauseAsync();
        _clock.UtcNow = _clock.UtcNow.AddSeconds(75);

        var resumed = await service.ResumeAsync();

        Assert.Equal(WorkEntryStatus.Running, resumed.Status);
        Assert.Equal(75, resumed.TotalPausedSeconds);
        Assert.All(resumed.Pauses, pause => Assert.NotNull(pause.ResumedAt));
    }

    [Fact]
    public async Task StopAsync_stops_running_entry()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        await service.StartAsync(customer.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(30);

        var stopped = await service.StopAsync();

        Assert.Equal(WorkEntryStatus.Stopped, stopped.Status);
        Assert.Equal(_clock.UtcNow, stopped.StoppedAt);
        Assert.Equal(0, stopped.TotalPausedSeconds);
        Assert.Null(await service.GetActiveEntryAsync());
    }

    [Fact]
    public async Task StopAsync_closes_open_pause_when_stopping_paused_entry()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var service = CreateService();
        await service.StartAsync(customer.Id);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await service.PauseAsync();
        _clock.UtcNow = _clock.UtcNow.AddSeconds(45);

        var stopped = await service.StopAsync();

        Assert.Equal(WorkEntryStatus.Stopped, stopped.Status);
        Assert.Equal(45, stopped.TotalPausedSeconds);
        Assert.All(stopped.Pauses, pause => Assert.NotNull(pause.ResumedAt));
    }

    [Fact]
    public async Task GetNetDuration_excludes_pauses_and_never_returns_negative_duration()
    {
        var service = CreateService();
        var entry = new WorkEntry
        {
            CustomerId = 1,
            StartedAt = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            StoppedAt = new DateTime(2026, 5, 7, 11, 0, 0, DateTimeKind.Utc),
            TotalPausedSeconds = 900,
            Status = WorkEntryStatus.Stopped
        };
        Assert.Equal(TimeSpan.FromMinutes(45), service.GetNetDuration(entry));

        entry.TotalPausedSeconds = 7200;

        Assert.Equal(TimeSpan.Zero, service.GetNetDuration(entry));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Concurrent_StartAsync_allows_only_one_active_work_entry()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var firstService = CreateService();
        var secondService = CreateService();

        var results = await Task.WhenAll(
            CaptureStartAsync(firstService, customer.Id),
            CaptureStartAsync(secondService, customer.Id));

        Assert.Single(results, result => result.Entry is not null);
        var exception = Assert.Single(results, result => result.Exception is not null);
        Assert.IsType<InvalidOperationException>(exception.Exception);

        await using var verificationContext = await _factory.CreateDbContextAsync();
        Assert.Equal(1, await verificationContext.WorkEntries.CountAsync(entry => entry.Status == WorkEntryStatus.Running || entry.Status == WorkEntryStatus.Paused));
    }

    [Fact]
    public async Task Database_rejects_more_than_one_active_work_entry()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var now = _clock.UtcNow;

        context.WorkEntries.AddRange(
            new WorkEntry
            {
                CustomerId = customer.Id,
                StartedAt = now,
                Status = WorkEntryStatus.Running
            },
            new WorkEntry
            {
                CustomerId = customer.Id,
                StartedAt = now.AddMinutes(1),
                Status = WorkEntryStatus.Paused
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task StartAsync_converts_database_active_entry_conflict_to_invalid_operation()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        var conflictFactory = new ConflictDbContextFactory(_connection, _clock);
        var service = new TimeTrackingService(conflictFactory, _clock);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(customer.Id));

        Assert.Contains("active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _clock = new FakeClock { UtcNow = new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc) };
        _factory = new TestDbContextFactory(_connection, _clock);
        await using var context = await _factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private TimeTrackingService CreateService()
    {
        return new TimeTrackingService(_factory, _clock);
    }

    private static async Task<(WorkEntry? Entry, Exception? Exception)> CaptureStartAsync(TimeTrackingService service, int customerId)
    {
        try
        {
            return (await service.StartAsync(customerId), null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    private static async Task<Customer> AddCustomerAsync(AppDbContext context, string name, bool isActive = true)
    {
        var customer = new Customer
        {
            Name = name,
            IsActive = isActive
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<BookingTarget> AddBookingTargetAsync(AppDbContext context, int customerId, string name, bool isActive = true)
    {
        var target = new BookingTarget
        {
            CustomerId = customerId,
            Name = name,
            BookingSoftware = "Jira",
            BookingTicketId = name.ToUpperInvariant().Replace(' ', '-'),
            IsActive = isActive
        };
        context.BookingTargets.Add(target);
        await context.SaveChangesAsync();
        return target;
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class TestDbContextFactory(SqliteConnection connection, ISystemClock clock) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new AppDbContext(options, AnonymousCurrentUserContext.Instance, clock);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class ConflictDbContextFactory(SqliteConnection connection, ISystemClock clock) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new ConflictingAppDbContext(options, connection, clock);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class FailingSwitchDbContextFactory(SqliteConnection connection, ISystemClock clock) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            return new FailingSwitchAppDbContext(options, clock);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class FailingSwitchAppDbContext(DbContextOptions<AppDbContext> options, ISystemClock clock)
        : AppDbContext(options, AnonymousCurrentUserContext.Instance, clock)
    {
        private int _saveCount;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _saveCount++;
            if (_saveCount == 2 && ChangeTracker.Entries<WorkEntry>().Any(IsAddedActiveEntry))
            {
                throw new DbUpdateException("Simulated switch start failure.");
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private static bool IsAddedActiveEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<WorkEntry> entry)
        {
            return entry.State == EntityState.Added
                && (entry.Entity.Status == WorkEntryStatus.Running || entry.Entity.Status == WorkEntryStatus.Paused);
        }
    }

    private sealed class ConflictingAppDbContext : AppDbContext
    {
        private readonly SqliteConnection _connection;
        private readonly ISystemClock _clock;
        private bool _conflictInserted;

        public ConflictingAppDbContext(
            DbContextOptions<AppDbContext> options,
            SqliteConnection connection,
            ISystemClock clock)
            : base(options, AnonymousCurrentUserContext.Instance, clock)
        {
            _connection = connection;
            _clock = clock;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pendingActiveEntry = ChangeTracker.Entries<WorkEntry>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .FirstOrDefault(entry => entry.Status == WorkEntryStatus.Running || entry.Status == WorkEntryStatus.Paused);

            if (!_conflictInserted && pendingActiveEntry is not null)
            {
                _conflictInserted = true;
                var conflictOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                await using var conflictContext = new AppDbContext(conflictOptions, AnonymousCurrentUserContext.Instance, _clock);
                var now = _clock.UtcNow.AddSeconds(1);
                conflictContext.WorkEntries.Add(new WorkEntry
                {
                    CustomerId = pendingActiveEntry.CustomerId,
                    StartedAt = now,
                    Status = WorkEntryStatus.Running
                });
                await conflictContext.SaveChangesAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}




