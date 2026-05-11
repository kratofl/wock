using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Wock.Common.Security;
using Wock.Common.Time;

namespace Wock.Data;

public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var provider = TryReadArgument(args, "provider") is { } providerName
            ? DatabaseOptionsExtensions.ParseDatabaseProvider(providerName)
            : DatabaseOptionsExtensions.GetConfiguredDatabaseProvider(configuration);
        var connectionString = TryReadArgument(args, "connection")
            ?? TryReadArgument(args, "connection-string")
            ?? DatabaseOptionsExtensions.GetOptionalConfiguredConnectionString(configuration);

        if (string.IsNullOrWhiteSpace(connectionString) && provider == DatabaseProvider.Sqlite)
        {
            connectionString = "Data Source=wock.design.db";
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseDatabaseProvider(provider, connectionString ?? string.Empty)
            .Options;

        return new AppDbContext(options, AnonymousCurrentUserContext.Instance, new SystemClock());
    }

    private static IConfiguration BuildConfiguration()
    {
        var contentRoot = ResolveContentRoot();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveContentRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        var projectDirectory = Path.Combine(currentDirectory, "src", "Wock");
        if (File.Exists(Path.Combine(projectDirectory, "appsettings.json")))
        {
            return projectDirectory;
        }

        return currentDirectory;
    }

    private static string? TryReadArgument(string[] args, string name)
    {
        var optionName = $"--{name}";
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                return argument[(optionName.Length + 1)..];
            }

            if (string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
