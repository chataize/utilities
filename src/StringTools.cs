using System.Collections.Frozen;

namespace ChatAIze.Utilities;

public static class StringTools
{
    private const int MaxStackStringLength = 256;

    private static readonly FrozenDictionary<char, char> transliterationMap = new Dictionary<char, char>
    {
        { 'ą', 'a' }, { 'ć', 'c' }, { 'ę', 'e' }, { 'ł', 'l' },
        { 'ń', 'n' }, { 'ó', 'o' }, { 'ś', 's' }, { 'ź', 'z' },
        { 'ż', 'z' }, { 'Ą', 'A' }, { 'Ć', 'C' }, { 'Ę', 'E' },
        { 'Ł', 'L' }, { 'Ń', 'N' }, { 'Ó', 'O' }, { 'Ś', 'S' },
        { 'Ź', 'Z' }, { 'Ż', 'Z' }
    }.ToFrozenDictionary();

    public static bool NormalizedEquals(this string? value, string? other)
    {
        if (value is null || other is null)
        {
            return value == other;
        }

        int i = 0, j = 0;

        while (i < value.Length && j < other.Length)
        {
            var latinValue = ToLatin(value[i]);
            var latinOther = ToLatin(other[j]);

            while (!char.IsAsciiLetterOrDigit(latinValue))
            {
                if (++i < value.Length)
                {
                    latinValue = ToLatin(value[i]);
                }
                else
                {
                    break;
                }
            }

            while (!char.IsAsciiLetterOrDigit(latinOther))
            {
                if (++j < other.Length)
                {
                    latinOther = ToLatin(other[j]);
                }
                else
                {
                    break;
                }
            }

            if (char.ToLowerInvariant(latinValue) != char.ToLowerInvariant(latinOther))
            {
                return false;
            }

            ++i;
            ++j;
        }

        while (i < value.Length)
        {
            var latinValue = ToLatin(value[i++]);
            if (char.IsAsciiLetterOrDigit(latinValue))
            {
                return false;
            }
        }

        while (j < other.Length)
        {
            var latinOther = ToLatin(other[j++]);
            if (char.IsAsciiLetterOrDigit(latinOther))
            {
                return false;
            }
        }

        return true;
    }

    public static string WithFallback(this string? value, params string?[] fallbackValues)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        foreach (var fallbackValue in fallbackValues)
        {
            if (!string.IsNullOrWhiteSpace(fallbackValue))
            {
                return fallbackValue;
            }
        }

        return string.Empty;
    }

    public static string ToAlphanumeric(this string value, params char[] additionalChars)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        var newLength = 0;

        var containsDash = false;
        var containsUnderscore = false;

        for (int i = 0; i < additionalChars.Length; ++i)
        {
            if (additionalChars[i] == '-')
            {
                containsDash = true;

                if (containsUnderscore)
                {
                    break;
                }
            }
            else if (additionalChars[i] == '_')
            {
                containsUnderscore = true;

                if (containsDash)
                {
                    break;
                }
            }
        }

        for (int i = 0; i < value.Length; ++i)
        {
            if (char.IsAsciiLetterOrDigit(value[i]) || additionalChars.Contains(value[i]))
            {
                buffer[newLength++] = value[i];
            }
            else if (value[i] == '-' || value[i] == '_' || char.IsWhiteSpace(value[i]))
            {
                if (containsDash)
                {
                    buffer[newLength++] = '-';
                }
                else if (containsUnderscore)
                {
                    buffer[newLength++] = '_';
                }
            }
        }

        return new string(buffer[..newLength]);
    }

    public static char ToLatin(char value)
    {
        return transliterationMap.TryGetValue(value, out var replacement) ? replacement : value;
    }

    public static string ToLatin(this string value)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        for (int i = 0; i < value.Length; ++i)
        {
            buffer[i] = ToLatin(value[i]);
        }

        return new string(buffer);
    }

    public static string ToSeparated(string value, char separator, bool upper = false)
    {
        var maxUnderscores = value.Length / 2 + 1;

        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length + maxUnderscores] : new char[value.Length + maxUnderscores];

        var newLength = 0;
        var actualLength = 0;

        var wasPreviousUpper = false;
        var wasPreviousUnderscore = false;

        for (var i = 0; i < value.Length; ++i)
        {
            if (value[i] == '_' || value[i] == '-' || value[i] == '.' || char.IsWhiteSpace(value[i]))
            {
                if (newLength > 0 && !wasPreviousUnderscore)
                {
                    buffer[newLength++] = separator;
                    wasPreviousUnderscore = true;
                }

                continue;
            }

            var character = ToLatin(value[i]);

            if (!char.IsAsciiLetterOrDigit(character))
            {
                continue;
            }

            if (char.IsAsciiLetterUpper(character))
            {
                if (!wasPreviousUpper && !wasPreviousUnderscore && newLength > 0)
                {
                    buffer[newLength++] = separator;
                }

                buffer[newLength++] = upper ? character : char.ToLowerInvariant(character);

                actualLength = newLength;
                wasPreviousUpper = true;
                wasPreviousUnderscore = false;
            }
            else
            {
                buffer[newLength++] = upper ? char.ToUpperInvariant(character) : character;

                actualLength = newLength;
                wasPreviousUpper = false;
                wasPreviousUnderscore = false;
            }
        }

        return new string(buffer[..actualLength]);
    }

    public static string ToSnakeLower(this string value)
    {
        return ToSeparated(value, '_', upper: false);
    }

    public static string ToSnakeUpper(this string value)
    {
        return ToSeparated(value, '_', upper: true);
    }

    public static string ToKebabLower(this string value)
    {
        return ToSeparated(value, '-', upper: false);
    }

    public static string ToKebabUpper(this string value)
    {
        return ToSeparated(value, '-', upper: true);
    }
}
