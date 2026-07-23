using Adex.Application.Abstractions;

namespace Adex.Api.Telemetry;

public sealed class OpenTelemetryCacheDegradationSink : ICacheDegradationSink
{
    public void Record(string reason) =>
        AdexTelemetry.CacheDegraded.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));
}
