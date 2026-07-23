namespace Adex.Domain.Placements;

/// <summary>
/// Grammar shared by placement and alternative keys. Implemented as an explicit
/// scan rather than a regular expression so the rule is obvious in a review and
/// cannot become a backtracking cost on the request path.
/// </summary>
internal static class KeyGrammar
{
    public static bool IsValid(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
        {
            return false;
        }

        if (!IsAlphanumeric(value[0]) || !IsAlphanumeric(value[^1]))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!IsAlphanumeric(character) && character is not ('.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlphanumeric(char character) =>
        character is >= 'a' and <= 'z' || character is >= '0' and <= '9';
}
