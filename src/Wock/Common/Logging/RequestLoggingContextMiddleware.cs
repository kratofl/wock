using System.Diagnostics;
using Wock.Common.Security;

namespace Wock.Common.Logging;

public sealed class RequestLoggingContextMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingContextMiddleware> logger)
{
    public const string RequestIdHeaderName = "X-Request-ID";

    public async Task InvokeAsync(HttpContext httpContext, ICurrentUserContext currentUserContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var requestId = ResolveRequestId(httpContext);
        httpContext.TraceIdentifier = requestId;
        httpContext.Response.Headers[RequestIdHeaderName] = requestId;
        var traceId = ResolveTraceId(httpContext);

        var scopeValues = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = currentUserContext.UserId,
            ["TraceId"] = traceId,
            ["RequestId"] = requestId
        };

        using var scope = logger.BeginScope(scopeValues);
        await next(httpContext);
    }

    private static string ResolveRequestId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(RequestIdHeaderName, out var requestIds)
            && requestIds.Count > 0
            && !string.IsNullOrWhiteSpace(requestIds[0]))
        {
            return requestIds[0]!.Trim();
        }

        return httpContext.TraceIdentifier;
    }

    private static string ResolveTraceId(HttpContext httpContext)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            if (activity.IdFormat == ActivityIdFormat.W3C && activity.TraceId != default)
            {
                return activity.TraceId.ToString();
            }

            if (!string.IsNullOrWhiteSpace(activity.Id))
            {
                return activity.Id;
            }
        }

        return httpContext.TraceIdentifier;
    }
}
