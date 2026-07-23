using Adex.Domain.Identifiers;

namespace Adex.Application.Abstractions;

/// <summary>
/// Generates the identifiers ADEX owns. Event identifiers are deliberately not
/// here: they are client-generated, which is what makes a client retry safe
/// (ADR-0012).
/// </summary>
public interface IIdentifierGenerator
{
    DecisionId NewDecisionId();
}
