namespace Adex.Domain.Policies;

/// <summary>
/// Stage 2 of the policy sequence (ADR-0009): pick uniformly from the eligible
/// set, using the injected seed so the assignment is stable for a subject and
/// reproducible from the stored decision.
///
/// This is deliberately the only policy that ships with the foundation. Its
/// purpose is to prove the reward pipeline — impressions, deduplication,
/// attribution — with an assignment whose analysis is textbook. Thompson
/// Sampling arrives only after that pipeline is verified.
/// </summary>
public sealed class UniformRandomPolicy : IDecisionPolicy
{
    public const string PolicyKey = "uniform-random";

    public UniformRandomPolicy(int version = 1) => Reference = PolicyReference.Create(PolicyKey, version);

    public PolicyReference Reference { get; }

    public PolicySelection Select(PolicySelectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        int count = input.Eligible.Count;
        int index = (int)(input.Seed % (uint)count);

        return new PolicySelection(
            input.Eligible[index],
            1d / count,
            $"Uniform selection {index + 1} of {count} from the eligible set.");
    }
}
