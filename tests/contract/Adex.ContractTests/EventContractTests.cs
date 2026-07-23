using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Adex.ContractTests;

public sealed class EventContractTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    private static string Now() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private static string NewEventId() =>
        "evt_" + Adex.Infrastructure.Identifiers.UlidIdentifierGenerator.NewUlid();

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<string> DecideAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/v1/decisions", new
        {
            placement = "homepage.primary-cta",
            eligible_alternatives = new[] { new { key = "variant-a" } },
        });

        return (await ReadAsync(response)).GetProperty("decision_id").GetString()!;
    }

    [Fact]
    public async Task Accepts_an_event_and_returns_202()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = NewEventId(),
                type = "click",
                occurred_at = Now(),
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        JsonElement body = await ReadAsync(response);
        Assert.Equal("accepted", body.GetProperty("status").GetString());
        Assert.EndsWith("Z", body.GetProperty("received_at").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_a_replay_as_a_duplicate_with_the_same_status_code()
    {
        // A retry is normal. Returning an error code would force every client to
        // treat one failure as success (ADR-0012).
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        var payload = new { event_id = NewEventId(), type = "click", occurred_at = Now() };

        HttpResponseMessage first = await client.PostAsJsonAsync("/v1/events", payload);
        HttpResponseMessage replay = await client.PostAsJsonAsync("/v1/events", payload);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        Assert.Equal("accepted", (await ReadAsync(first)).GetProperty("status").GetString());
        Assert.Equal("duplicate", (await ReadAsync(replay)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deduplicates_per_tenant_not_globally()
    {
        string eventId = NewEventId();
        var payload = new { event_id = eventId, type = "click", occurred_at = Now() };

        HttpResponseMessage a = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", payload);
        HttpResponseMessage b = await factory.CreateClientFor(AdexApiFactory.TenantBKey)
            .PostAsJsonAsync("/v1/events", payload);

        Assert.Equal("accepted", (await ReadAsync(a)).GetProperty("status").GetString());
        Assert.Equal("accepted", (await ReadAsync(b)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Accepts_a_site_wide_event_with_no_decision()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = NewEventId(),
                decision_id = (string?)null,
                type = "purchase",
                occurred_at = Now(),
                properties = new { currency = "COP", item_count = 3 },
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Links_an_event_to_a_decision_of_the_same_tenant()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        string decisionId = await DecideAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/v1/events", new
        {
            event_id = NewEventId(),
            decision_id = decisionId,
            type = "lead",
            occurred_at = Now(),
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Refuses_an_event_that_cites_another_tenants_decision()
    {
        // Cross-tenant isolation at the transport boundary: tenant B must not be
        // able to attach a reward to tenant A's decision (threat T2).
        string decisionOfTenantA = await DecideAsync(factory.CreateClientFor(AdexApiFactory.TenantAKey));

        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantBKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = NewEventId(),
                decision_id = decisionOfTenantA,
                type = "lead",
                occurred_at = Now(),
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement body = await ReadAsync(response);
        Assert.Equal(
            "unknown_decision",
            body.GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_a_timestamp_that_is_not_utc()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = NewEventId(),
                type = "click",
                occurred_at = "2026-07-22T09:04:02-05:00",
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement body = await ReadAsync(response);
        Assert.Equal(
            "format",
            body.GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_a_client_clock_far_in_the_future()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = NewEventId(),
                type = "click",
                occurred_at = DateTimeOffset.UtcNow.AddHours(2)
                    .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement body = await ReadAsync(response);
        Assert.Equal(
            "clock_skew",
            body.GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_a_server_generated_looking_event_id()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = "not-a-ulid",
                type = "click",
                occurred_at = Now(),
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Requires_an_api_key()
    {
        HttpResponseMessage response = await factory.CreateClient().PostAsJsonAsync("/v1/events", new
        {
            event_id = NewEventId(),
            type = "click",
            occurred_at = Now(),
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
