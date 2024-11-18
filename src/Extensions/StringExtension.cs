using System.Text;

namespace ChatAIze.Utilities.Extensions;

public static class StringExtension
{
    private const int MaxStackStringLength = 256;

    public static bool NormalizedEquals(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
    {
        int i = 0, j = 0;

        while (i < value.Length && j < other.Length)
        {
            if (value[i] == other[j])
            {
                ++i;
                ++j;

                continue;
            }

            char latinValue = value[i].ToLatin();
            char latinOther = other[j].ToLatin();

            while (!char.IsAsciiLetterOrDigit(latinValue))
            {
                if (++i < value.Length)
                {
                    latinValue = value[i].ToLatin();
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
                    latinOther = other[j].ToLatin();
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
            char latinValue = value[i++].ToLatin();
            if (char.IsAsciiLetterOrDigit(latinValue))
            {
                return false;
            }
        }

        while (j < other.Length)
        {
            char latinOther = other[j++].ToLatin();
            if (char.IsAsciiLetterOrDigit(latinOther))
            {
                return false;
            }
        }

        return true;
    }

    public static bool NormalizedEquals(this string? value, string? other)
    {
        if (value is null || other is null)
        {
            return value == other;
        }

        return value.AsSpan().NormalizedEquals(other.AsSpan());
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

    public static string ToAlphanumeric(this ReadOnlySpan<char> value, params char[] additionalChars)
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

    public static string ToAlphanumeric(this string value, params char[] additionalChars)
    {
        return value.AsSpan().ToAlphanumeric(additionalChars);
    }

    public static string ToLatin(this ReadOnlySpan<char> value)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        for (int i = 0; i < value.Length; ++i)
        {
            buffer[i] = value[i].ToLatin();
        }

        return new string(buffer);
    }

    public static string ToLatin(this string value)
    {
        return value.AsSpan().ToLatin();
    }

    public static string ToSeparated(this ReadOnlySpan<char> value, char separator, bool upper = false)
    {
        var maxUnderscores = value.Length / 2 + 1;
        Span<char> buffer = value.Length + maxUnderscores <= MaxStackStringLength ? stackalloc char[value.Length + maxUnderscores] : new char[value.Length + maxUnderscores];

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

            var character = value[i].ToLatin();

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

    public static string ToSeparated(this string value, char separator, bool upper = false)
    {
        return value.AsSpan().ToSeparated(separator, upper);
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

    public static string WithPlaceholderValues(this string value, params IEnumerable<KeyValuePair<string, string>> placeholders)
    {
        var result = new StringBuilder(value);

        foreach (var placeholder in placeholders)
        {
            result.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            result.Replace($"{{{placeholder.Key.ToSnakeLower()}}}", placeholder.Value);
        }

        return result.ToString();
    }

    public static string WithPlaceholderValues(this string value, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var result = new StringBuilder(value);

        foreach (var placeholder in placeholders)
        {
            result.Replace($"{{{placeholder.Key}}}", placeholder.Value.ToString());
            result.Replace($"{{{placeholder.Key.ToSnakeLower()}}}", placeholder.Value.ToString());
        }

        return result.ToString();
    }
}
