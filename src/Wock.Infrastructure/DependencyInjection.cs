using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wock.Common.Time;
using Wock.Data;
using Wock.Features.Plugins;

namespace Wock.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWockInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<PluginInstallOptions>? configurePluginInstall = null)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddDbContextFactory<AppDbContext>(
            (_, options) => options.UseConfiguredDatabaseProvider(configuration),
            ServiceLifetime.Scoped);

        if (configurePluginInstall is not null)
        {
            services.Configure(configurePluginInstall);
        }

        services.AddScoped<PluginInstallService>();
        services.AddSingleton<PluginLoader>();
        services.AddSingleton<ISystemClock, SystemClock>();

        return services;
    }
}
