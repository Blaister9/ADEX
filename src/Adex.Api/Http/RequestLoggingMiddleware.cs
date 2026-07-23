using System.Diagnostics;

namespace Adex.Api.Http;

/// <summary>Emits exactly one bounded-cardinality structured log per request.</summary>
public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        long startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            string tenant = context.Items.TryGetValue(TenantEndpointFilter.ItemKey, out object? value)
                ? value?.ToString() ?? "none"
                : "none";

            logger.LogInformation(
                "HTTP request completed: correlation_id={CorrelationId} tenant={Tenant} "
                + "route={Route} status={Status} duration_ms={DurationMs}",
                CorrelationIdMiddleware.Current(context),
                tenant,
                context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown",
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}
