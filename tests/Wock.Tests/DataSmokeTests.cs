using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wock.Common.Security;
using Wock.Common.Time;
using Wock.Data;
using Wock.Features.Users.Models;
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
            Name = "Example Customer"
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
            Name = "Example Customer"
        };

        context.WorkEntries.Add(new WorkEntry
        {
            Customer = customer,
            StartedAt = DateTime.UtcNow,
            TotalPausedSeconds = -1,
            Status = WorkEntryStatus.Running
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_sets_audit_and_owner_fields_for_authenticated_user()
    {
        var now = new DateTime(2026, 5, 8, 7, 0, 0, DateTimeKind.Utc);
        var userContext = new MutableCurrentUserContext("user-1", "Test User");
        var clock = new FakeClock { UtcNow = now };
        await using var context = CreateContext(userContext, clock);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser { Id = userContext.UserId!, UserName = "test.user" });
        await context.SaveChangesAsync();

        var customer = new Customer { Name = "Audited Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        Assert.Equal(now, customer.CreatedAt);
        Assert.Equal(userContext.UserId, customer.CreatedByUserId);
        Assert.Equal(userContext.UserId, customer.OwnerUserId);
        Assert.Null(customer.ModifiedAt);
        Assert.Null(customer.ModifiedByUserId);
    }

    [Fact]
    public async Task SaveChanges_sets_modified_audit_without_changing_created_audit()
    {
        var userContext = new MutableCurrentUserContext("user-1", "Test User");
        var clock = new FakeClock { UtcNow = new DateTime(2026, 5, 8, 7, 0, 0, DateTimeKind.Utc) };
        await using var context = CreateContext(userContext, clock);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser { Id = userContext.UserId!, UserName = "test.user" });
        var customer = new Customer { Name = "Audited Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var createdAt = customer.CreatedAt;

        clock.UtcNow = clock.UtcNow.AddHours(1);
        customer.Name = "Updated Customer";
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, customer.CreatedAt);
        Assert.Equal(userContext.UserId, customer.CreatedByUserId);
        Assert.Equal(clock.UtcNow, customer.ModifiedAt);
        Assert.Equal(userContext.UserId, customer.ModifiedByUserId);
    }

    [Fact]
    public async Task SaveChanges_overwrites_spoofed_audit_and_owner_fields_on_insert()
    {
        var userContext = new MutableCurrentUserContext("actual-user", "Actual User");
        await using var context = CreateContext(userContext, new FakeClock { UtcNow = new DateTime(2026, 5, 8, 7, 0, 0, DateTimeKind.Utc) });
        await context.Database.EnsureCreatedAsync();
        context.Users.AddRange(
            new ApplicationUser { Id = userContext.UserId!, UserName = "actual.user" },
            new ApplicationUser { Id = "spoofed-user", UserName = "spoofed.user" });
        await context.SaveChangesAsync();

        var customer = new Customer
        {
            Name = "Audited Customer",
            CreatedByUserId = "spoofed-user",
            OwnerUserId = "spoofed-user"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        Assert.Equal(userContext.UserId, customer.CreatedByUserId);
        Assert.Equal(userContext.UserId, customer.OwnerUserId);
    }

    [Fact]
    public async Task SaveChanges_clears_modified_by_when_later_modified_anonymously()
    {
        var userContext = new MutableCurrentUserContext("user-1", "Test User");
        var clock = new FakeClock { UtcNow = new DateTime(2026, 5, 8, 7, 0, 0, DateTimeKind.Utc) };
        await using var context = CreateContext(userContext, clock);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser { Id = userContext.UserId!, UserName = "test.user" });
        var customer = new Customer { Name = "Audited Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        clock.UtcNow = clock.UtcNow.AddHours(1);
        customer.Name = "Updated Customer";
        await context.SaveChangesAsync();
        Assert.Equal(userContext.UserId, customer.ModifiedByUserId);

        userContext.SignOut();
        clock.UtcNow = clock.UtcNow.AddHours(1);
        customer.Name = "System Updated Customer";
        await context.SaveChangesAsync();

        Assert.Equal(clock.UtcNow, customer.ModifiedAt);
        Assert.Null(customer.ModifiedByUserId);
    }

    [Fact]
    public async Task Deleting_a_user_referenced_by_audit_fields_is_restricted()
    {
        var userContext = new MutableCurrentUserContext("user-1", "Test User");
        await using var context = CreateContext(userContext, new FakeClock { UtcNow = new DateTime(2026, 5, 8, 7, 0, 0, DateTimeKind.Utc) });
        await context.Database.EnsureCreatedAsync();
        var user = new ApplicationUser { Id = userContext.UserId!, UserName = "test.user" };
        context.Users.Add(user);
        context.Customers.Add(new Customer { Name = "Audited Customer" });
        await context.SaveChangesAsync();

        context.Users.Remove(user);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("Sqlite", DatabaseOptionsExtensions.SqliteProviderName)]
    [InlineData("SqlLite", DatabaseOptionsExtensions.SqliteProviderName)]
    [InlineData("SqlServer", DatabaseOptionsExtensions.SqlServerProviderName)]
    [InlineData("MsSql", DatabaseOptionsExtensions.SqlServerProviderName)]
    [InlineData("Postgres", DatabaseOptionsExtensions.PostgresProviderName)]
    [InlineData("PostgreSQL", DatabaseOptionsExtensions.PostgresProviderName)]
    public void Configured_database_provider_selects_expected_ef_provider(
        string configuredProvider,
        string expectedProviderName)
    {
        var configuration = BuildConfiguration(configuredProvider);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseConfiguredDatabaseProvider(configuration)
            .Options;

        using var context = new AppDbContext(options, AnonymousCurrentUserContext.Instance, new SystemClock());

        Assert.Equal(expectedProviderName, context.Database.ProviderName);
    }

    [Fact]
    public async Task DbContextFactory_resolves_from_di_without_constructor_ambiguity()
    {
        var configuration = BuildConfiguration("Sqlite", "Data Source=:memory:");
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<ICurrentUserContext>(_ => AnonymousCurrentUserContext.Instance);
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseConfiguredDatabaseProvider(serviceProvider.GetRequiredService<IConfiguration>()),
            ServiceLifetime.Scoped);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        Assert.Equal(DatabaseOptionsExtensions.SqliteProviderName, context.Database.ProviderName);
    }

    private static AppDbContext CreateContext(ICurrentUserContext? currentUserContext = null, ISystemClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new AppDbContext(
            options,
            currentUserContext ?? AnonymousCurrentUserContext.Instance,
            clock ?? new SystemClock());
        context.Database.OpenConnection();
        return context;
    }

    private static IConfiguration BuildConfiguration(string provider, string? connectionString = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DatabaseOptionsExtensions.ProviderConfigurationKey] = provider,
                [DatabaseOptionsExtensions.ConnectionStringConfigurationKey] =
                    connectionString ?? ConnectionStringForProvider(provider)
            })
            .Build();
    }

    private static string ConnectionStringForProvider(string provider)
    {
        return DatabaseOptionsExtensions.ParseDatabaseProvider(provider) switch
        {
            DatabaseProvider.Sqlite => "Data Source=:memory:",
            DatabaseProvider.SqlServer => "Server=(localdb)\\mssqllocaldb;Database=Wock;Trusted_Connection=True",
            DatabaseProvider.Postgres => "Host=localhost;Database=wock;Username=wock;Password=wock",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class MutableCurrentUserContext(string? userId, string? displayName) : ICurrentUserContext
    {
        public string? UserId { get; private set; } = userId;

        public string? DisplayName { get; private set; } = displayName;

        public bool IsAuthenticated => UserId is not null;

        public void SignOut()
        {
            UserId = null;
            DisplayName = null;
        }
    }
}
