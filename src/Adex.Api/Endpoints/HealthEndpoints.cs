using Adex.Api.Contracts;
using Adex.Application.Health;

namespace Adex.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Liveness never touches a dependency: it answers "is this process
        // running", and nothing else. A liveness probe that checks the database
        // turns a database blip into a restart storm.
        endpoints.MapGet("/health/live", () => Results.Ok(
                new HealthReportDto("healthy", [])))
            .WithName("getLiveness")
            .AllowAnonymous();

        endpoints.MapGet("/health/ready", async (
                IEnumerable<IDependencyProbe> probes,
                CancellationToken cancellationToken) =>
            {
                HealthCheckResult[] results = await Task
                    .WhenAll(probes.Select(probe => probe.CheckAsync(cancellationToken)))
                    .ConfigureAwait(false);

                HealthReport report = HealthReport.From(results);

                var dto = new HealthReportDto(
                    Describe(report.Status),
                    [.. report.Checks.Select(check => new HealthCheckDto(
                        check.Name,
                        Describe(check.Status),
                        check.Required,
                        check.Detail))]);

                // Degraded still returns 200. Draining an instance because an
                // optional cache is down would turn a latency problem into an
                // availability incident (ADR-0004).
                return report.Status == HealthStatus.Unhealthy
                    ? Results.Json(dto, statusCode: StatusCodes.Status503ServiceUnavailable)
                    : Results.Ok(dto);
            })
            .WithName("getReadiness")
            .AllowAnonymous();

        return endpoints;
    }

    private static string Describe(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "healthy",
        HealthStatus.Degraded => "degraded",
        _ => "unhealthy",
    };
}
