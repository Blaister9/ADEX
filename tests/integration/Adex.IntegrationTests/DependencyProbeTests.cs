using Adex.Application.Abstractions;
using Adex.Application.Health;
using Adex.Infrastructure.Caching;
using Adex.Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adex.IntegrationTests;

/// <summary>
/// Failure-path probes. These need no infrastructure and always run: the point
/// is that an unreachable dependency is <em>reported</em>, never thrown.
/// </summary>
public sealed class DependencyProbeFailureTests
{
    private const string DeadPostgres =
        "Host=127.0.0.1;Port=1;Database=adex;Username=adex;Password=irrelevant;Timeout=2";

    private const string DeadRedis = "127.0.0.1:1,connectTimeout=500,abortConnect=false";

    [Fact]
    public async Task Postgres_probe_reports_instead_of_throwing()
    {
        var probe = new PostgresDependencyProbe(DeadPostgres, required: true, NullLogger<PostgresDependencyProbe>.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(result.Required);
        Assert.Equal("postgres", result.Name);
    }

    [Fact]
    public async Task Postgres_outage_only_degrades_while_the_in_memory_provider_is_in_use()
    {
        var probe = new PostgresDependencyProbe(DeadPostgres, required: false, NullLogger<PostgresDependencyProbe>.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.False(result.Required);
    }

    [Fact]
    public async Task Redis_outage_degrades_and_never_fails_readiness()
    {
        // Redis is never on the correctness path, so its absence must not drain
        // an instance from a load balancer (ADR-0004).
        await using var probe = new RedisDependencyProbe(
            DeadRedis,
            NullLogger<RedisDependencyProbe>.Instance,
            NullCacheDegradationSink.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.False(result.Required);
        Assert.DoesNotContain("PostgreSQL", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Redis_reports_degraded_when_it_is_not_configured_at_all()
    {
        await using var probe = new RedisDependencyProbe(
            null,
            NullLogger<RedisDependencyProbe>.Instance,
            NullCacheDegradationSink.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("without cache", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PostgreSQL", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_unhealthy_optional_dependency_never_makes_the_report_unhealthy()
    {
        HealthReport report = HealthReport.From(
        [
            new HealthCheckResult("redis", HealthStatus.Unhealthy, Required: false),
        ]);

        Assert.Equal(HealthStatus.Degraded, report.Status);
    }
}

/// <summary>
/// Happy-path probes against the containers in <c>docker-compose.yml</c>. These
/// skip — never silently pass — when the infrastructure is not running.
/// </summary>
public sealed class DependencyProbeInfrastructureTests
{
    [Fact]
    public async Task Postgres_answers_the_readiness_query()
    {
        InfrastructureAvailability.RequireInfrastructure();

        var probe = new PostgresDependencyProbe(
            InfrastructureAvailability.PostgresConnectionString,
            required: true,
            NullLogger<PostgresDependencyProbe>.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Redis_answers_a_ping()
    {
        InfrastructureAvailability.RequireInfrastructure();

        await using var probe = new RedisDependencyProbe(
            InfrastructureAvailability.RedisConnectionString,
            NullLogger<RedisDependencyProbe>.Instance,
            NullCacheDegradationSink.Instance);

        HealthCheckResult result = await probe.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}

internal sealed class NullCacheDegradationSink : ICacheDegradationSink
{
    public static NullCacheDegradationSink Instance { get; } = new();

    public void Record(string reason)
    {
    }
}
