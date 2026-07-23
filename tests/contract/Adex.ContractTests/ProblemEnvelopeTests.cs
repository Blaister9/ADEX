using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Adex.ContractTests;

/// <summary>
/// The error envelope is part of the public contract: clients branch on it. It
/// therefore gets the same scrutiny as a success body.
/// </summary>
public sealed class ProblemEnvelopeTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private async Task<HttpResponseMessage> ValidationFailureAsync() =>
        await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "NOT A VALID KEY",
                eligible_alternatives = new[] { new { key = "variant-a" } },
            });

    [Fact]
    public async Task Uses_the_problem_json_media_type()
    {
        HttpResponseMessage response = await ValidationFailureAsync();
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Carries_a_stable_type_that_clients_can_branch_on()
    {
        JsonElement body = await ReadAsync(await ValidationFailureAsync());

        string? type = body.GetProperty("type").GetString();
        Assert.StartsWith("https://contracts.adex.dev/problems/", type, StringComparison.Ordinal);
        Assert.Equal(422, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Uses_snake_case_for_every_extension()
    {
        // One naming convention on the wire. In particular ASP.NET's camelCase
        // `traceId` is removed: `correlation_id` is the identifier a tenant
        // quotes, and two identifiers under two conventions is worse than one.
        JsonElement body = await ReadAsync(await ValidationFailureAsync());

        var standard = new HashSet<string>(StringComparer.Ordinal)
        {
            "type", "title", "status", "detail", "instance",
        };

        foreach (JsonProperty property in body.EnumerateObject())
        {
            if (standard.Contains(property.Name))
            {
                continue;
            }

            Assert.DoesNotMatch("[A-Z]", property.Name);
        }

        Assert.False(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Never_leaks_internals_in_the_detail()
    {
        using var content = new StringContent("{ this is not json", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsync("/v1/decisions", content);

        string body = await response.Content.ReadAsStringAsync();

        foreach (string forbidden in new[] { "Exception", "   at ", "Npgsql", "System.", "select " })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Carries_the_correlation_id_in_the_body_and_the_header()
    {
        HttpResponseMessage response = await ValidationFailureAsync();
        JsonElement body = await ReadAsync(response);

        string? fromBody = body.GetProperty("correlation_id").GetString();
        string fromHeader = response.Headers.GetValues("X-Correlation-Id").Single();

        Assert.Equal(fromHeader, fromBody);
    }

    [Fact]
    public async Task Names_the_offending_field_with_a_json_pointer_and_a_machine_readable_code()
    {
        JsonElement error = (await ReadAsync(await ValidationFailureAsync()))
            .GetProperty("errors").EnumerateArray().First();

        Assert.StartsWith("/", error.GetProperty("pointer").GetString(), StringComparison.Ordinal);
        Assert.Matches("^[a-z][a-z0-9_]*$", error.GetProperty("code").GetString());
    }
}
