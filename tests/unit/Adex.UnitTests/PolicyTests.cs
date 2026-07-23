using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.UnitTests;

public sealed class PolicyTests
{
    private static readonly AlternativeKey[] Three =
    [
        AlternativeKey.Parse("variant-a"),
        AlternativeKey.Parse("variant-b"),
        AlternativeKey.Parse("variant-c"),
    ];

    private static PolicySelectionInput Input(uint seed) =>
        new(Three, DecisionContext.Empty, seed);

    [Fact]
    public void Selects_only_from_the_eligible_set()
    {
        var policy = new UniformRandomPolicy();

        for (uint seed = 0; seed < 500; seed++)
        {
            Assert.Contains(policy.Select(Input(seed)).Selected, Three);
        }
    }

    [Fact]
    public void Is_pure_the_same_seed_always_yields_the_same_alternative()
    {
        var policy = new UniformRandomPolicy();
        PolicySelection first = policy.Select(Input(4_242));
        PolicySelection second = policy.Select(Input(4_242));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Reports_the_propensity_of_its_own_choice()
    {
        // Logged from the first policy onwards: off-policy evaluation cannot be
        // added to unlogged history (ADR-0009).
        PolicySelection selection = new UniformRandomPolicy().Select(Input(1));
        Assert.Equal(1d / 3d, selection.Propensity, 12);
    }

    [Fact]
    public void Allocates_uniformly_across_seeds()
    {
        var policy = new UniformRandomPolicy();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        const int draws = 30_000;
        for (uint seed = 0; seed < draws; seed++)
        {
            string key = policy.Select(Input(seed)).Selected.Value;
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Assert.Equal(3, counts.Count);
        foreach (int count in counts.Values)
        {
            Assert.InRange(count, draws / 3 - 500, draws / 3 + 500);
        }
    }

    [Fact]
    public void Refuses_to_be_called_with_an_empty_eligible_set()
    {
        // An empty set is rejected before a policy is reached; a policy that
        // "handled" it would have to invent an alternative.
        Assert.Throws<ArgumentException>(() =>
            new PolicySelectionInput([], DecisionContext.Empty, 1));
    }

    [Fact]
    public void Carries_an_explanation_that_an_operator_can_read()
    {
        Assert.False(string.IsNullOrWhiteSpace(new UniformRandomPolicy().Select(Input(7)).Explanation));
    }

    [Fact]
    public void Policy_versions_start_at_one_and_are_part_of_its_identity()
    {
        Assert.Equal(1, new UniformRandomPolicy().Reference.Version);
        Assert.Equal("uniform-random", new UniformRandomPolicy().Reference.Key);
        Assert.Throws<ArgumentOutOfRangeException>(() => PolicyReference.Create("uniform-random", 0));
    }
}

public sealed class DecisionSeedTests
{
    private static readonly byte[] Salt = "an-environment-scoped-salt"u8.ToArray();
    private static readonly TenantId Tenant = TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0");
    private static readonly PlacementKey Placement = PlacementKey.Parse("homepage.primary-cta");
    private static readonly PolicyReference Policy = PolicyReference.Create("uniform-random", 1);

    [Fact]
    public void Is_stable_for_the_same_inputs()
    {
        // This is what makes an A/B split sticky across page views.
        uint first = DecisionSeed.Derive(Salt, Tenant, Placement, Policy, "anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0");
        uint second = DecisionSeed.Derive(Salt, Tenant, Placement, Policy, "anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Separates_tenants_placements_policy_versions_and_subjects()
    {
        uint baseline = DecisionSeed.Derive(Salt, Tenant, Placement, Policy, "subject-1");

        Assert.NotEqual(baseline, DecisionSeed.Derive(
            Salt, TenantId.Parse("ten_01JQZ6B2C3D4E5F6G7H8J9K0M1"), Placement, Policy, "subject-1"));
        Assert.NotEqual(baseline, DecisionSeed.Derive(
            Salt, Tenant, PlacementKey.Parse("catalog.recommendation-slot"), Policy, "subject-1"));
        Assert.NotEqual(baseline, DecisionSeed.Derive(
            Salt, Tenant, Placement, PolicyReference.Create("uniform-random", 2), "subject-1"));
        Assert.NotEqual(baseline, DecisionSeed.Derive(
            Salt, Tenant, Placement, Policy, "subject-2"));
    }

    [Fact]
    public void Rotating_the_salt_reshuffles_assignments()
    {
        // Documented operator consequence, asserted so it cannot regress silently.
        Assert.NotEqual(
            DecisionSeed.Derive(Salt, Tenant, Placement, Policy, "subject-1"),
            DecisionSeed.Derive("a-rotated-salt"u8.ToArray(), Tenant, Placement, Policy, "subject-1"));
    }

    [Fact]
    public void Cannot_be_derived_without_a_subject_or_decision_identifier()
    {
        Assert.Throws<ArgumentException>(() =>
            DecisionSeed.Derive(Salt, Tenant, Placement, Policy, string.Empty));
    }
}
