namespace Adex.Domain.Identifiers;

/// <summary>
/// Identifies one isolated customer of ADEX. Every entity, query and credential
/// is scoped to exactly one tenant (ADR-0007). Strongly typed so a tenant can
/// never be passed where a placement or subject was expected.
/// </summary>
public readonly record struct TenantId
{
    public const string Prefix = "ten_";

    private TenantId(string value) => Value = value;

    public string Value { get; }

    public static TenantId Parse(string? value) =>
        new(PrefixedIdentifier.Require(value, Prefix, nameof(value)));

    public static bool TryParse(string? value, out TenantId tenantId)
    {
        if (PrefixedIdentifier.IsValid(value, Prefix))
        {
            tenantId = new TenantId(value!);
            return true;
        }

        tenantId = default;
        return false;
    }

    public override string ToString() => Value;
}
