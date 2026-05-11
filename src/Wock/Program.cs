using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Formatting.Compact;
using Wock.Common.Logging;
using Wock.Common.Security;
using Wock.Components;
using Wock.Data;
using Wock.Features.BookingTargets;
using Wock.Features.Customers;
using Wock.Features.Plugins;
using Wock.Features.Reports;
using Wock.Features.TimeTracking;
using Wock.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

    if (context.HostingEnvironment.IsDevelopment())
    {
        loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    }
    else
    {
        loggerConfiguration.WriteTo.Console(new RenderedCompactJsonFormatter());
    }
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddWockInfrastructure(builder.Configuration, options =>
{
    options.StoragePath = builder.Configuration["Plugins:StoragePath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "plugins");
});
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<BookingTargetService>();
builder.Services.AddScoped<PluginRegistryService>();
builder.Services.AddScoped<TimeTrackingService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRequestLoggingContext();
app.UseSerilogRequestLogging();

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
