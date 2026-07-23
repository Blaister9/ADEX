using Adex.Domain.Alternatives;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;

namespace Adex.UnitTests;

public sealed class IdentifierTests
{
    private const string ValidUlid = "01JQZ8K3N4P5R6S7T8V9W0X1Y2";

    [Fact]
    public void Accepts_a_well_formed_identifier()
    {
        Assert.True(TenantId.TryParse("ten_" + ValidUlid, out TenantId tenant));
        Assert.Equal("ten_" + ValidUlid, tenant.Value);
    }

    [Theory]
    [InlineData("dec_" + ValidUlid)]   // right shape, wrong prefix for a tenant
    [InlineData("ten_" + "0123")]      // too short
    [InlineData("ten_01JQZ8K3N4P5R6S7T8V9W0X1YI")] // contains an ambiguous character
    [InlineData("ten_81JQZ8K3N4P5R6S7T8V9W0X1Y2")] // timestamp overflows 48 bits
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_anything_else(string? value)
    {
        Assert.False(TenantId.TryParse(value, out _));
        Assert.Throws<ArgumentException>(() => TenantId.Parse(value));
    }

    [Fact]
    public void Rejects_the_characters_that_get_mistranscribed()
    {
        // I, L, O and U are absent from Crockford base32 precisely so that a
        // support ticket cannot turn one identifier into another.
        foreach (char ambiguous in "ILOU")
        {
            Assert.DoesNotContain(ambiguous, CrockfordBase32.Alphabet);
        }
    }

    [Fact]
    public void Distinguishes_identifier_kinds_by_prefix()
    {
        Assert.False(DecisionId.TryParse("evt_" + ValidUlid, out _));
        Assert.False(EventId.TryParse("dec_" + ValidUlid, out _));
        Assert.False(SubjectId.TryParse("ten_" + ValidUlid, out _));
    }

    [Theory]
    [InlineData("homepage.primary-cta", true)]
    [InlineData("catalog.recommendation-slot", true)]
    [InlineData("a", true)]
    [InlineData("Homepage.Primary", false)]  // upper case
    [InlineData("-leading-dash", false)]
    [InlineData("trailing-dash-", false)]
    [InlineData("has space", false)]
    [InlineData("", false)]
    public void Placement_keys_follow_the_published_grammar(string value, bool expected)
    {
        Assert.Equal(expected, PlacementKey.IsValid(value));
        Assert.Equal(expected, AlternativeKey.IsValid(value));
    }

    [Fact]
    public void Keys_are_length_limited_at_the_boundary()
    {
        Assert.True(PlacementKey.IsValid(new string('a', PlacementKey.MaxLength)));
        Assert.False(PlacementKey.IsValid(new string('a', PlacementKey.MaxLength + 1)));
    }
}
