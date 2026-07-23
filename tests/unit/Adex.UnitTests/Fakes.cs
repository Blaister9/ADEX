using System.Globalization;
using System.Text;
using Adex.Application.Abstractions;
using Adex.Domain.Identifiers;
using Adex.Infrastructure.Identifiers;

namespace Adex.UnitTests;

/// <summary>A clock the test owns, so time-dependent behaviour is asserted, not waited for.</summary>
internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public static FixedClock At(string iso) =>
        new(DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
}

/// <summary>Real ULID generation; only the entropy source is fixed.</summary>
internal sealed class SequentialIdentifierGenerator(DateTimeOffset timestamp) : IIdentifierGenerator
{
    private int _counter;

    public DecisionId NewDecisionId()
    {
        _counter++;
        Span<byte> randomness = stackalloc byte[10];
        randomness[9] = (byte)_counter;
        randomness[8] = (byte)(_counter >> 8);
        return DecisionId.Parse(
            DecisionId.Prefix + UlidIdentifierGenerator.NewUlidForTesting(timestamp, randomness));
    }
}

internal sealed class StaticSaltProvider(string salt) : ISeedSaltProvider
{
    private readonly byte[] _salt = Encoding.UTF8.GetBytes(salt);

    public byte[] GetSalt() => _salt;
}
