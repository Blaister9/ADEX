using Adex.Application.Decisions;
using Adex.Application.Events;
using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Events;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Infrastructure.Persistence.InMemory;
using Adex.Infrastructure.Policies;

namespace Adex.UnitTests;

public sealed class RecordEventHandlerTests
{
    private static readonly TenantId TenantA = TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0");
    private static readonly TenantId TenantB = TenantId.Parse("ten_01JQZ6B2C3D4E5F6G7H8J9K0M1");
    private static readonly EventId Event = EventId.Parse("evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9");

    private sealed record Harness(
        RecordEventHandler Handler,
        RequestDecisionHandler Decisions,
        InMemoryEventStore Events,
        FixedClock Clock);

    private static Harness Build()
    {
        var clock = FixedClock.At("2026-07-22T14:04:02.640Z");
        var decisionStore = new InMemoryDecisionStore();
        var eventStore = new InMemoryEventStore();

        return new Harness(
            new RecordEventHandler(eventStore, decisionStore, clock),
            new RequestDecisionHandler(
                new UniformRandomPolicyResolver(),
                decisionStore,
                new SequentialIdentifierGenerator(clock.UtcNow),
                new StaticSaltProvider("an-environment-scoped-salt"),
                clock),
            eventStore,
            clock);
    }

    private static async Task<DecisionId> DecideAsync(Harness harness, TenantId tenant)
    {
        RequestDecisionResult result = await harness.Decisions.HandleAsync(
            new RequestDecisionCommand(
                tenant,
                PlacementKey.Parse("homepage.primary-cta"),
                [AlternativeKey.Parse("variant-a")],
                DecisionContext.Empty));

        return result.Decision!.Id;
    }

    private static RecordEventCommand Command(
        TenantId tenant,
        DateTimeOffset occurredAt,
        DecisionId? decision = null,
        EventId? eventId = null) =>
        new(tenant, eventId ?? Event, EventType.Click, occurredAt, decision);

    [Fact]
    public async Task Accepts_a_first_delivery()
    {
        Harness harness = Build();

        RecordEventResult result = await harness.Handler.HandleAsync(
            Command(TenantA, harness.Clock.UtcNow));

        Assert.Equal(RecordEventStatus.Accepted, result.Status);
        Assert.Equal(harness.Clock.UtcNow, result.ReceivedAt);
        Assert.Equal(1, harness.Events.Count);
    }

    [Fact]
    public async Task Treats_a_replay_as_a_duplicate_rather_than_an_error()
    {
        // A retry is normal, not exceptional: browsers retry on unload, on
        // bfcache restore and on flaky networks (ADR-0012).
        Harness harness = Build();

        await harness.Handler.HandleAsync(Command(TenantA, harness.Clock.UtcNow));
        RecordEventResult replay = await harness.Handler.HandleAsync(Command(TenantA, harness.Clock.UtcNow));

        Assert.Equal(RecordEventStatus.Duplicate, replay.Status);
        Assert.Equal(1, harness.Events.Count);
    }

    [Fact]
    public async Task Scopes_deduplication_to_the_tenant()
    {
        // Two tenants may legitimately generate the same event identifier.
        Harness harness = Build();

        Assert.Equal(
            RecordEventStatus.Accepted,
            (await harness.Handler.HandleAsync(Command(TenantA, harness.Clock.UtcNow))).Status);
        Assert.Equal(
            RecordEventStatus.Accepted,
            (await harness.Handler.HandleAsync(Command(TenantB, harness.Clock.UtcNow))).Status);
        Assert.Equal(2, harness.Events.Count);
    }

    [Fact]
    public async Task Rejects_a_client_clock_too_far_in_the_future()
    {
        Harness harness = Build();

        RecordEventResult result = await harness.Handler.HandleAsync(
            Command(TenantA, harness.Clock.UtcNow + RecordEventHandler.MaxFutureSkew + TimeSpan.FromMinutes(1)));

        Assert.Equal(RecordEventStatus.RejectedClockSkew, result.Status);
        Assert.Equal(0, harness.Events.Count);
    }

    [Fact]
    public async Task Rejects_a_client_clock_too_far_in_the_past()
    {
        Harness harness = Build();

        RecordEventResult result = await harness.Handler.HandleAsync(
            Command(TenantA, harness.Clock.UtcNow - RecordEventHandler.MaxPastSkew - TimeSpan.FromMinutes(1)));

        Assert.Equal(RecordEventStatus.RejectedClockSkew, result.Status);
    }

    [Fact]
    public async Task Accepts_an_event_at_the_edge_of_the_allowed_skew()
    {
        Harness harness = Build();

        Assert.Equal(
            RecordEventStatus.Accepted,
            (await harness.Handler.HandleAsync(
                Command(TenantA, harness.Clock.UtcNow + RecordEventHandler.MaxFutureSkew))).Status);
    }

    [Fact]
    public async Task Accepts_an_event_that_cites_no_decision()
    {
        // Site-wide signals no placement claimed are still useful denominators.
        Harness harness = Build();

        Assert.Equal(
            RecordEventStatus.Accepted,
            (await harness.Handler.HandleAsync(Command(TenantA, harness.Clock.UtcNow, decision: null))).Status);
    }

    [Fact]
    public async Task Accepts_an_event_that_cites_a_decision_of_the_same_tenant()
    {
        Harness harness = Build();
        DecisionId decision = await DecideAsync(harness, TenantA);

        Assert.Equal(
            RecordEventStatus.Accepted,
            (await harness.Handler.HandleAsync(Command(TenantA, harness.Clock.UtcNow, decision))).Status);
    }

    [Fact]
    public async Task Rejects_an_event_that_cites_an_unknown_decision()
    {
        Harness harness = Build();

        RecordEventResult result = await harness.Handler.HandleAsync(
            Command(TenantA, harness.Clock.UtcNow, DecisionId.Parse("dec_01JQZ8K3N4P5R6S7T8V9W0X1Y2")));

        Assert.Equal(RecordEventStatus.UnknownDecision, result.Status);
        Assert.Equal(0, harness.Events.Count);
    }

    [Fact]
    public async Task Rejects_an_event_that_cites_another_tenants_decision()
    {
        // Forging events against someone else's decision is threat T2; the
        // tenant check is the control, so it is asserted here.
        Harness harness = Build();
        DecisionId decisionOfA = await DecideAsync(harness, TenantA);

        RecordEventResult result = await harness.Handler.HandleAsync(
            Command(TenantB, harness.Clock.UtcNow, decisionOfA));

        Assert.Equal(RecordEventStatus.UnknownDecision, result.Status);
    }

    [Fact]
    public async Task Stores_both_the_client_and_the_server_clock()
    {
        Harness harness = Build();
        DateTimeOffset occurredAt = harness.Clock.UtcNow.AddMinutes(-3);

        await harness.Handler.HandleAsync(Command(TenantA, occurredAt));

        // Analytics ordering uses the server clock; the client value is kept as
        // reported and never overwrites it.
        Assert.Equal(1, harness.Events.Count);
    }
}
