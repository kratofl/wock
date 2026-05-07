namespace Wock.Abstractions;

public sealed record BookingExportRequest(
    string CustomerName,
    string? BookingTargetName,
    string BookingSoftware,
    string BookingTicketId,
    string? ExternalTicketId,
    string? Description,
    DateTime StartedAt,
    DateTime StoppedAt,
    TimeSpan NetDuration);
