using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wock.Data;
using Wock.Features.BookingTargets;
using Wock.Features.Customers;

namespace Wock.Tests.Features.BookingTargets;

public sealed class BookingTargetsPageTests
{
    [Fact]
    public async Task BookingTargetsPage_renders_management_heading_and_missing_customer_message()
    {
        await using var fixture = await PageTestFixture.CreateAsync();

        var page = fixture.TestContext.Render<BookingTargetsPage>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Booking Targets", page.Markup);
            Assert.Contains("Customer", page.Markup);
            Assert.Contains("Create an active customer", page.Markup);
        });
    }

    private sealed class PageTestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private PageTestFixture(
            BunitContext testContext,
            SqliteConnection connection,
            CustomerService customerService,
            BookingTargetService bookingTargetService)
        {
            TestContext = testContext;
            _connection = connection;
            CustomerService = customerService;
            BookingTargetService = bookingTargetService;
        }

        public BunitContext TestContext { get; }

        public CustomerService CustomerService { get; }

        public BookingTargetService BookingTargetService { get; }

        public static async Task<PageTestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var factory = new TestDbContextFactory(connection);
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.EnsureCreatedAsync();
            }

            var customerService = new CustomerService(factory);
            var bookingTargetService = new BookingTargetService(factory);
            var testContext = new BunitContext();
            testContext.Services.AddSingleton<IDbContextFactory<AppDbContext>>(factory);
            testContext.Services.AddSingleton(customerService);
            testContext.Services.AddSingleton(bookingTargetService);

            return new PageTestFixture(testContext, connection, customerService, bookingTargetService);
        }

        public async ValueTask DisposeAsync()
        {
            TestContext.Dispose();
            await _connection.DisposeAsync();
        }
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
