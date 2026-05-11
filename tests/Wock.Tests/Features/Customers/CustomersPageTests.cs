using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wock.Data;
using Wock.Features.Customers;

namespace Wock.Tests.Features.Customers;

public sealed class CustomersPageTests
{
    [Fact]
    public async Task CustomersPage_renders_customer_management_heading_and_create_action()
    {
        await using var fixture = await PageTestFixture.CreateAsync();

        var page = fixture.TestContext.Render<CustomersPage>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Customers", page.Markup);
            Assert.Contains("Create customer", page.Markup);
            Assert.Contains("No active customers", page.Markup);
        });
    }

    private sealed class PageTestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private PageTestFixture(
            BunitContext testContext,
            SqliteConnection connection,
            CustomerService customerService)
        {
            TestContext = testContext;
            _connection = connection;
            CustomerService = customerService;
        }

        public BunitContext TestContext { get; }

        public CustomerService CustomerService { get; }

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
            var testContext = new BunitContext();
            testContext.JSInterop.Mode = JSRuntimeMode.Loose;
            testContext.Services.AddMudServices(config => config.PopoverOptions.CheckForPopoverProvider = false);
            testContext.Services.AddSingleton<IDbContextFactory<AppDbContext>>(factory);
            testContext.Services.AddSingleton(customerService);

            return new PageTestFixture(testContext, connection, customerService);
        }

        public async ValueTask DisposeAsync()
        {
            await TestContext.DisposeAsync();
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

            return new AppDbContext(options, AnonymousCurrentUserContext.Instance, new SystemClock());
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
