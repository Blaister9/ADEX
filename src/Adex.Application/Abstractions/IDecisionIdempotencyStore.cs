using Adex.Application.Decisions;
using Adex.Domain.Identifiers;

namespace Adex.Application.Abstractions;

public enum IdempotentDecisionStatus
{
    Executed,
    Replayed,
    Conflict,
}

public sealed record IdempotentDecisionResult(
    IdempotentDecisionStatus Status,
    RequestDecisionResult? Result);

/// <summary>
/// Serializes decision retries per tenant and idempotency key. The durable
/// adapter must enforce uniqueness on (tenant_id, idempotency_key).
/// </summary>
public interface IDecisionIdempotencyStore
{
    Task<IdempotentDecisionResult> ExecuteAsync(
        TenantId tenant,
        string key,
        string requestFingerprint,
        DateTimeOffset now,
        Func<CancellationToken, Task<RequestDecisionResult>> execute,
        CancellationToken cancellationToken = default);
}
