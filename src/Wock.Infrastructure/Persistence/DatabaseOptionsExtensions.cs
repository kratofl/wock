using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Wock.Data;

public static class DatabaseOptionsExtensions
{
    public const string ProviderConfigurationKey = DatabaseOptions.SectionName + ":Provider";
    public const string ConnectionStringConfigurationKey = DatabaseOptions.SectionName + ":ConnectionString";
    public const string DefaultProviderName = "Sqlite";

    public const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";
    public const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";
    public const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    public const string SqliteMigrationsAssemblyName = "Wock.Migrations.Sqlite";
    public const string SqlServerMigrationsAssemblyName = "Wock.Migrations.SqlServer";
    public const string PostgresMigrationsAssemblyName = "Wock.Migrations.Postgres";

    public static DbContextOptionsBuilder UseConfiguredDatabaseProvider(
        this DbContextOptionsBuilder optionsBuilder,
        IConfiguration configuration)
    {
        var provider = GetConfiguredDatabaseProvider(configuration);
        var connectionString = GetConfiguredConnectionString(configuration);

        return optionsBuilder.UseDatabaseProvider(provider, connectionString);
    }

    public static DbContextOptionsBuilder<TContext> UseConfiguredDatabaseProvider<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        IConfiguration configuration)
        where TContext : DbContext
    {
        var provider = GetConfiguredDatabaseProvider(configuration);
        var connectionString = GetConfiguredConnectionString(configuration);

        optionsBuilder.UseDatabaseProvider(provider, connectionString);
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder UseDatabaseProvider(
        this DbContextOptionsBuilder optionsBuilder,
        DatabaseProvider provider,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringConfigurationKey}' is not configured.");
        }

        var migrationsAssembly = GetMigrationsAssemblyName(provider);

        return provider switch
        {
            DatabaseProvider.Sqlite => optionsBuilder.UseSqlite(
                connectionString,
                sqliteOptions => sqliteOptions.MigrationsAssembly(migrationsAssembly)),
            DatabaseProvider.SqlServer => optionsBuilder.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly)),
            DatabaseProvider.Postgres => optionsBuilder.UseNpgsql(
                connectionString,
                postgresOptions => postgresOptions.MigrationsAssembly(migrationsAssembly)),
            _ => throw new InvalidOperationException($"Unsupported database provider '{provider}'.")
        };
    }

    public static DbContextOptionsBuilder<TContext> UseDatabaseProvider<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DatabaseProvider provider,
        string connectionString)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)optionsBuilder).UseDatabaseProvider(provider, connectionString);
        return optionsBuilder;
    }

    public static DatabaseProvider GetConfiguredDatabaseProvider(IConfiguration configuration)
    {
        return ParseDatabaseProvider(configuration[ProviderConfigurationKey] ?? DefaultProviderName);
    }

    public static string GetConfiguredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration[ConnectionStringConfigurationKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringConfigurationKey}' is not configured.");
        }

        return connectionString;
    }

    public static string? GetOptionalConfiguredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration[ConnectionStringConfigurationKey];
        return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
    }

    public static DatabaseProvider ParseDatabaseProvider(string? providerName)
    {
        var normalized = providerName?
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            null or "" or "SQLITE" or "SQLLITE" => DatabaseProvider.Sqlite,
            "SQLSERVER" or "MSSQL" or "MSSQLSERVER" or "MICROSOFTSQLSERVER" => DatabaseProvider.SqlServer,
            "POSTGRES" or "POSTGRESQL" or "NPGSQL" => DatabaseProvider.Postgres,
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{providerName}'. Supported providers are Sqlite, SqlServer, and Postgres.")
        };
    }

    public static string GetMigrationsAssemblyName(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.Sqlite => SqliteMigrationsAssemblyName,
            DatabaseProvider.SqlServer => SqlServerMigrationsAssemblyName,
            DatabaseProvider.Postgres => PostgresMigrationsAssemblyName,
            _ => throw new InvalidOperationException($"Unsupported database provider '{provider}'.")
        };
    }
}
