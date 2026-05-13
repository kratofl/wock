using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wock.Data;
using Wock.Features.Customers;
using Wock.Models;

namespace Wock.Tests.Features.Customers;

public sealed class ProjectServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private TestDbContextFactory _factory = null!;

    [Fact]
    public async Task CreateProjectAsync_persists_project_for_active_customer()
    {
        var customer = await AddCustomerAsync("Acme");
        var service = CreateService();

        var project = await service.CreateProjectAsync(
            customer.Id,
            " Relaunch ",
            " Build new portal ",
            120m,
            15000m,
            BillingModel.Hourly,
            125m,
            ProjectStatus.Active);

        Assert.True(project.Id > 0);
        Assert.Equal("Relaunch", project.Name);
        Assert.Equal("Build new portal", project.Description);
        Assert.Equal(120m, project.BudgetHours);
        Assert.Equal(15000m, project.BudgetAmount);
        Assert.Equal(BillingModel.Hourly, project.BillingModel);
        Assert.Equal(125m, project.DefaultHourlyRate);

        await using var context = await _factory.CreateDbContextAsync();
        var saved = await context.Projects.SingleAsync();
        Assert.Equal(customer.Id, saved.CustomerId);
        Assert.Equal(ProjectStatus.Active, saved.Status);
    }

    [Fact]
    public async Task CreateTaskAsync_persists_task_with_category()
    {
        var customer = await AddCustomerAsync("Acme");
        var project = await AddProjectAsync(customer.Id, "Relaunch");
        await using var context = await _factory.CreateDbContextAsync();
        var category = await context.ActivityCategories.SingleAsync(category => category.Name == "Entwicklung");
        var service = CreateService();

        var task = await service.CreateTaskAsync(
            project.Id,
            " Frontend ",
            " Implement customer page ",
            category.Id,
            ProjectTaskStatus.InProgress);

        Assert.True(task.Id > 0);
        Assert.Equal("Frontend", task.Title);
        Assert.Equal("Implement customer page", task.Description);
        Assert.Equal(category.Id, task.ActivityCategoryId);
        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task ArchiveProjectAsync_hides_project_from_customer_list()
    {
        var customer = await AddCustomerAsync("Acme");
        var project = await AddProjectAsync(customer.Id, "Relaunch");
        var service = CreateService();

        await service.ArchiveProjectAsync(project.Id);

        var projects = await service.ListForCustomerAsync(customer.Id);
        Assert.Empty(projects);
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

    private ProjectService CreateService()
    {
        return new ProjectService(_factory);
    }

    private async Task<Customer> AddCustomerAsync(string name, bool isActive = true)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var customer = new Customer
        {
            Name = name,
            IsActive = isActive
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private async Task<Project> AddProjectAsync(int customerId, string name)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var project = new Project
        {
            CustomerId = customerId,
            Name = name,
            Status = ProjectStatus.Active
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
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
