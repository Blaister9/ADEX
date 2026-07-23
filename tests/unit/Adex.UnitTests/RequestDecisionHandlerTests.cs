using Adex.Application.Decisions;
using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;
using Adex.Infrastructure.Persistence.InMemory;
using Adex.Infrastructure.Policies;

namespace Adex.UnitTests;

public sealed class RequestDecisionHandlerTests
{
    private static readonly TenantId TenantA = TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0");
    private static readonly TenantId TenantB = TenantId.Parse("ten_01JQZ6B2C3D4E5F6G7H8J9K0M1");
    private static readonly PlacementKey Placement = PlacementKey.Parse("homepage.primary-cta");
    private static readonly AlternativeKey[] Eligible =
    [
        AlternativeKey.Parse("variant-a"),
        AlternativeKey.Parse("variant-b"),
    ];

    private static readonly byte[] Salt = "an-environment-scoped-salt"u8.ToArray();

    private sealed record Harness(
        RequestDecisionHandler Handler,
        InMemoryDecisionStore Store,
        FixedClock Clock);

    private static Harness Build()
    {
        var clock = FixedClock.At("2026-07-22T14:03:11.482Z");
        var store = new InMemoryDecisionStore();
        var handler = new RequestDecisionHandler(
            new UniformRandomPolicyResolver(),
            store,
            new SequentialIdentifierGenerator(clock.UtcNow),
            new StaticSaltProvider("an-environment-scoped-salt"),
            clock,
            new InMemoryDecisionIdempotencyStore());

        return new Harness(handler, store, clock);
    }

    private static RequestDecisionCommand Command(TenantId tenant, SubjectId? subject = null) =>
        new(tenant, Placement, Eligible, DecisionContext.Empty, subject, "correlation-0001");

    [Fact]
    public async Task Persists_the_audit_record_before_answering()
    {
        Harness harness = Build();

        RequestDecisionResult result = await harness.Handler.HandleAsync(Command(TenantA));

        Assert.Equal(RequestDecisionStatus.Decided, result.Status);
        Assert.NotNull(result.Decision);
        Assert.Equal(1, harness.Store.Count);

        DecisionRecord? stored = await harness.Store.FindAsync(TenantA, result.Decision!.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Records_everything_needed_to_explain_the_decision()
    {
        Harness harness = Build();
        SubjectId subject = SubjectId.Parse("anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0");

        DecisionRecord decision = (await harness.Handler.HandleAsync(Command(TenantA, subject))).Decision!;

        Assert.Equal(TenantA, decision.Tenant);
        Assert.Equal(Placement, decision.Placement);
        Assert.Equal(subject, decision.Subject);
        Assert.Equal("uniform-random", decision.Policy.Key);
        Assert.Equal(1, decision.Policy.Version);
        Assert.Equal(Eligible, decision.RequestedAlternatives);
        Assert.Equal(Eligible, decision.EligibleAlternatives);
        Assert.Contains(decision.Selected, Eligible);
        Assert.Equal(0.5d, decision.Propensity, 12);
        Assert.Equal(harness.Clock.UtcNow, decision.DecidedAt);
        Assert.Equal("correlation-0001", decision.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(decision.Explanation));
    }

    [Fact]
    public async Task Stores_a_decision_that_can_be_replayed_to_the_same_alternative()
    {
        // The replay check that gates every policy: recompute the selection from
        // the stored inputs and compare it with what was served.
        Harness harness = Build();
        DecisionRecord decision = (await harness.Handler.HandleAsync(
            Command(TenantA, SubjectId.Parse("anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0")))).Decision!;

        uint recomputedSeed = DecisionSeed.Derive(
            Salt,
            decision.Tenant,
            decision.Placement,
            decision.Policy,
            decision.Subject!.Value.Value);

        PolicySelection replay = new UniformRandomPolicy(decision.Policy.Version).Select(
            new PolicySelectionInput(decision.EligibleAlternatives, decision.Context, recomputedSeed));

        Assert.Equal(decision.Seed, recomputedSeed);
        Assert.Equal(decision.Selected, replay.Selected);
        Assert.Equal(decision.Propensity, replay.Propensity, 12);
    }

    [Fact]
    public async Task Gives_the_same_subject_a_stable_assignment_across_requests()
    {
        Harness harness = Build();
        SubjectId subject = SubjectId.Parse("anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0");

        DecisionRecord first = (await harness.Handler.HandleAsync(Command(TenantA, subject))).Decision!;
        DecisionRecord second = (await harness.Handler.HandleAsync(Command(TenantA, subject))).Decision!;

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Selected, second.Selected);
    }

    [Fact]
    public async Task Refuses_to_decide_when_nothing_is_eligible()
    {
        Harness harness = Build();

        RequestDecisionResult result = await harness.Handler.HandleAsync(
            new RequestDecisionCommand(TenantA, Placement, [], DecisionContext.Empty));

        Assert.Equal(RequestDecisionStatus.NoEligibleAlternatives, result.Status);
        Assert.Null(result.Decision);
        Assert.Equal(0, harness.Store.Count);
    }

    [Fact]
    public async Task Keeps_a_decision_invisible_to_another_tenant()
    {
        Harness harness = Build();
        DecisionRecord decision = (await harness.Handler.HandleAsync(Command(TenantA))).Decision!;

        Assert.NotNull(await harness.Store.FindAsync(TenantA, decision.Id));
        Assert.Null(await harness.Store.FindAsync(TenantB, decision.Id));
    }

    [Fact]
    public async Task Separates_assignments_across_tenants_for_the_same_subject()
    {
        // The same browser visiting two ADEX tenants must not carry an
        // assignment from one to the other.
        Harness harness = Build();
        SubjectId subject = SubjectId.Parse("anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0");

        DecisionRecord a = (await harness.Handler.HandleAsync(Command(TenantA, subject))).Decision!;
        DecisionRecord b = (await harness.Handler.HandleAsync(Command(TenantB, subject))).Decision!;

        Assert.NotEqual(a.Seed, b.Seed);
    }
}
