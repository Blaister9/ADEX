namespace Adex.Domain.Policies;

/// <summary>
/// A selection strategy. Implementations must be pure: same input, same output,
/// no I/O, no clock, no ambient randomness. The policy sequence and the evidence
/// that gates each stage are defined in ADR-0009.
/// </summary>
public interface IDecisionPolicy
{
    /// <summary>Identity and configuration version of this policy.</summary>
    PolicyReference Reference { get; }

    /// <summary>Chooses one alternative from the eligible set.</summary>
    PolicySelection Select(PolicySelectionInput input);
}
