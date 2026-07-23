using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Adex.ContractTests;

/// <summary>
/// Hosts the real application, not a parallel wiring of it: these tests assert
/// what a client actually receives from
/// <c>packages/contracts/openapi/adex-public-v1.yaml</c>.
///
/// Configuration is overridden to remove infrastructure dependencies, so the
/// contract suite runs with no containers. Behaviour that genuinely needs
/// PostgreSQL or Redis belongs in the integration suite instead.
/// </summary>
public sealed class AdexApiFactory : WebApplicationFactory<Program>
{
    public static LockedCollection<LogRecord> ExportedLogs { get; } = [];
    public const string TenantAKey = "pk_test_reference_services";
    public const string TenantBKey = "pk_test_reference_catalog";
    public const string TenantAId = "ten_01JQZ6A1B2C3D4E5F6G7H8J9K0";
    public const string TenantBId = "ten_01JQZ6B2C3D4E5F6G7H8J9K0M1";

    /// <summary>
    /// Overrides are applied as environment variables rather than through
    /// <c>ConfigureAppConfiguration</c>.
    ///
    /// Under the minimal hosting model both the web-host and host-builder
    /// configuration callbacks are layered <em>under</em> the application's own
    /// <c>appsettings.Development.json</c>, so an override added there silently
    /// loses to it — verified, not assumed. Environment variables are the last
    /// source in the default chain and therefore win.
    ///
    /// A single space rather than an empty string, because setting an
    /// environment variable to an empty value deletes it on Windows; the code
    /// under test treats whitespace as "not configured".
    /// </summary>
    static AdexApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", " ");
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", " ");
        Environment.SetEnvironmentVariable("Adex__Persistence__Provider", "InMemory");
        Environment.SetEnvironmentVariable(
            "Adex__Privacy__SubjectSalt",
            "contract-test-seed-salt-not-a-secret");
        Environment.SetEnvironmentVariable(
            $"Adex__Tenancy__DevelopmentApiKeys__{TenantAKey}",
            TenantAId);
        Environment.SetEnvironmentVariable(
            $"Adex__Tenancy__DevelopmentApiKeys__{TenantBKey}",
            TenantBId);
        Environment.SetEnvironmentVariable(
            $"Adex__Tenancy__DevelopmentAdditionalContextKeys__{TenantAId}__0",
            "campaign_bucket");
        Environment.SetEnvironmentVariable(
            $"Adex__Tenancy__DevelopmentAdditionalContextKeys__{TenantBId}__0",
            "catalog_segment");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.AddOpenTelemetry(
            options => options.AddInMemoryExporter(ExportedLogs)));
    }

    public HttpClient CreateClientFor(string apiKey)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Adex-Api-Key", apiKey);
        return client;
    }
}
