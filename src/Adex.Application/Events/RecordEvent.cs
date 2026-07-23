using Adex.Application.Abstractions;
using Adex.Domain.Events;
using Adex.Domain.Identifiers;

namespace Adex.Application.Events;

public sealed record RecordEventCommand(
    TenantId Tenant,
    EventId EventId,
    EventType Type,
    DateTimeOffset OccurredAt,
    DecisionId? Decision = null,
    SubjectId? Subject = null,
    IReadOnlyDictionary<string, string?>? Properties = null,
    string? CorrelationId = null);

public enum RecordEventStatus
{
    Accepted,
    Duplicate,
    RejectedClockSkew,
    UnknownDecision,
}

public sealed record RecordEventResult(RecordEventStatus Status, DateTimeOffset ReceivedAt);

/// <summary>
/// Idempotent event ingestion.
///
/// Three rules live here rather than at the transport boundary, because all
/// three are data-correctness rules that any caller of this use case must get:
/// <list type="number">
///   <item>client clocks are untrusted, so an <c>occurred_at</c> far in the
///   future or the distant past is rejected instead of silently corrupting
///   attribution windows;</item>
///   <item>an event may only cite a decision of the same tenant, which is what
///   stops forged events from attaching a reward to someone else's decision
///   (threat T2 in the threat model);</item>
///   <item>a replay is not an error — it returns <see cref="RecordEventStatus.Duplicate"/>
///   and changes no state, so a client can retry freely (ADR-0012).</item>
/// </list>
/// </summary>
public sealed class RecordEventHandler(IEventStore eventStore, IDecisionStore decisionStore, IClock clock)
{
    /// <summary>How far ahead of the server clock a client event may claim to have happened.</summary>
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

    /// <summary>How far in the past a client event may claim to have happened.</summary>
    public static readonly TimeSpan MaxPastSkew = TimeSpan.FromDays(7);

    public async Task<RecordEventResult> HandleAsync(
        RecordEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset receivedAt = clock.UtcNow;

        if (command.OccurredAt > receivedAt + MaxFutureSkew
            || command.OccurredAt < receivedAt - MaxPastSkew)
        {
            return new RecordEventResult(RecordEventStatus.RejectedClockSkew, receivedAt);
        }

        if (command.Decision is { } decisionId)
        {
            Domain.Decisions.DecisionRecord? decision = await decisionStore
                .FindAsync(command.Tenant, decisionId, cancellationToken)
                .ConfigureAwait(false);

            // A decision of another tenant is indistinguishable from one that
            // never existed, so this never leaks cross-tenant existence.
            if (decision is null)
            {
                return new RecordEventResult(RecordEventStatus.UnknownDecision, receivedAt);
            }
        }

        var behaviouralEvent = new BehaviouralEvent
        {
            Id = command.EventId,
            Tenant = command.Tenant,
            Decision = command.Decision,
            Subject = command.Subject,
            Type = command.Type,
            OccurredAt = command.OccurredAt.ToUniversalTime(),
            ReceivedAt = receivedAt,
            Properties = command.Properties ?? new Dictionary<string, string?>(StringComparer.Ordinal),
            CorrelationId = command.CorrelationId,
        };

        bool stored = await eventStore
            .TryAppendAsync(behaviouralEvent, cancellationToken)
            .ConfigureAwait(false);

        return new RecordEventResult(
            stored ? RecordEventStatus.Accepted : RecordEventStatus.Duplicate,
            receivedAt);
    }
}
