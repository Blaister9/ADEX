namespace Adex.Domain.Events;

/// <summary>
/// The kind of behavioural signal. Four names are reserved with a fixed meaning
/// across every tenant; anything else is a tenant-defined type that must be
/// declared in tenant configuration. The reserved set is deliberately generic —
/// no vertical's vocabulary belongs here.
/// </summary>
public readonly record struct EventType
{
    public const int MaxLength = 64;

    public static readonly EventType Impression = new("impression");
    public static readonly EventType Click = new("click");
    public static readonly EventType Lead = new("lead");
    public static readonly EventType Purchase = new("purchase");

    private EventType(string value) => Value = value;

    public string Value { get; }

    public bool IsReserved =>
        Value is "impression" or "click" or "lead" or "purchase";

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        if (value[0] is < 'a' or > 'z')
        {
            return false;
        }

        foreach (char character in value)
        {
            bool allowed =
                character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character is '_' or '.' or '-';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    public static EventType Parse(string? value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Value is not a valid event type.", nameof(value));
        }

        return new EventType(value!);
    }

    public static bool TryParse(string? value, out EventType type)
    {
        if (IsValid(value))
        {
            type = new EventType(value!);
            return true;
        }

        type = default;
        return false;
    }

    public override string ToString() => Value;
}
