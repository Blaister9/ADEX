using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;

namespace Adex.Application.Abstractions;

/// <summary>
/// Durable home of the decision audit trail. Every method takes the tenant
/// explicitly: there is no ambient "current tenant" that a background task could
/// inherit incorrectly (ADR-0007).
/// </summary>
public interface IDecisionStore
{
    /// <summary>Appends an immutable decision record. Decisions are never updated.</summary>
    Task AppendAsync(DecisionRecord decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one decision. Returns null when it does not exist <em>for this
    /// tenant</em>; a decision belonging to another tenant is indistinguishable
    /// from one that does not exist.
    /// </summary>
    Task<DecisionRecord?> FindAsync(
        TenantId tenant,
        DecisionId decisionId,
        CancellationToken cancellationToken = default);
}
