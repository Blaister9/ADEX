using System.Collections.Concurrent;
using Adex.Application.Abstractions;
using Adex.Domain.Events;

namespace Adex.Infrastructure.Persistence.InMemory;

/// <summary>
/// DEVELOPMENT ONLY. Process-local event storage.
///
/// Not a system of record — see <see cref="InMemoryDecisionStore"/>. Uniqueness
/// on <c>(tenant, event_id)</c> is modelled with an atomic
/// <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> so that the
/// idempotency semantics the PostgreSQL adapter will enforce with a unique
/// constraint are already exercised by tests (ADR-0012).
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<(string Tenant, string Event), BehaviouralEvent> _events = new();

    public int Count => _events.Count;

    public Task<bool> TryAppendAsync(
        BehaviouralEvent behaviouralEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(behaviouralEvent);
        cancellationToken.ThrowIfCancellationRequested();

        bool stored = _events.TryAdd(
            (behaviouralEvent.Tenant.Value, behaviouralEvent.Id.Value),
            behaviouralEvent);

        return Task.FromResult(stored);
    }
}
