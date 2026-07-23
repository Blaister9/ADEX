using System.Collections.Concurrent;
using Adex.Application.Abstractions;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;

namespace Adex.Infrastructure.Persistence.InMemory;

/// <summary>
/// DEVELOPMENT ONLY. Process-local decision storage.
///
/// This is not a system of record: nothing survives a restart, nothing is shared
/// between instances, and there is no Row Level Security backstop. It exists so
/// the foundation has a runnable, testable decision path while the PostgreSQL
/// adapter is designed together with the schema (roadmap task 004).
///
/// Tenant scoping is still enforced here, because a store that ignored the
/// tenant would let a test pass that the real adapter must fail.
/// </summary>
public sealed class InMemoryDecisionStore : IDecisionStore
{
    private readonly ConcurrentDictionary<(string Tenant, string Decision), DecisionRecord> _decisions = new();

    public int Count => _decisions.Count;

    public Task AppendAsync(DecisionRecord decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();

        // Decisions are immutable: a repeated identifier is a defect, not an update.
        if (!_decisions.TryAdd((decision.Tenant.Value, decision.Id.Value), decision))
        {
            throw new InvalidOperationException($"Decision {decision.Id} already exists for this tenant.");
        }

        return Task.CompletedTask;
    }

    public Task<DecisionRecord?> FindAsync(
        TenantId tenant,
        DecisionId decisionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _decisions.TryGetValue((tenant.Value, decisionId.Value), out DecisionRecord? decision);
        return Task.FromResult(decision);
    }
}
