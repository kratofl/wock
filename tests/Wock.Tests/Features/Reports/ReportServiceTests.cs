using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.Reports;
using Wock.Models;

namespace Wock.Tests.Features.Reports;

public sealed class ReportServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task GetReportAsync_filters_stopped_entries_by_inclusive_date_range()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        await AddStoppedEntryAsync(context, customer, startedAt: new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc), externalTicketId: "BEFORE");
        var firstIncluded = await AddStoppedEntryAsync(context, customer, startedAt: new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc), externalTicketId: "FIRST");
        var lastIncluded = await AddStoppedEntryAsync(context, customer, startedAt: new DateTime(2026, 5, 3, 17, 0, 0, DateTimeKind.Utc), externalTicketId: "LAST");
        await AddStoppedEntryAsync(context, customer, startedAt: new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc), externalTicketId: "AFTER");
        await AddRunningEntryAsync(context, customer, startedAt: new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc), externalTicketId: "RUNNING");
        var service = CreateService();

        var rows = await service.GetReportAsync(new ReportFilter(
            FromDate: new DateOnly(2026, 5, 2),
            ToDate: new DateOnly(2026, 5, 3)));

        Assert.Equal([firstIncluded.Id, lastIncluded.Id], rows.Select(row => row.WorkEntryId));
    }

    [Fact]
    public async Task GetReportAsync_filters_by_customer_booking_target_and_external_ticket()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var acme = await AddCustomerAsync(context, "Acme");
        var globex = await AddCustomerAsync(context, "Globex");
        var acmeTarget = await AddBookingTargetAsync(context, acme, "Acme Jira", "Jira", "ACME-1");
        var otherAcmeTarget = await AddBookingTargetAsync(context, acme, "Acme Azure", "Azure Boards", "AZ-9");
        var expected = await AddStoppedEntryAsync(context, acme, acmeTarget, externalTicketId: "EXT-123");
        await AddStoppedEntryAsync(context, globex, externalTicketId: "EXT-123");
        await AddStoppedEntryAsync(context, acme, otherAcmeTarget, externalTicketId: "EXT-123");
        await AddStoppedEntryAsync(context, acme, acmeTarget, externalTicketId: "EXT-999");
        var service = CreateService();

        var rows = await service.GetReportAsync(new ReportFilter(
            FromDate: new DateOnly(2026, 5, 1),
            ToDate: new DateOnly(2026, 5, 31),
            CustomerId: acme.Id,
            BookingTargetId: acmeTarget.Id,
            ExternalTicketId: "EXT-123"));

        var row = Assert.Single(rows);
        Assert.Equal(expected.Id, row.WorkEntryId);
        Assert.Equal("Acme", row.Customer);
        Assert.Equal("Jira", row.BookingSoftware);
        Assert.Equal("ACME-1", row.BookingTicketId);
        Assert.Equal("EXT-123", row.ExternalTicketId);
    }

    [Fact]
    public async Task GetReportAsync_calculates_non_negative_net_duration_excluding_pause_duration()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme");
        await AddStoppedEntryAsync(
            context,
            customer,
            startedAt: new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            stoppedAt: new DateTime(2026, 5, 7, 11, 0, 0, DateTimeKind.Utc),
            totalPausedSeconds: 900);
        await AddStoppedEntryAsync(
            context,
            customer,
            startedAt: new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            stoppedAt: new DateTime(2026, 5, 7, 12, 5, 0, DateTimeKind.Utc),
            totalPausedSeconds: 600);
        var service = CreateService();

        var rows = await service.GetReportAsync(new ReportFilter(new DateOnly(2026, 5, 7), new DateOnly(2026, 5, 7)));

        Assert.Equal(TimeSpan.FromMinutes(15), rows[0].PauseDuration);
        Assert.Equal(TimeSpan.FromMinutes(45), rows[0].NetDuration);
        Assert.Equal(TimeSpan.Zero, rows[1].NetDuration);
    }

    [Fact]
    public async Task ExportCsvAsync_includes_stable_columns_and_escapes_commas_quotes_and_newlines()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = await AddCustomerAsync(context, "Acme, Inc.");
        var target = await AddBookingTargetAsync(context, customer, "Target", "Jira \"Cloud\"", "WOCK-1");
        await AddStoppedEntryAsync(
            context,
            customer,
            target,
            externalTicketId: "EXT-1",
            description: "Line one\r\nLine \"two\", with comma",
            startedAt: new DateTime(2026, 5, 7, 9, 0, 0, DateTimeKind.Utc),
            stoppedAt: new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
            totalPausedSeconds: 60);
        var service = CreateService();

        var csv = await service.ExportCsvAsync(new ReportFilter(new DateOnly(2026, 5, 7), new DateOnly(2026, 5, 7)));

        var lines = csv.Split("\r\n");
        Assert.Equal("Date,Customer,Booking software,Booking ticket ID,External ticket ID,Description,Start,Stop,Pause duration,Net duration", lines[0]);
        Assert.Contains("\"Acme, Inc.\"", csv);
        Assert.Contains("\"Jira \"\"Cloud\"\"\"", csv);
        Assert.Contains("\"Line one\r\nLine \"\"two\"\", with comma\"", csv);
        Assert.Contains("00:01:00,00:59:00", csv);
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

    private ReportService CreateService()
    {
        return new ReportService(_factory);
    }

    private static async Task<Customer> AddCustomerAsync(AppDbContext context, string name)
    {
        var customer = new Customer
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<BookingTarget> AddBookingTargetAsync(
        AppDbContext context,
        Customer customer,
        string name,
        string bookingSoftware,
        string bookingTicketId)
    {
        var target = new BookingTarget
        {
            CustomerId = customer.Id,
            Name = name,
            BookingSoftware = bookingSoftware,
            BookingTicketId = bookingTicketId
        };
        context.BookingTargets.Add(target);
        await context.SaveChangesAsync();
        return target;
    }

    private static Task<WorkEntry> AddStoppedEntryAsync(
        AppDbContext context,
        Customer customer,
        BookingTarget? bookingTarget = null,
        string? externalTicketId = null,
        string? description = null,
        DateTime? startedAt = null,
        DateTime? stoppedAt = null,
        int totalPausedSeconds = 0)
    {
        var start = startedAt ?? new DateTime(2026, 5, 7, 9, 0, 0, DateTimeKind.Utc);
        return AddEntryAsync(
            context,
            customer,
            WorkEntryStatus.Stopped,
            bookingTarget,
            externalTicketId,
            description,
            start,
            stoppedAt ?? start.AddHours(1),
            totalPausedSeconds);
    }

    private static Task<WorkEntry> AddRunningEntryAsync(
        AppDbContext context,
        Customer customer,
        DateTime startedAt,
        string externalTicketId)
    {
        return AddEntryAsync(
            context,
            customer,
            WorkEntryStatus.Running,
            bookingTarget: null,
            externalTicketId,
            description: null,
            startedAt,
            stoppedAt: null,
            totalPausedSeconds: 0);
    }

    private static async Task<WorkEntry> AddEntryAsync(
        AppDbContext context,
        Customer customer,
        WorkEntryStatus status,
        BookingTarget? bookingTarget,
        string? externalTicketId,
        string? description,
        DateTime startedAt,
        DateTime? stoppedAt,
        int totalPausedSeconds)
    {
        var entry = new WorkEntry
        {
            CustomerId = customer.Id,
            BookingTargetId = bookingTarget?.Id,
            ExternalTicketId = externalTicketId,
            Description = description,
            StartedAt = startedAt,
            StoppedAt = stoppedAt,
            TotalPausedSeconds = totalPausedSeconds,
            Status = status,
            CreatedAt = startedAt,
            UpdatedAt = stoppedAt ?? startedAt
        };
        context.WorkEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
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
