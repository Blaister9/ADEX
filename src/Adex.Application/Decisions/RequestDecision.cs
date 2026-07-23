using Adex.Application.Abstractions;
using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.Application.Decisions;

public sealed record RequestDecisionCommand(
    TenantId Tenant,
    PlacementKey Placement,
    IReadOnlyList<AlternativeKey> EligibleAlternatives,
    DecisionContext Context,
    SubjectId? Subject = null,
    string? CorrelationId = null);

public enum RequestDecisionStatus
{
    Decided,
    NoEligibleAlternatives,
}

public sealed record RequestDecisionResult(RequestDecisionStatus Status, DecisionRecord? Decision)
{
    public static RequestDecisionResult Decided(DecisionRecord decision) =>
        new(RequestDecisionStatus.Decided, decision);

    public static RequestDecisionResult NoEligibleAlternatives() =>
        new(RequestDecisionStatus.NoEligibleAlternatives, null);
}

/// <summary>
/// The online decision path.
///
/// Order matters and is asserted by tests: the audit record is written
/// <em>before</em> the caller is answered. An unlogged decision is worse than a
/// failed one, because rewards would later attach to nothing
/// (docs/architecture/decision-lifecycle.md).
/// </summary>
public sealed class RequestDecisionHandler(
    IPolicyResolver policyResolver,
    IDecisionStore decisionStore,
    IIdentifierGenerator identifiers,
    ISeedSaltProvider saltProvider,
    IClock clock)
{
    public async Task<RequestDecisionResult> HandleAsync(
        RequestDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Constraint evaluation belongs here, between the request and the policy.
        // The foundation has no constraint store yet, so the eligible set passes
        // through unchanged and both lists are still recorded separately — the
        // shape the audit trail needs once constraints exist (roadmap task 004).
        IReadOnlyList<AlternativeKey> eligible = command.EligibleAlternatives;
        if (eligible.Count == 0)
        {
            return RequestDecisionResult.NoEligibleAlternatives();
        }

        IDecisionPolicy policy = await policyResolver
            .ResolveAsync(command.Tenant, command.Placement, cancellationToken)
            .ConfigureAwait(false);

        DecisionId decisionId = identifiers.NewDecisionId();

        // With no subject the seed falls back to the decision id, which makes the
        // draw unpredictable but still reproducible from the stored record.
        string seedSubject = command.Subject?.Value ?? decisionId.Value;
        uint seed = DecisionSeed.Derive(
            saltProvider.GetSalt(),
            command.Tenant,
            command.Placement,
            policy.Reference,
            seedSubject);

        PolicySelection selection = policy.Select(new PolicySelectionInput(eligible, command.Context, seed));

        var record = new DecisionRecord
        {
            Id = decisionId,
            Tenant = command.Tenant,
            Placement = command.Placement,
            Subject = command.Subject,
            Policy = policy.Reference,
            RequestedAlternatives = command.EligibleAlternatives,
            EligibleAlternatives = eligible,
            Selected = selection.Selected,
            Propensity = selection.Propensity,
            Seed = seed,
            Context = command.Context,
            Explanation = selection.Explanation,
            DecidedAt = clock.UtcNow,
            CorrelationId = command.CorrelationId,
        };

        await decisionStore.AppendAsync(record, cancellationToken).ConfigureAwait(false);

        return RequestDecisionResult.Decided(record);
    }
}
