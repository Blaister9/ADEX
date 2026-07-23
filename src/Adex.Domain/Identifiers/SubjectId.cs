namespace Adex.Domain.Identifiers;

/// <summary>
/// Anonymous, first-party, rotatable, tenant-scoped identifier for the entity a
/// decision is about. It is not a person and not a user account: it is never
/// derived from device properties and carries no personal data (ADR-0008).
/// Optional throughout the system — a decision without a subject is valid.
/// </summary>
public readonly record struct SubjectId
{
    public const string Prefix = "anon_";

    private SubjectId(string value) => Value = value;

    public string Value { get; }

    public static SubjectId Parse(string? value) =>
        new(PrefixedIdentifier.Require(value, Prefix, nameof(value)));

    public static bool TryParse(string? value, out SubjectId subjectId)
    {
        if (PrefixedIdentifier.IsValid(value, Prefix))
        {
            subjectId = new SubjectId(value!);
            return true;
        }

        subjectId = default;
        return false;
    }

    public override string ToString() => Value;
}
