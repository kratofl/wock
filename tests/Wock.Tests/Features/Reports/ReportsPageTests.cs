using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        Services.AddSingleton<IDbContextFactory<AppDbContext>>(_factory);
        Services.AddScoped<ReportService>();
        using var context = _factory.CreateDbContext();
        context.Database.EnsureCreated();
        var customer = new Customer { Name = "Acme", CreatedAt = DateTime.UtcNow };
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
        Assert.Contains("External ticket", cut.Markup);
        Assert.Contains("Export CSV", cut.Markup);
        Assert.Contains("<table", cut.Markup);
    }

    public new void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
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
            UpdatedAt = startedAt.AddHours(1)
        };
    }
}
