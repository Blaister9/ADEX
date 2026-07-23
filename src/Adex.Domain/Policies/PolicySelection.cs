using Adex.Domain.Alternatives;

namespace Adex.Domain.Policies;

/// <summary>
/// The outcome of a policy evaluation.
/// </summary>
/// <param name="Selected">The chosen alternative.</param>
/// <param name="Propensity">
/// Probability with which the acting policy would have chosen
/// <paramref name="Selected"/>. Logged from the first policy onwards because
/// off-policy evaluation cannot be added to unlogged history (ADR-0009).
/// </param>
/// <param name="Explanation">
/// Short, human-readable reason, stored with the decision so an operator can
/// answer "why this alternative" without reading source code.
/// </param>
public readonly record struct PolicySelection(
    AlternativeKey Selected,
    double Propensity,
    string Explanation);
