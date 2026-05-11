using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wock.Data;
using Wock.Features.Reports;
using Wock.Models;

namespace Wock.Tests.Features.Reports;

public sealed class ReportsPageTests : BunitContext, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestDbContextFactory _factory;

    public ReportsPageTests()
    {
        _connection.Open();
        _factory = new TestDbContextFactory(_connection);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(config => config.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddSingleton<IDbContextFactory<AppDbContext>>(_factory);
        Services.AddScoped<ReportService>();
        using var context = _factory.CreateDbContext();
        context.Database.EnsureCreated();
        var customer = new Customer { Name = "Acme" };
        context.Customers.Add(customer);
        context.SaveChanges();
        context.WorkEntries.AddRange(
            CreateStoppedEntry(customer.Id, "EXT-1"),
            CreateStoppedEntry(customer.Id, "EXT-2"));
        context.SaveChanges();
    }

    [Fact]
    public void ReportsPage_renders_filter_form_table_and_export_button()
    {
        var cut = Render<ReportsPage>();

        Assert.Contains("Reports", cut.Markup);
        Assert.Contains("From", cut.Markup);
        Assert.Contains("Customer", cut.Markup);
        Assert.Contains("Task", cut.Markup);
        Assert.Contains("Reference", cut.Markup);
        Assert.Contains("External ticket", cut.Markup);
        Assert.Contains("Export CSV", cut.Markup);
        Assert.Contains("<table", cut.Markup);
        Assert.DoesNotContain("Booking ticket ID", cut.Markup);
    }

    [Fact]
    public void ReportsPage_uses_mud_date_pickers_instead_of_native_date_inputs()
    {
        var cut = Render<ReportsPage>();
        var expectedYearPrefix = $"{DateTime.UtcNow.Year}-";

        Assert.Contains(expectedYearPrefix, cut.Markup);
        Assert.DoesNotContain("type=\"date\"", cut.Markup);
    }

    public new void Dispose()
    {
        base.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _connection.Dispose();
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

    private static WorkEntry CreateStoppedEntry(int customerId, string externalTicketId)
    {
        var startedAt = DateTime.UtcNow.Date.AddHours(9);
        return new WorkEntry
        {
            CustomerId = customerId,
            ExternalTicketId = externalTicketId,
            Description = $"Entry {externalTicketId}",
            StartedAt = startedAt,
            StoppedAt = startedAt.AddHours(1),
            Status = WorkEntryStatus.Stopped,
            CreatedAt = startedAt,
            ModifiedAt = startedAt.AddHours(1)
        };
    }
}
