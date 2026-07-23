using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using OpenTelemetry.Logs;

namespace Adex.ContractTests;

public sealed class ObservabilityContractTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    [Fact]
    public async Task In_memory_exporter_observes_the_complete_structured_request_log()
    {
        const string correlationId = "otel-log-contract-0001";
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
        {
            Content = JsonContent.Create(new
            {
                placement = "homepage.primary-cta",
                eligible_alternatives = new[] { new { key = "variant-a" } },
            }),
        };
        request.Headers.Add("X-Correlation-Id", correlationId);

        _ = await client.SendAsync(request);

        LogRecord log = Assert.Single(
            AdexApiFactory.ExportedLogs,
            record => record.Attributes?.Any(pair =>
                pair.Key == "CorrelationId" && Equals(pair.Value, correlationId)) == true);
        IReadOnlyDictionary<string, object?> fields = log.Attributes!
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        Assert.Equal(AdexApiFactory.TenantAId, fields["Tenant"]?.ToString());
        Assert.Contains("/v1/decisions", fields["Route"]?.ToString(), StringComparison.Ordinal);
        Assert.Equal(200, fields["Status"]);
        Assert.True(Convert.ToDouble(fields["DurationMs"], System.Globalization.CultureInfo.InvariantCulture) >= 0);
        Assert.DoesNotContain(fields.Keys, key => key.Contains("subject", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fields.Keys, key => key.Contains("decision_id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Redis_unavailable_path_increments_cache_degraded_without_high_cardinality_tags()
    {
        var measurements = new ConcurrentBag<Measurement<long>>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Adex" && instrument.Name == "adex.cache.degraded")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
            measurements.Add(new Measurement<long>(measurement, tags)));
        listener.Start();

        _ = await factory.CreateClient().GetAsync("/health/ready");

        Measurement<long>[] snapshot = measurements.ToArray();
        Assert.NotEmpty(snapshot);
        foreach (Measurement<long> observed in snapshot)
        {
            Assert.Equal(1, observed.Value);
            Assert.Equal("not_configured", observed.Tags.ToArray().Single().Value);
            Assert.DoesNotContain(
                observed.Tags.ToArray(),
                tag => tag.Key.Contains("tenant", StringComparison.Ordinal));
            Assert.DoesNotContain(
                observed.Tags.ToArray(),
                tag => tag.Key.Contains("decision", StringComparison.Ordinal));
        }
    }
}
