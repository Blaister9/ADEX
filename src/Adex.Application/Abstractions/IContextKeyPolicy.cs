using Adex.Domain.Identifiers;

namespace Adex.Application.Abstractions;

/// <summary>
/// Resolves the context keys a tenant is allowed to submit. The foundation
/// adapter reads development configuration; the durable implementation will
/// resolve versioned placement configuration from the system of record.
/// </summary>
public interface IContextKeyPolicy
{
    Task<IReadOnlySet<string>> GetAllowedKeysAsync(
        TenantId tenant,
        CancellationToken cancellationToken = default);
}
