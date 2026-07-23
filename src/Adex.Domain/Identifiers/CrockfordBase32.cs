namespace Adex.Domain.Identifiers;

/// <summary>
/// Crockford base32 alphabet used by ADEX public identifiers (ULIDs).
/// The letters I, L, O and U are excluded so an identifier read aloud or copied
/// from a support ticket cannot be transcribed into a different one.
/// </summary>
public static class CrockfordBase32
{
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Length of the encoded 128-bit ULID body.</summary>
    public const int UlidLength = 26;

    /// <summary>
    /// True when <paramref name="value"/> is a 26-character ULID body. The first
    /// character is limited to 0-7 because the 48-bit timestamp cannot overflow
    /// into the high bits.
    /// </summary>
    public static bool IsUlid(ReadOnlySpan<char> value)
    {
        if (value.Length != UlidLength)
        {
            return false;
        }

        if (value[0] is < '0' or > '7')
        {
            return false;
        }

        foreach (char character in value)
        {
            if (Alphabet.IndexOf(character) < 0)
            {
                return false;
            }
        }

        return true;
    }
}
