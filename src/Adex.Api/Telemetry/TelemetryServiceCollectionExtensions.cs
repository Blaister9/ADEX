using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Adex.Api.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Wires OpenTelemetry traces and metrics.
    ///
    /// Export is OTLP and only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
    /// With no collector the application must start and behave identically —
    /// observability tooling is never a runtime dependency (ADR-0011).
    /// </summary>
    public static IServiceCollection AddAdexTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string serviceName = configuration["OTEL_SERVICE_NAME"] ?? "adex-api";
        bool exportEnabled = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(AdexTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                        // Health probes are high-volume and uninteresting; tracing
                        // them would drown the signal that matters.
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health"));

                if (exportEnabled)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(AdexTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();

                if (exportEnabled)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
