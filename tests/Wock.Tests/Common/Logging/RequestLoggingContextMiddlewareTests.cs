using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wock.Common.Logging;
using Wock.Common.Security;

namespace Wock.Tests.Common.Logging;

public class RequestLoggingContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesIncomingRequestIdAndUserIdInScope()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "fallback-request"
        };
        context.Request.Headers[RequestLoggingContextMiddleware.RequestIdHeaderName] = " request-123 ";
        var logger = new CapturingLogger<RequestLoggingContextMiddleware>();
        IReadOnlyDictionary<string, object?>? scopeDuringRequest = null;
        var userContext = new TestCurrentUserContext("user-1", "Test User");
        var middleware = new RequestLoggingContextMiddleware(
            _ =>
            {
                scopeDuringRequest = logger.CurrentScope;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context, userContext);

        var scope = AssertScope(scopeDuringRequest);
        Assert.Equal("request-123", context.TraceIdentifier);
        Assert.Equal("request-123", context.Response.Headers[RequestLoggingContextMiddleware.RequestIdHeaderName]);
        Assert.Equal("request-123", scope["RequestId"]);
        Assert.Equal("request-123", scope["TraceId"]);
        Assert.Equal("user-1", scope["UserId"]);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToTraceIdentifierWhenRequestIdHeaderIsMissing()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "fallback-request"
        };
        var logger = new CapturingLogger<RequestLoggingContextMiddleware>();
        IReadOnlyDictionary<string, object?>? scopeDuringRequest = null;
        var userContext = new TestCurrentUserContext("user-1", "Test User");
        var middleware = new RequestLoggingContextMiddleware(
            _ =>
            {
                scopeDuringRequest = logger.CurrentScope;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context, userContext);

        var scope = AssertScope(scopeDuringRequest);
        Assert.Equal("fallback-request", context.TraceIdentifier);
        Assert.Equal("fallback-request", context.Response.Headers[RequestLoggingContextMiddleware.RequestIdHeaderName]);
        Assert.Equal("fallback-request", scope["RequestId"]);
        Assert.Equal("fallback-request", scope["TraceId"]);
    }

    [Fact]
    public async Task InvokeAsync_UsesActivityTraceIdWhenAvailable()
    {
        using var activity = new Activity("request");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var context = new DefaultHttpContext
        {
            TraceIdentifier = "fallback-request"
        };
        context.Request.Headers[RequestLoggingContextMiddleware.RequestIdHeaderName] = "request-456";
        var logger = new CapturingLogger<RequestLoggingContextMiddleware>();
        IReadOnlyDictionary<string, object?>? scopeDuringRequest = null;
        var userContext = new TestCurrentUserContext("user-1", "Test User");
        var middleware = new RequestLoggingContextMiddleware(
            _ =>
            {
                scopeDuringRequest = logger.CurrentScope;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context, userContext);

        var scope = AssertScope(scopeDuringRequest);
        Assert.Equal("request-456", scope["RequestId"]);
        Assert.Equal(activity.TraceId.ToString(), scope["TraceId"]);
    }

    [Fact]
    public async Task InvokeAsync_KeepsUserIdNullForAnonymousRequests()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "anonymous-request"
        };
        var logger = new CapturingLogger<RequestLoggingContextMiddleware>();
        IReadOnlyDictionary<string, object?>? scopeDuringRequest = null;
        var userContext = new TestCurrentUserContext(null, null);
        var middleware = new RequestLoggingContextMiddleware(
            _ =>
            {
                scopeDuringRequest = logger.CurrentScope;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context, userContext);

        var scope = AssertScope(scopeDuringRequest);
        Assert.Null(scope["UserId"]);
        Assert.Equal("anonymous-request", scope["RequestId"]);
        Assert.Equal("anonymous-request", scope["TraceId"]);
    }

    private static IReadOnlyDictionary<string, object?> AssertScope(
        IReadOnlyDictionary<string, object?>? scope)
    {
        Assert.NotNull(scope);
        return scope;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public IReadOnlyDictionary<string, object?>? CurrentScope { get; private set; }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            var previousScope = CurrentScope;
            CurrentScope = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["Scope"] = state
                };

            return new Scope(() => CurrentScope = previousScope);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class Scope(Action dispose) : IDisposable
        {
            public void Dispose()
            {
                dispose();
            }
        }
    }

    private sealed class TestCurrentUserContext(string? userId, string? displayName) : ICurrentUserContext
    {
        public string? UserId { get; } = userId;

        public string? DisplayName { get; } = displayName;

        public bool IsAuthenticated => UserId is not null;
    }
}
