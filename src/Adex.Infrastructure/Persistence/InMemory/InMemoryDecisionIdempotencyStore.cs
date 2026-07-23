using System.Collections.Concurrent;
using Adex.Application.Abstractions;
using Adex.Application.Decisions;
using Adex.Domain.Identifiers;

namespace Adex.Infrastructure.Persistence.InMemory;

/// <summary>
/// DEVELOPMENT ONLY process-local decision idempotency window.
/// </summary>
public sealed class InMemoryDecisionIdempotencyStore : IDecisionIdempotencyStore
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<(string Tenant, string Key), Entry> _entries = new();
    private readonly ConcurrentDictionary<(string Tenant, string Key), SemaphoreSlim> _gates = new();

    public async Task<IdempotentDecisionResult> ExecuteAsync(
        TenantId tenant,
        string key,
        string requestFingerprint,
        DateTimeOffset now,
        Func<CancellationToken, Task<RequestDecisionResult>> execute,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);
        ArgumentNullException.ThrowIfNull(execute);

        foreach ((var expiredScope, Entry entry) in _entries)
        {
            if (entry.ExpiresAt <= now)
            {
                _ = _entries.TryRemove(
                    new KeyValuePair<(string Tenant, string Key), Entry>(expiredScope, entry));
            }
        }

        (string Tenant, string Key) scope = (tenant.Value, key);
        SemaphoreSlim gate = _gates.GetOrAdd(scope, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_entries.TryGetValue(scope, out Entry? existing) && existing.ExpiresAt > now)
            {
                return string.Equals(
                    existing.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal)
                    ? new IdempotentDecisionResult(IdempotentDecisionStatus.Replayed, existing.Result)
                    : new IdempotentDecisionResult(IdempotentDecisionStatus.Conflict, null);
            }

            _ = _entries.TryRemove(scope, out _);
            RequestDecisionResult result = await execute(cancellationToken).ConfigureAwait(false);
            _entries[scope] = new Entry(requestFingerprint, result, now + Retention);
            return new IdempotentDecisionResult(IdempotentDecisionStatus.Executed, result);
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record Entry(
        string RequestFingerprint,
        RequestDecisionResult Result,
        DateTimeOffset ExpiresAt);
}
