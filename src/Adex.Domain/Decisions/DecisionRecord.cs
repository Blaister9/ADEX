using Adex.Domain.Alternatives;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.Domain.Decisions;

/// <summary>
/// The immutable audit record of one selection.
///
/// A decision is written once and never updated; a correction is a new record.
/// It carries everything needed to answer "why did this visitor see this?" and
/// to recompute the selection offline — the replay check that gates every
/// policy (docs/architecture/decision-lifecycle.md).
/// </summary>
public sealed record DecisionRecord
{
    public required DecisionId Id { get; init; }

    public required TenantId Tenant { get; init; }

    public required PlacementKey Placement { get; init; }

    public SubjectId? Subject { get; init; }

    public required PolicyReference Policy { get; init; }

    /// <summary>Alternatives the caller declared eligible, in the order received.</summary>
    public required IReadOnlyList<AlternativeKey> RequestedAlternatives { get; init; }

    /// <summary>
    /// Alternatives that survived constraints and were actually offered to the
    /// policy. Stored separately from <see cref="RequestedAlternatives"/> so an
    /// exclusion is visible after the fact instead of inferred.
    /// </summary>
    public required IReadOnlyList<AlternativeKey> EligibleAlternatives { get; init; }

    public required AlternativeKey Selected { get; init; }

    /// <summary>Probability of selecting <see cref="Selected"/> under the acting policy.</summary>
    public required double Propensity { get; init; }

    public required uint Seed { get; init; }

    public required DecisionContext Context { get; init; }

    public required string Explanation { get; init; }

    /// <summary>Always UTC. The API rejects non-UTC input rather than converting it.</summary>
    public required DateTimeOffset DecidedAt { get; init; }

    public string? CorrelationId { get; init; }
}
