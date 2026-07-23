namespace Adex.IntegrationTests;

/// <summary>
/// Integration tests need the containers from <c>docker-compose.yml</c>.
///
/// When they are not running the affected tests report <em>skipped</em>, never
/// passed: a green suite must never mean "the infrastructure was absent so we
/// checked nothing".
///
/// <code>
/// docker compose up -d
/// ADEX_INTEGRATION=1 dotnet test tests/integration/Adex.IntegrationTests
/// </code>
/// </summary>
internal static class InfrastructureAvailability
{
    public const string EnableVariable = "ADEX_INTEGRATION";

    public static bool Enabled =>
        Environment.GetEnvironmentVariable(EnableVariable) == "1";

    // 127.0.0.1 rather than localhost: compose publishes these ports on the IPv4
    // loopback only, and on many machines `localhost` resolves to ::1 first —
    // which reports a healthy Redis as unreachable.
    public static string PostgresConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
        ?? "Host=127.0.0.1;Port=55432;Database=adex;Username=adex;Password=local-dev-only-change-me";

    public static string RedisConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? "127.0.0.1:56379";

    /// <summary>Skips the calling test unless the local infrastructure was explicitly enabled.</summary>
    public static void RequireInfrastructure()
    {
        Assert.SkipUnless(
            Enabled,
            $"Set {EnableVariable}=1 and run `docker compose up -d` to execute infrastructure tests.");
    }
}
