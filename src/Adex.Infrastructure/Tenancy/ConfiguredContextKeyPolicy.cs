using Adex.Application.Abstractions;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;
using Microsoft.Extensions.Options;

namespace Adex.Infrastructure.Tenancy;

/// <summary>DEVELOPMENT ONLY context allow-list backed by configuration.</summary>
public sealed class ConfiguredContextKeyPolicy : IContextKeyPolicy
{
    public static readonly IReadOnlySet<string> BaseKeys = new HashSet<string>(
        [
            "device_class",
            "referrer_group",
            "locale",
            "page_group",
            "session_ordinal",
        ],
        StringComparer.Ordinal);

    private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _keysByTenant;

    public ConfiguredContextKeyPolicy(IOptions<TenancyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var configured = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        foreach ((string tenant, string[] keys) in options.Value.DevelopmentAdditionalContextKeys)
        {
            if (!TenantId.TryParse(tenant, out _))
            {
                throw new InvalidOperationException(
                    $"{TenancyOptions.SectionName}:DevelopmentAdditionalContextKeys contains "
                    + $"invalid tenant identifier '{tenant}'.");
            }

            var allowed = new HashSet<string>(BaseKeys, StringComparer.Ordinal);
            foreach (string key in keys)
            {
                if (!DecisionContext.IsValidKey(key))
                {
                    throw new InvalidOperationException(
                        $"{TenancyOptions.SectionName}:DevelopmentAdditionalContextKeys for "
                        + $"'{tenant}' contains invalid key '{key}'.");
                }

                _ = allowed.Add(key);
            }

            configured[tenant] = allowed;
        }

        _keysByTenant = configured;
    }

    public Task<IReadOnlySet<string>> GetAllowedKeysAsync(
        TenantId tenant,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _keysByTenant.TryGetValue(tenant.Value, out IReadOnlySet<string>? keys)
                ? keys
                : BaseKeys);
    }
}
