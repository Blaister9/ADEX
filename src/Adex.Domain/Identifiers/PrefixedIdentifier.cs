namespace Adex.Domain.Identifiers;

/// <summary>
/// Shared parsing for the opaque public identifiers described in
/// docs/architecture/domain-model.md: a fixed prefix followed by a ULID body.
/// Clients must treat the whole string as opaque; nothing in ADEX parses meaning
/// out of it beyond validating the shape.
/// </summary>
public static class PrefixedIdentifier
{
    public static bool IsValid(string? value, string prefix)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return CrockfordBase32.IsUlid(value.AsSpan(prefix.Length));
    }

    public static string Require(string? value, string prefix, string parameterName)
    {
        if (!IsValid(value, prefix))
        {
            throw new ArgumentException(
                $"Value is not a valid '{prefix}' identifier.",
                parameterName);
        }

        return value!;
    }
}
