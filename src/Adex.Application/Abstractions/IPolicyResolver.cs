using Adex.Domain.Identifiers;
using Adex.Domain.Placements;
using Adex.Domain.Policies;

namespace Adex.Application.Abstractions;

/// <summary>
/// Resolves which policy governs a placement for a tenant.
///
/// In the foundation there is no configuration store yet, so the only
/// implementation returns <c>uniform-random</c> version 1 for every placement
/// and is named accordingly. Real placement configuration is roadmap task 004.
/// </summary>
public interface IPolicyResolver
{
    Task<IDecisionPolicy> ResolveAsync(
        TenantId tenant,
        PlacementKey placement,
        CancellationToken cancellationToken = default);
}
