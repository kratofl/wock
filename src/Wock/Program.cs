using Microsoft.EntityFrameworkCore;
using Wock.Components;
using Wock.Data;
using Wock.Features.BookingTargets;
using Wock.Features.Customers;
using Wock.Features.Plugins;
using Wock.Features.Reports;
using Wock.Features.TimeTracking;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("WockDb")
    ?? throw new InvalidOperationException("Connection string 'WockDb' is not configured.");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<BookingTargetService>();
builder.Services.Configure<PluginInstallOptions>(options =>
{
    options.StoragePath = builder.Configuration["Plugins:StoragePath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "plugins");
});
builder.Services.AddScoped<PluginInstallService>();
builder.Services.AddScoped<PluginRegistryService>();
builder.Services.AddSingleton<PluginLoader>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddScoped<TimeTrackingService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    using var scope = app.Services.CreateScope();
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}
catch (Exception exception)
{
    app.Logger.LogError(exception, "Failed to apply database migrations.");
    throw;
}

app.Run();
