namespace Adex.Domain.Identifiers;

/// <summary>
/// Identifies one behavioural event. Client generated on purpose: it is the
/// deduplication key, and a client that cannot choose the key cannot retry
/// safely (ADR-0012).
/// </summary>
public readonly record struct EventId
{
    public const string Prefix = "evt_";

    private EventId(string value) => Value = value;

    public string Value { get; }

    public static EventId Parse(string? value) =>
        new(PrefixedIdentifier.Require(value, Prefix, nameof(value)));

    public static bool TryParse(string? value, out EventId eventId)
    {
        if (PrefixedIdentifier.IsValid(value, Prefix))
        {
            eventId = new EventId(value!);
            return true;
        }

        eventId = default;
        return false;
    }

    public override string ToString() => Value;
}
