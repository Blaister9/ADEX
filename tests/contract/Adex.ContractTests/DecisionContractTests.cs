using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Adex.ContractTests;

public sealed class DecisionContractTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    private static readonly JsonDocumentOptions JsonOptions = default;

    private static object MinimalRequest() => new
    {
        placement = "homepage.primary-cta",
        eligible_alternatives = new[] { new { key = "request-callback" }, new { key = "book-appointment" } },
    };

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body, JsonOptions).RootElement.Clone();
    }

    [Fact]
    public async Task Rejects_a_request_with_no_api_key()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/v1/decisions", MinimalRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Rejects_a_request_with_an_unknown_api_key()
    {
        HttpResponseMessage response = await factory.CreateClientFor("pk_not_a_real_key")
            .PostAsJsonAsync("/v1/decisions", MinimalRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns_the_documented_decision_body()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", MinimalRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadAsync(response);
        Assert.Matches("^dec_[0-7][0-9A-HJKMNP-TV-Z]{25}$", body.GetProperty("decision_id").GetString());
        Assert.Contains(
            body.GetProperty("alternative_key").GetString(),
            new[] { "request-callback", "book-appointment" });
        Assert.Equal("uniform-random", body.GetProperty("policy").GetProperty("key").GetString());
        Assert.Equal(1, body.GetProperty("policy").GetProperty("version").GetInt32());
        Assert.EndsWith("Z", body.GetProperty("decided_at").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Never_selects_an_alternative_the_caller_did_not_declare()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);

        for (int attempt = 0; attempt < 25; attempt++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/v1/decisions", new
            {
                placement = "homepage.primary-cta",
                subject_id = "anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0",
                eligible_alternatives = new[] { new { key = "only-option" } },
            });

            JsonElement body = await ReadAsync(response);
            Assert.Equal("only-option", body.GetProperty("alternative_key").GetString());
        }
    }

    [Fact]
    public async Task Echoes_the_correlation_id_the_client_supplied()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
        {
            Content = JsonContent.Create(MinimalRequest()),
        };
        request.Headers.Add("X-Correlation-Id", "support-ticket-4821");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal("support-ticket-4821", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Generates_a_correlation_id_when_the_client_supplies_none()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", MinimalRequest());

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? values));
        Assert.False(string.IsNullOrWhiteSpace(values!.Single()));
    }

    [Theory]
    [InlineData("Homepage.Primary", "/placement", "pattern")]
    [InlineData(null, "/placement", "required")]
    public async Task Reports_field_level_validation_failures(string? placement, string pointer, string code)
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement,
                eligible_alternatives = new[] { new { key = "variant-a" } },
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        JsonElement body = await ReadAsync(response);
        Assert.Equal("https://contracts.adex.dev/problems/validation-failed", body.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlation_id").GetString()));

        JsonElement error = body.GetProperty("errors").EnumerateArray().First();
        Assert.Equal(pointer, error.GetProperty("pointer").GetString());
        Assert.Equal(code, error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_an_empty_eligible_set_rather_than_inventing_an_alternative()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "homepage.primary-cta",
                eligible_alternatives = Array.Empty<object>(),
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_more_alternatives_than_the_contract_allows()
    {
        var many = Enumerable.Range(0, 51).Select(i => new { key = $"variant-{i}" }).ToArray();

        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new { placement = "homepage.primary-cta", eligible_alternatives = many });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement body = await ReadAsync(response);
        Assert.Equal("max_items", body.GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_a_context_key_that_was_never_declared()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "homepage.primary-cta",
                eligible_alternatives = new[] { new { key = "variant-a" } },
                context = new Dictionary<string, object> { ["email"] = "someone@example.test" },
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement body = await ReadAsync(response);
        Assert.Equal(
            "unknown_context_key",
            body.GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Enforces_tenant_specific_context_allow_lists()
    {
        static object Request(string key) => new
        {
            placement = "homepage.primary-cta",
            eligible_alternatives = new[] { new { key = "variant-a" } },
            context = new Dictionary<string, object> { [key] = "segment-a" },
        };

        HttpResponseMessage aOwn = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", Request("campaign_bucket"));
        HttpResponseMessage aForeign = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", Request("catalog_segment"));
        HttpResponseMessage bOwn = await factory.CreateClientFor(AdexApiFactory.TenantBKey)
            .PostAsJsonAsync("/v1/decisions", Request("catalog_segment"));
        HttpResponseMessage bForeign = await factory.CreateClientFor(AdexApiFactory.TenantBKey)
            .PostAsJsonAsync("/v1/decisions", Request("campaign_bucket"));

        Assert.Equal(HttpStatusCode.OK, aOwn.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, aForeign.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bOwn.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bForeign.StatusCode);
    }

    [Fact]
    public async Task Rejects_unknown_fields_at_the_root_and_in_alternatives()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        HttpResponseMessage root = await client.PostAsJsonAsync("/v1/decisions", new
        {
            placement = "homepage.primary-cta",
            eligible_alternatives = new[] { new { key = "variant-a" } },
            force_alternative = "variant-a",
        });
        HttpResponseMessage nested = await client.PostAsJsonAsync("/v1/decisions", new
        {
            placement = "homepage.primary-cta",
            eligible_alternatives = new[] { new { key = "variant-a", weight = 1 } },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, root.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nested.StatusCode);
        Assert.Equal(
            "/force_alternative",
            (await ReadAsync(root)).GetProperty("errors")[0].GetProperty("pointer").GetString());
        Assert.Equal(
            "/eligible_alternatives/0/weight",
            (await ReadAsync(nested)).GetProperty("errors")[0].GetProperty("pointer").GetString());
    }

    [Fact]
    public async Task Replays_the_same_decision_for_the_same_idempotency_key_and_body()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        async Task<JsonElement> SendAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
            {
                Content = JsonContent.Create(MinimalRequest()),
            };
            request.Headers.Add("Idempotency-Key", "retry-request-0001");
            return await ReadAsync(await client.SendAsync(request));
        }

        JsonElement first = await SendAsync();
        JsonElement replay = await SendAsync();

        Assert.Equal(first.GetProperty("decision_id").GetString(), replay.GetProperty("decision_id").GetString());
        Assert.Equal(first.GetRawText(), replay.GetRawText());
    }

    [Fact]
    public async Task Rejects_an_idempotency_key_reused_with_a_different_body()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        async Task<HttpResponseMessage> SendAsync(string alternative)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
            {
                Content = JsonContent.Create(new
                {
                    placement = "homepage.primary-cta",
                    eligible_alternatives = new[] { new { key = alternative } },
                }),
            };
            request.Headers.Add("Idempotency-Key", "retry-request-0002");
            return await client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendAsync("variant-a")).StatusCode);
        HttpResponseMessage conflict = await SendAsync("variant-b");
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            "https://contracts.adex.dev/problems/idempotency-key-reused",
            (await ReadAsync(conflict)).GetProperty("type").GetString());
    }

    [Fact]
    public async Task Scopes_idempotency_keys_by_tenant()
    {
        async Task<string?> SendAsync(string tenantKey)
        {
            HttpClient client = factory.CreateClientFor(tenantKey);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
            {
                Content = JsonContent.Create(MinimalRequest()),
            };
            request.Headers.Add("Idempotency-Key", "shared-across-tenants");
            return (await ReadAsync(await client.SendAsync(request))).GetProperty("decision_id").GetString();
        }

        Assert.NotEqual(
            await SendAsync(AdexApiFactory.TenantAKey),
            await SendAsync(AdexApiFactory.TenantBKey));
    }

    [Fact]
    public async Task Concurrent_retries_observe_one_decision()
    {
        HttpClient client = factory.CreateClientFor(AdexApiFactory.TenantAKey);
        async Task<string?> SendAsync(int _)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
            {
                Content = JsonContent.Create(MinimalRequest()),
            };
            request.Headers.Add("Idempotency-Key", "concurrent-retry-0001");
            return (await ReadAsync(await client.SendAsync(request))).GetProperty("decision_id").GetString();
        }

        string?[] ids = await Task.WhenAll(Enumerable.Range(0, 16).Select(SendAsync));
        Assert.Single(ids.Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Rejects_a_nested_context_value()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "homepage.primary-cta",
                eligible_alternatives = new[] { new { key = "variant-a" } },
                context = new Dictionary<string, object> { ["page_group"] = new { nested = true } },
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_body_that_is_not_json()
    {
        using var content = new StringContent("not json", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsync("/v1/decisions", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Serves_two_unrelated_tenants_from_the_same_binary()
    {
        // Domain independence, asserted rather than asserted-in-prose: the same
        // deployment answers a services placement and a catalog placement with
        // no source difference.
        HttpResponseMessage services = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", MinimalRequest());

        HttpResponseMessage catalog = await factory.CreateClientFor(AdexApiFactory.TenantBKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "catalog.recommendation-slot",
                eligible_alternatives = new[] { new { key = "order-by-popularity" }, new { key = "order-by-margin" } },
            });

        Assert.Equal(HttpStatusCode.OK, services.StatusCode);
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
    }
}
