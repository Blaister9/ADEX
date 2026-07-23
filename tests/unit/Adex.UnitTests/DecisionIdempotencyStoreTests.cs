using Adex.Application.Abstractions;
using Adex.Application.Decisions;
using Adex.Domain.Identifiers;
using Adex.Infrastructure.Persistence.InMemory;

namespace Adex.UnitTests;

public sealed class DecisionIdempotencyStoreTests
{
    private static readonly TenantId Tenant =
        TenantId.Parse("ten_01JQZ6A1B2C3D4E5F6G7H8J9K0");

    [Fact]
    public async Task Expires_a_key_after_the_documented_24_hour_window()
    {
        var store = new InMemoryDecisionIdempotencyStore();
        DateTimeOffset now = DateTimeOffset.Parse("2026-07-22T14:00:00Z");
        int executions = 0;

        Task<RequestDecisionResult> Execute(CancellationToken _)
        {
            executions++;
            return Task.FromResult(RequestDecisionResult.NoEligibleAlternatives());
        }

        IdempotentDecisionResult first = await store.ExecuteAsync(
            Tenant, "retry-key", "fingerprint", now, Execute);
        IdempotentDecisionResult replay = await store.ExecuteAsync(
            Tenant, "retry-key", "fingerprint", now.AddHours(23), Execute);
        IdempotentDecisionResult expired = await store.ExecuteAsync(
            Tenant, "retry-key", "fingerprint", now.AddHours(24), Execute);

        Assert.Equal(IdempotentDecisionStatus.Executed, first.Status);
        Assert.Equal(IdempotentDecisionStatus.Replayed, replay.Status);
        Assert.Equal(IdempotentDecisionStatus.Executed, expired.Status);
        Assert.Equal(2, executions);
    }
}
