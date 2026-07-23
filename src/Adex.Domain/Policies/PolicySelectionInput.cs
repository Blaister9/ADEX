using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;

namespace Adex.Domain.Policies;

/// <summary>
/// Everything a policy is allowed to see. There is nothing else: no clock, no
/// ambient randomness, no I/O. That is what makes a decision replayable from its
/// stored inputs (docs/architecture/decision-lifecycle.md).
/// </summary>
/// <param name="Eligible">
/// Alternatives remaining after constraints were applied. Never empty — an empty
/// set is rejected before a policy is ever called.
/// </param>
/// <param name="Context">Allow-listed, non-identifying signals.</param>
/// <param name="Seed">
/// Deterministic draw derived from the tenant, placement, policy version and
/// subject. Passed in rather than generated so the same inputs reproduce the
/// same selection.
/// </param>
public sealed record PolicySelectionInput(
    IReadOnlyList<AlternativeKey> Eligible,
    DecisionContext Context,
    uint Seed)
{
    public IReadOnlyList<AlternativeKey> Eligible { get; } =
        Eligible.Count > 0
            ? Eligible
            : throw new ArgumentException("A policy is never called with an empty eligible set.", nameof(Eligible));
}
