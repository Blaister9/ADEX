namespace Adex.Domain.Identifiers;

/// <summary>
/// Identifies one immutable decision record. Server generated, because a client
/// must never be able to choose the key of an audit row.
/// </summary>
public readonly record struct DecisionId
{
    public const string Prefix = "dec_";

    private DecisionId(string value) => Value = value;

    public string Value { get; }

    public static DecisionId Parse(string? value) =>
        new(PrefixedIdentifier.Require(value, Prefix, nameof(value)));

    public static bool TryParse(string? value, out DecisionId decisionId)
    {
        if (PrefixedIdentifier.IsValid(value, Prefix))
        {
            decisionId = new DecisionId(value!);
            return true;
        }

        decisionId = default;
        return false;
    }

    public override string ToString() => Value;
}
