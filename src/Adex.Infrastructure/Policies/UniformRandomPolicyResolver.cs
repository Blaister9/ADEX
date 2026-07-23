using Adex.Application.Abstractions;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.Infrastructure.Policies;

/// <summary>
/// FOUNDATION ONLY. Resolves every placement of every tenant to
/// <c>uniform-random</c> version 1.
///
/// There is no placement configuration store yet, so there is nothing to look
/// up. This is named for exactly what it does rather than dressed up as a
/// general resolver: the configuration-backed implementation arrives with
/// roadmap task 004, and until then no tenant can select a different policy.
/// </summary>
public sealed class UniformRandomPolicyResolver : IPolicyResolver
{
    private static readonly UniformRandomPolicy Policy = new(version: 1);

    public Task<IDecisionPolicy> ResolveAsync(
        TenantId tenant,
        PlacementKey placement,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDecisionPolicy>(Policy);
    }
}
