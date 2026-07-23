namespace Adex.Domain.Placements;

/// <summary>
/// Tenant-unique, human-authored name of a decision point, for example
/// <c>homepage.primary-cta</c>. Grammar is fixed by the public contract:
/// <c>^[a-z0-9]([a-z0-9._-]{0,62}[a-z0-9])?$</c>.
/// </summary>
public readonly record struct PlacementKey
{
    public const int MaxLength = 64;

    private PlacementKey(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? value) => KeyGrammar.IsValid(value, MaxLength);

    public static PlacementKey Parse(string? value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Value is not a valid placement key.", nameof(value));
        }

        return new PlacementKey(value!);
    }

    public static bool TryParse(string? value, out PlacementKey key)
    {
        if (IsValid(value))
        {
            key = new PlacementKey(value!);
            return true;
        }

        key = default;
        return false;
    }

    public override string ToString() => Value;
}
