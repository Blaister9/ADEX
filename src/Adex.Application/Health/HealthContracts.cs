namespace Adex.Application.Health;

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
}

/// <param name="Required">
/// When false, an unhealthy result degrades readiness instead of failing it. A
/// Redis outage must never drain an instance from a load balancer (ADR-0004).
/// </param>
public sealed record HealthCheckResult(string Name, HealthStatus Status, bool Required, string? Detail = null);

public sealed record HealthReport(HealthStatus Status, IReadOnlyList<HealthCheckResult> Checks)
{
    public static HealthReport From(IReadOnlyList<HealthCheckResult> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        bool requiredFailure = checks.Any(check => check.Required && check.Status == HealthStatus.Unhealthy);
        bool anyFailure = checks.Any(check => check.Status != HealthStatus.Healthy);

        HealthStatus status = requiredFailure
            ? HealthStatus.Unhealthy
            : anyFailure
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        return new HealthReport(status, checks);
    }
}

/// <summary>
/// One dependency ADEX can probe. Implementations must never throw: an
/// unreachable dependency is a reported status, not an exception.
/// </summary>
public interface IDependencyProbe
{
    string Name { get; }

    /// <summary>False for dependencies ADEX degrades around, such as Redis.</summary>
    bool Required { get; }

    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
