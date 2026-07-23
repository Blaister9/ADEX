using Adex.Application.Health;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Adex.Infrastructure.Caching;

/// <summary>
/// Readiness probe for the optional cache.
///
/// Redis is never on the correctness path (ADR-0004), so an outage reports
/// <see cref="HealthStatus.Degraded"/> and readiness still returns 200. Draining
/// instances because a cache is down would turn a latency problem into an
/// availability incident.
/// </summary>
public sealed class RedisDependencyProbe(
    string? connectionString,
    ILogger<RedisDependencyProbe> logger) : IDependencyProbe, IAsyncDisposable
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnectionMultiplexer? _multiplexer;

    public string Name => "redis";

    public bool Required => false;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HealthCheckResult(
                Name,
                HealthStatus.Degraded,
                Required,
                "No Redis connection string is configured; ADEX is running without cache acceleration.");
        }

        try
        {
            IConnectionMultiplexer multiplexer = await GetMultiplexerAsync(cancellationToken).ConfigureAwait(false);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProbeTimeout);

            _ = await multiplexer.GetDatabase().PingAsync().WaitAsync(timeout.Token).ConfigureAwait(false);
            return new HealthCheckResult(Name, HealthStatus.Healthy, Required);
        }
        catch (Exception exception) when (exception is RedisException or OperationCanceledException or TimeoutException)
        {
            logger.LogWarning(exception, "Redis readiness probe failed; continuing without cache acceleration.");
            return new HealthCheckResult(
                Name,
                HealthStatus.Degraded,
                Required,
                "Redis is unreachable; ADEX continues to serve from PostgreSQL.");
        }
    }

    private async Task<IConnectionMultiplexer> GetMultiplexerAsync(CancellationToken cancellationToken)
    {
        if (_multiplexer is { IsConnected: true })
        {
            return _multiplexer;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_multiplexer is { IsConnected: true })
            {
                return _multiplexer;
            }

            ConfigurationOptions configuration = ConfigurationOptions.Parse(connectionString!);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectTimeout = (int)ProbeTimeout.TotalMilliseconds;

            _multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false);
            return _multiplexer;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
