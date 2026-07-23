using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Adex.Api.Telemetry;

/// <summary>
/// Traces and metrics for the decision path.
///
/// The cardinality rule from ADR-0011 is enforced by what is (and is not) here:
/// tenant, placement and policy are bounded by configuration and may be
/// dimensions; decision ids, subject ids and free-form context values never are.
/// </summary>
public static class AdexTelemetry
{
    public const string ActivitySourceName = "Adex.Decisioning";
    public const string MeterName = "Adex";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    public static Meter Meter { get; } = new(MeterName);

    public static Counter<long> Decisions { get; } = Meter.CreateCounter<long>(
        "adex.decisions.count",
        unit: "{decision}",
        description: "Decisions produced, by tenant, placement, policy and outcome.");

    public static Histogram<double> DecisionDuration { get; } = Meter.CreateHistogram<double>(
        "adex.decision.duration",
        unit: "ms",
        description: "Server-side duration of the decision path.");

    public static Counter<long> Events { get; } = Meter.CreateCounter<long>(
        "adex.events.count",
        unit: "{event}",
        description: "Events ingested, by tenant, type and result (accepted, duplicate, rejected).");

    public static Counter<long> CacheDegraded { get; } = Meter.CreateCounter<long>(
        "adex.cache.degraded",
        unit: "{occurrence}",
        description: "Times an optional cache was bypassed and PostgreSQL answered instead.");
}
