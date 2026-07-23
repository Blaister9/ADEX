using Adex.Application.Abstractions;
using Adex.Domain.Identifiers;
using Microsoft.Extensions.Options;

namespace Adex.Infrastructure.Tenancy;

/// <summary>
/// DEVELOPMENT ONLY tenant directory backed by configuration.
///
/// What it deliberately does not do, and what roadmap task 004 must add:
/// hashed key storage, per-key origin allow-lists, rotation, revocation, rate
/// limits, and a distinction between publishable and secret keys. Until then the
/// API only accepts keys listed in configuration, and refuses to start with any
/// configured outside the Development environment.
/// </summary>
public sealed class ConfiguredTenantDirectory : ITenantDirectory
{
    private readonly IReadOnlyDictionary<string, TenantId> _tenantsByKey;

    public ConfiguredTenantDirectory(IOptions<TenancyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var map = new Dictionary<string, TenantId>(StringComparer.Ordinal);
        foreach ((string apiKey, string tenant) in options.Value.DevelopmentApiKeys)
        {
            if (!TenantId.TryParse(tenant, out TenantId tenantId))
            {
                throw new InvalidOperationException(
                    $"{TenancyOptions.SectionName}:DevelopmentApiKeys contains '{tenant}', "
                    + "which is not a valid tenant identifier.");
            }

            map[apiKey] = tenantId;
        }

        _tenantsByKey = map;
    }

    public int Count => _tenantsByKey.Count;

    public Task<TenantId?> ResolveByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrEmpty(apiKey) && _tenantsByKey.TryGetValue(apiKey, out TenantId tenant))
        {
            return Task.FromResult<TenantId?>(tenant);
        }

        // No fallback tenant, ever. An unresolved key is an authentication failure.
        return Task.FromResult<TenantId?>(null);
    }
}
