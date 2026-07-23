using System.Collections;

namespace Adex.Domain.Decisions;

/// <summary>
/// Allow-listed, non-identifying signals describing the situation of a decision.
/// Immutable, because it is stored verbatim with the decision and must still
/// describe the inputs when the decision is replayed years later.
///
/// The core does not interpret any particular key. Meaning is tenant
/// configuration — that is what keeps the engine free of a vertical's
/// vocabulary.
/// </summary>
public sealed class DecisionContext : IReadOnlyDictionary<string, string?>
{
    public const int MaxKeys = 32;
    public const int MaxValueLength = 256;

    private readonly IReadOnlyDictionary<string, string?> _values;

    private DecisionContext(IReadOnlyDictionary<string, string?> values) => _values = values;

    public static DecisionContext Empty { get; } =
        new(new Dictionary<string, string?>(StringComparer.Ordinal));

    public static DecisionContext Create(IEnumerable<KeyValuePair<string, string?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copy = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach ((string key, string? value) in values)
        {
            if (!IsValidKey(key))
            {
                throw new ArgumentException($"Context key '{key}' is not a valid context key.", nameof(values));
            }

            if (value is { Length: > MaxValueLength })
            {
                throw new ArgumentException($"Context value for '{key}' exceeds {MaxValueLength} characters.", nameof(values));
            }

            copy[key] = value;
        }

        if (copy.Count > MaxKeys)
        {
            throw new ArgumentException($"A decision context carries at most {MaxKeys} keys.", nameof(values));
        }

        return new DecisionContext(copy);
    }

    /// <summary>Lower-case snake_case, matching the public contract grammar.</summary>
    public static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > 40 || key[0] is < 'a' or > 'z')
        {
            return false;
        }

        foreach (char character in key)
        {
            bool allowed = character is >= 'a' and <= 'z' || character is >= '0' and <= '9' || character == '_';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    public string? this[string key] => _values[key];

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<string?> Values => _values.Values;

    public int Count => _values.Count;

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out string? value) => _values.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
