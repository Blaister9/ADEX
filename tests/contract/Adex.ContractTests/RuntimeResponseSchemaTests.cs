using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Json.Schema;

namespace Adex.ContractTests;

public sealed class RuntimeResponseSchemaTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> Schemas =
        new(LoadSchemas, LazyThreadSafetyMode.ExecutionAndPublication);

    [Fact]
    public async Task Decision_success_matches_the_published_response_schema()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "homepage.primary-cta",
                eligible_alternatives = new[] { new { key = "variant-a" } },
            });

        Assert.Equal(200, (int)response.StatusCode);
        await AssertMatchesSchemaAsync(response, "decision-response.schema.json");
    }

    [Fact]
    public async Task Event_success_matches_the_published_response_schema()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/events", new
            {
                event_id = "evt_" + Adex.Infrastructure.Identifiers.UlidIdentifierGenerator.NewUlid(),
                type = "click",
                occurred_at = DateTimeOffset.UtcNow.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.InvariantCulture),
            });

        Assert.Equal(202, (int)response.StatusCode);
        await AssertMatchesSchemaAsync(response, "event-response.schema.json");
    }

    [Fact]
    public async Task Error_response_matches_the_published_problem_schema()
    {
        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsJsonAsync("/v1/decisions", new
            {
                placement = "INVALID",
                eligible_alternatives = new[] { new { key = "variant-a" } },
            });

        Assert.Equal(422, (int)response.StatusCode);
        await AssertMatchesSchemaAsync(response, "problem.schema.json");
    }

    private static async Task AssertMatchesSchemaAsync(HttpResponseMessage response, string schemaName)
    {
        JsonSchema schema = Schemas.Value[schemaName];
        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = true,
        };

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        EvaluationResults result = schema.Evaluate(body.RootElement, options);
        Assert.True(result.IsValid, result.ToString());
    }

    private static IReadOnlyDictionary<string, JsonSchema> LoadSchemas()
    {
        string directory = FindSchemaDirectory();
        string[] names =
        [
            "common.schema.json",
            "decision-response.schema.json",
            "event-response.schema.json",
            "problem.schema.json",
        ];

        return names.ToDictionary(
            name => name,
            name => JsonSchema.FromText(File.ReadAllText(Path.Combine(directory, name))),
            StringComparer.Ordinal);
    }

    private static string FindSchemaDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "packages", "contracts", "schemas");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find packages/contracts/schemas.");
    }
}
