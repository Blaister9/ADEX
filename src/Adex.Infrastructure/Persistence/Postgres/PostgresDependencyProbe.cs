using Adex.Application.Health;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Adex.Infrastructure.Persistence.Postgres;

/// <summary>
/// Readiness probe for the system of record.
///
/// Whether PostgreSQL is <em>required</em> follows the configured persistence
/// provider rather than being hard-coded: while the foundation runs on the
/// development in-memory store the API genuinely does not depend on PostgreSQL,
/// and claiming otherwise would make readiness lie. Once the provider is
/// <c>Postgres</c> the dependency is required and an outage fails readiness,
/// because ADEX fails closed rather than serving an unlogged decision.
/// </summary>
public sealed class PostgresDependencyProbe(
    string connectionString,
    bool required,
    ILogger<PostgresDependencyProbe> logger) : IDependencyProbe
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    public string Name => "postgres";

    public bool Required { get; } = required;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(timeout.Token).ConfigureAwait(false);

            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "select 1";
            _ = await command.ExecuteScalarAsync(timeout.Token).ConfigureAwait(false);

            return new HealthCheckResult(Name, HealthStatus.Healthy, Required);
        }
        catch (Exception exception) when (exception is NpgsqlException or OperationCanceledException or TimeoutException)
        {
            // A probe reports, it never throws: an unreachable dependency is a
            // status, not an unhandled request failure.
            logger.LogWarning(exception, "PostgreSQL readiness probe failed.");
            return new HealthCheckResult(
                Name,
                Required ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                Required,
                "PostgreSQL did not answer the readiness query.");
        }
    }
}
