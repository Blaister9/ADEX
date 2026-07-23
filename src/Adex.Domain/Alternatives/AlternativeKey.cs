using Adex.Domain.Placements;

namespace Adex.Domain.Alternatives;

/// <summary>
/// Tenant-unique key of one option that may be selected at a placement. The core
/// never interprets what an alternative means — that is tenant configuration,
/// which is what keeps ADEX free of any vertical's vocabulary.
/// </summary>
public readonly record struct AlternativeKey
{
    public const int MaxLength = 64;

    private AlternativeKey(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? value) => KeyGrammar.IsValid(value, MaxLength);

    public static AlternativeKey Parse(string? value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Value is not a valid alternative key.", nameof(value));
        }

        return new AlternativeKey(value!);
    }

    public static bool TryParse(string? value, out AlternativeKey key)
    {
        if (IsValid(value))
        {
            key = new AlternativeKey(value!);
            return true;
        }

        key = default;
        return false;
    }

    public override string ToString() => Value;
}
