namespace Wock.Abstractions;

public interface IBookingConnector
{
    WockPluginMetadata Metadata { get; }

    string Name { get; }

    Task<BookingExportResult> ValidateAsync(
        BookingExportRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingExportResult> ExportAsync(
        BookingExportRequest request,
        CancellationToken cancellationToken = default);
}
