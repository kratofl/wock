namespace Wock.Common.Logging;

public static class RequestLoggingContextMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLoggingContext(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestLoggingContextMiddleware>();
    }
}
