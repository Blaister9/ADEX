using Adex.Domain.Identifiers;

namespace Adex.Application.Abstractions;

/// <summary>
/// Maps a presented API key to exactly one tenant. Failure to resolve is an
/// authentication failure, never a fallback to a default tenant.
/// </summary>
public interface ITenantDirectory
{
    Task<TenantId?> ResolveByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
