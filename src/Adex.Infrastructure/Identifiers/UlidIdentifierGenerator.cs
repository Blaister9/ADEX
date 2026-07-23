using System.Security.Cryptography;
using Adex.Application.Abstractions;
using Adex.Domain.Identifiers;

namespace Adex.Infrastructure.Identifiers;

/// <summary>
/// Generates ULIDs: a 48-bit millisecond timestamp followed by 80 bits from the
/// system CSPRNG, rendered as 26 Crockford base32 characters.
///
/// Time-ordered identifiers keep an index on the decision table append-friendly,
/// and the encoding avoids the characters that get mistranscribed in a support
/// ticket. The value carries no device or user information (ADR-0008).
///
/// Monotonicity within a single millisecond is not guaranteed; nothing in ADEX
/// depends on ordering finer than the millisecond.
/// </summary>
public sealed class UlidIdentifierGenerator : IIdentifierGenerator
{
    public DecisionId NewDecisionId() => DecisionId.Parse(DecisionId.Prefix + NewUlid());

    public static string NewUlid() => NewUlid(DateTimeOffset.UtcNow);

    public static string NewUlid(DateTimeOffset timestamp)
    {
        Span<byte> randomness = stackalloc byte[10];
        RandomNumberGenerator.Fill(randomness);
        return NewUlidForTesting(timestamp, randomness);
    }

    /// <summary>
    /// Deterministic overload: the entropy is supplied rather than drawn, so a
    /// test can assert the encoding itself instead of only its shape.
    /// </summary>
    public static string NewUlidForTesting(DateTimeOffset timestamp, ReadOnlySpan<byte> randomness)
    {
        if (randomness.Length != 10)
        {
            throw new ArgumentException("A ULID needs exactly 80 bits of randomness.", nameof(randomness));
        }

        long milliseconds = timestamp.ToUnixTimeMilliseconds();
        if (milliseconds < 0 || milliseconds > 0xFFFF_FFFF_FFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timestamp),
                timestamp,
                "Timestamp is outside the 48-bit ULID range.");
        }

        UInt128 value = (UInt128)(ulong)milliseconds << 80;
        for (int index = 0; index < randomness.Length; index++)
        {
            value |= (UInt128)randomness[index] << (8 * (randomness.Length - 1 - index));
        }

        Span<char> characters = stackalloc char[CrockfordBase32.UlidLength];
        for (int index = CrockfordBase32.UlidLength - 1; index >= 0; index--)
        {
            characters[index] = CrockfordBase32.Alphabet[(int)(value & 31)];
            value >>= 5;
        }

        return new string(characters);
    }
}
