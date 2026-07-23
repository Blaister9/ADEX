using System.Text;
using System.Text.Json;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.UnitTests;

/// <summary>
/// The C# half of the cross-language seed contract.
///
/// A policy proven offline in Python is reimplemented in C# for production
/// (ADR-0003). The shared fixture file — not a description of the algorithm — is
/// what makes that promotion verifiable, so both languages assert the same
/// vectors and either one drifting fails a build.
///
/// The Python half is
/// <c>simulation/adex-simulator/tests/test_seeds.py</c>.
/// </summary>
public sealed class CrossLanguageSeedTests
{
    private sealed record SeedVector(
        string Name,
        string Salt,
        string TenantId,
        string Placement,
        string PolicyKey,
        int PolicyVersion,
        string SubjectOrDecisionId,
        uint ExpectedSeed);

    private static IReadOnlyList<SeedVector> LoadVectors()
    {
        string path = FindFixture();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        return
        [
            .. document.RootElement.GetProperty("vectors").EnumerateArray().Select(element =>
                new SeedVector(
                    element.GetProperty("name").GetString()!,
                    element.GetProperty("salt").GetString()!,
                    element.GetProperty("tenant_id").GetString()!,
                    element.GetProperty("placement").GetString()!,
                    element.GetProperty("policy_key").GetString()!,
                    element.GetProperty("policy_version").GetInt32(),
                    element.GetProperty("subject_or_decision_id").GetString()!,
                    element.GetProperty("expected_seed").GetUInt32())),
        ];
    }

    /// <summary>
    /// Walks up from the test binary to the repository root. Keeps the fixture in
    /// one place instead of copying it per test project, which is how the two
    /// copies would drift.
    /// </summary>
    private static string FindFixture()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName, "tests", "fixtures", "decision-seed-vectors.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "tests/fixtures/decision-seed-vectors.json was not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void The_shared_fixture_file_is_present_and_populated()
    {
        Assert.True(LoadVectors().Count >= 5);
    }

    [Fact]
    public void Every_shared_vector_reproduces_exactly()
    {
        foreach (SeedVector vector in LoadVectors())
        {
            uint actual = DecisionSeed.Derive(
                Encoding.UTF8.GetBytes(vector.Salt),
                TenantId.Parse(vector.TenantId),
                PlacementKey.Parse(vector.Placement),
                PolicyReference.Create(vector.PolicyKey, vector.PolicyVersion),
                vector.SubjectOrDecisionId);

            Assert.Equal(vector.ExpectedSeed, actual);
        }
    }

    [Fact]
    public void Field_boundaries_cannot_be_forged_by_concatenation()
    {
        // Without a separator, ("ab", "c") and ("a", "bc") would hash identically
        // and two different tenants could share an assignment.
        byte[] salt = "salt"u8.ToArray();
        PolicyReference policy = PolicyReference.Create("uniform-random", 1);

        uint first = DecisionSeed.Derive(
            salt,
            TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0"),
            PlacementKey.Parse("ab.c"),
            policy,
            "subject");

        uint second = DecisionSeed.Derive(
            salt,
            TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0"),
            PlacementKey.Parse("a.bc"),
            policy,
            "subject");

        Assert.NotEqual(first, second);
    }
}
