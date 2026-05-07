namespace Wock.Abstractions;

public sealed record BookingExportResult(
    bool Success,
    string? ExternalReference = null,
    string? Message = null,
    string? ErrorDetails = null);
