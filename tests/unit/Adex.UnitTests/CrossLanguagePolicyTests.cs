using System.Text.Json;
using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Policies;

namespace Adex.UnitTests;

public sealed class CrossLanguagePolicyTests
{
    [Fact]
    public void Uniform_random_matches_every_shared_policy_vector()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(FindFixture()));
        var policy = new UniformRandomPolicy(
            document.RootElement.GetProperty("policy_version").GetInt32());

        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            AlternativeKey[] eligible =
            [
                .. vector.GetProperty("eligible_alternatives").EnumerateArray()
                    .Select(item => AlternativeKey.Parse(item.GetString()!)),
            ];

            PolicySelection actual = policy.Select(new PolicySelectionInput(
                eligible,
                DecisionContext.Empty,
                vector.GetProperty("seed").GetUInt32()));

            Assert.Equal(vector.GetProperty("expected_alternative").GetString(), actual.Selected.Value);
            Assert.Equal(vector.GetProperty("expected_propensity").GetDouble(), actual.Propensity, 15);
        }
    }

    private static string FindFixture()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName, "tests", "fixtures", "uniform-random-policy-vectors.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Shared uniform-random policy fixture was not found.");
    }
}
