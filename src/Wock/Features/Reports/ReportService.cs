using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wock.Abstractions;
using Wock.Data;
using Wock.Features.Plugins;
using Wock.Models;

namespace Wock.Features.Reports;

public sealed record ReportFilter(
    DateOnly FromDate,
    DateOnly ToDate,
    int? CustomerId = null,
    int? BookingTargetId = null,
    string? ExternalTicketId = null);

public sealed record ReportRow(
    int WorkEntryId,
    DateOnly Date,
    string Customer,
    string? BookingSoftware,
    string? BookingTicketId,
    string? ExternalTicketId,
    string? Description,
    DateTime Start,
    DateTime Stop,
    TimeSpan PauseDuration,
    TimeSpan NetDuration);

public sealed record ReportFilterOptions(
    IReadOnlyList<Customer> Customers,
    IReadOnlyList<BookingTarget> BookingTargets,
    IReadOnlyList<WockPluginMetadata> BookingConnectors);

public sealed class ReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly PluginRegistryService? _pluginRegistry;

    public ReportService(IDbContextFactory<AppDbContext> dbContextFactory)
        : this(dbContextFactory, null)
    {
    }

    public ReportService(IDbContextFactory<AppDbContext> dbContextFactory, PluginRegistryService? pluginRegistry)
    {
        _dbContextFactory = dbContextFactory;
        _pluginRegistry = pluginRegistry;
    }

    private static readonly string[] CsvColumns =
    [
        "Date",
        "Customer",
        "Booking software",
        "Booking ticket ID",
        "External ticket ID",
        "Description",
        "Start",
        "Stop",
        "Pause duration",
        "Net duration"
    ];

    public async Task<IReadOnlyList<ReportRow>> GetReportAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var from = ToStartOfDay(filter.FromDate);
        var toExclusive = ToStartOfDay(filter.ToDate.AddDays(1));
        var externalTicketId = Normalize(filter.ExternalTicketId);

        var query = context.WorkEntries
            .AsNoTracking()
            .Include(entry => entry.Customer)
            .Include(entry => entry.BookingTarget)
            .Where(entry => entry.Status == WorkEntryStatus.Stopped && entry.StoppedAt != null)
            .Where(entry => entry.StartedAt >= from && entry.StartedAt < toExclusive);

        if (filter.CustomerId is not null)
        {
            query = query.Where(entry => entry.CustomerId == filter.CustomerId);
        }

        if (filter.BookingTargetId is not null)
        {
            query = query.Where(entry => entry.BookingTargetId == filter.BookingTargetId);
        }

        if (externalTicketId is not null)
        {
            query = query.Where(entry => entry.ExternalTicketId == externalTicketId);
        }

        var entries = await query
            .OrderBy(entry => entry.StartedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

        return entries.Select(ToReportRow).ToList();
    }

    public async Task<string> ExportCsvAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetReportAsync(filter, cancellationToken);
        return ExportCsv(rows);
    }

    public string ExportCsv(IEnumerable<ReportRow> rows)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, CsvColumns);

        foreach (var row in rows)
        {
            AppendCsvRow(builder,
            [
                row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.Customer,
                row.BookingSoftware ?? string.Empty,
                row.BookingTicketId ?? string.Empty,
                row.ExternalTicketId ?? string.Empty,
                row.Description ?? string.Empty,
                FormatDateTime(row.Start),
                FormatDateTime(row.Stop),
                row.PauseDuration.ToString("c", CultureInfo.InvariantCulture),
                row.NetDuration.ToString("c", CultureInfo.InvariantCulture)
            ]);
        }

        return builder.ToString();
    }

    public async Task<ReportFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var customers = await context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
        var bookingTargets = await context.BookingTargets
            .AsNoTracking()
            .Include(target => target.Customer)
            .OrderBy(target => target.Customer.Name)
            .ThenBy(target => target.Name)
            .ToListAsync(cancellationToken);

        var bookingConnectors = _pluginRegistry is null
            ? []
            : (await _pluginRegistry.GetEnabledBookingConnectorsAsync(cancellationToken))
                .Select(connector => connector.Metadata)
                .ToList();

        return new ReportFilterOptions(customers, bookingTargets, bookingConnectors);
    }

    private static ReportRow ToReportRow(WorkEntry entry)
    {
        var stop = entry.StoppedAt!.Value;
        var pauseDuration = TimeSpan.FromSeconds(Math.Max(0, entry.TotalPausedSeconds));
        var grossDuration = TimeSpan.FromSeconds(Math.Max(0, (stop - entry.StartedAt).TotalSeconds));
        var netDuration = grossDuration - pauseDuration;

        if (netDuration < TimeSpan.Zero)
        {
            netDuration = TimeSpan.Zero;
        }

        return new ReportRow(
            entry.Id,
            DateOnly.FromDateTime(entry.StartedAt),
            entry.Customer.Name,
            entry.BookingTarget?.BookingSoftware,
            entry.BookingTarget?.BookingTicketId,
            entry.ExternalTicketId,
            entry.Description,
            entry.StartedAt,
            stop,
            pauseDuration,
            netDuration);
    }

    private static void AppendCsvRow(StringBuilder builder, IEnumerable<string> values)
    {
        builder.AppendJoin(',', values.Select(EscapeCsv));
        builder.Append("\r\n");
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static DateTime ToStartOfDay(DateOnly date)
    {
        return DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
