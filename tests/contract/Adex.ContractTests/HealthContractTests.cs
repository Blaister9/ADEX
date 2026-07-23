using System.Net;
using System.Text.Json;

namespace Adex.ContractTests;

public sealed class HealthContractTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task Liveness_needs_no_api_key_and_touches_no_dependency()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", (await ReadAsync(response)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_needs_no_api_key()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/ready");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_stays_available_when_only_an_optional_dependency_is_missing()
    {
        // This factory configures no Redis connection string, so Redis reports
        // degraded. Readiness must still be 200: a cache outage may not drain an
        // instance (ADR-0004).
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadAsync(response);
        Assert.Equal("degraded", body.GetProperty("status").GetString());

        JsonElement redis = body.GetProperty("checks").EnumerateArray()
            .Single(check => check.GetProperty("name").GetString() == "redis");
        Assert.False(redis.GetProperty("required").GetBoolean());
    }

    [Fact]
    public async Task Readiness_labels_each_dependency_as_required_or_optional()
    {
        JsonElement body = await ReadAsync(await factory.CreateClient().GetAsync("/health/ready"));

        foreach (JsonElement check in body.GetProperty("checks").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("name").GetString()));
            Assert.Contains(
                check.GetProperty("status").GetString(),
                new[] { "healthy", "degraded", "unhealthy" });
            Assert.True(check.TryGetProperty("required", out _));
        }
    }
}
