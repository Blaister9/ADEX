using Adex.Domain.Events;

namespace Adex.Application.Abstractions;

/// <summary>
/// Durable home of ingested events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends an event, or reports that this tenant already stored one with the
    /// same <c>event_id</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the event was stored; <c>false</c> when it was a replay.
    /// Implementations must derive this from a uniqueness constraint on
    /// <c>(tenant_id, event_id)</c> rather than from a prior read, so two
    /// concurrent deliveries cannot both succeed (ADR-0012).
    /// </returns>
    Task<bool> TryAppendAsync(BehaviouralEvent behaviouralEvent, CancellationToken cancellationToken = default);
}
