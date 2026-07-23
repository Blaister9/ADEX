using Adex.Domain.Identifiers;

namespace Adex.Domain.Events;

/// <summary>
/// A behavioural signal reported after a decision, or independently of any
/// decision. Append-only: a correction is a new record, never an update.
/// </summary>
public sealed record BehaviouralEvent
{
    /// <summary>
    /// Client-generated. Together with the tenant this is the deduplication key,
    /// enforced by a database constraint rather than by application logic
    /// (ADR-0012).
    /// </summary>
    public required EventId Id { get; init; }

    public required TenantId Tenant { get; init; }

    /// <summary>
    /// The decision this event is a consequence of, when there is one. Site-wide
    /// signals that no placement claimed are valid with no decision.
    /// </summary>
    public DecisionId? Decision { get; init; }

    public SubjectId? Subject { get; init; }

    public required EventType Type { get; init; }

    /// <summary>Client clock. Untrusted: stored as reported, never used in place of <see cref="ReceivedAt"/>.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Server clock. Ordering, windows and analytics use this by default.</summary>
    public required DateTimeOffset ReceivedAt { get; init; }

    public required IReadOnlyDictionary<string, string?> Properties { get; init; }

    public string? CorrelationId { get; init; }
}
