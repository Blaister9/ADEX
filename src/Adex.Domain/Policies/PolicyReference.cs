namespace Adex.Domain.Policies;

/// <summary>
/// The policy and configuration version that produced a decision. A
/// configuration change mints a new version; learned state never crosses
/// versions (ADR-0009).
/// </summary>
public readonly record struct PolicyReference(string Key, int Version)
{
    public static PolicyReference Create(string key, int version)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Policy key is required.", nameof(key));
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Policy version starts at 1.");
        }

        return new PolicyReference(key, version);
    }

    public override string ToString() => $"{Key}@{Version}";
}
