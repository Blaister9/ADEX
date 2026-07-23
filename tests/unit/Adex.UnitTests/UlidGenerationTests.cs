using Adex.Domain.Identifiers;
using Adex.Infrastructure.Identifiers;

namespace Adex.UnitTests;

public sealed class UlidGenerationTests
{
    private static readonly DateTimeOffset Timestamp =
        DateTimeOffset.FromUnixTimeMilliseconds(1_774_180_991_482);

    [Fact]
    public void Produces_a_valid_ulid_body()
    {
        Assert.True(CrockfordBase32.IsUlid(UlidIdentifierGenerator.NewUlid()));
    }

    [Fact]
    public void Is_deterministic_when_the_entropy_is_supplied()
    {
        byte[] randomness = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        Assert.Equal(
            UlidIdentifierGenerator.NewUlidForTesting(Timestamp, randomness),
            UlidIdentifierGenerator.NewUlidForTesting(Timestamp, randomness));
    }

    [Fact]
    public void Sorts_lexicographically_by_time()
    {
        byte[] randomness = new byte[10];
        string earlier = UlidIdentifierGenerator.NewUlidForTesting(Timestamp, randomness);
        string later = UlidIdentifierGenerator.NewUlidForTesting(Timestamp.AddSeconds(1), randomness);

        Assert.True(string.CompareOrdinal(earlier, later) < 0);
    }

    [Fact]
    public void Encodes_the_timestamp_in_the_leading_characters()
    {
        // Same millisecond, different entropy: the time prefix must be identical,
        // which is what makes the identifier index-friendly.
        string first = UlidIdentifierGenerator.NewUlidForTesting(Timestamp, new byte[10]);
        string second = UlidIdentifierGenerator.NewUlidForTesting(Timestamp, [.. Enumerable.Repeat((byte)255, 10)]);

        Assert.Equal(first[..10], second[..10]);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Rejects_the_wrong_amount_of_entropy()
    {
        Assert.Throws<ArgumentException>(() =>
            UlidIdentifierGenerator.NewUlidForTesting(Timestamp, new byte[9]));
    }

    [Fact]
    public void Generates_unique_decision_identifiers()
    {
        var generator = new UlidIdentifierGenerator();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < 5_000; index++)
        {
            Assert.True(seen.Add(generator.NewDecisionId().Value));
        }
    }
}
