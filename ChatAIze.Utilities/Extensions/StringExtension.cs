using System.Text;
using System.Text.Json;

namespace ChatAIze.Utilities.Extensions;

/// <summary>
/// String normalization and lightweight templating helpers used across the ChatAIze stack.
/// </summary>
/// <remarks>
/// These helpers are intentionally culture-invariant and primarily designed for:
/// <list type="bullet">
/// <item><description>normalizing identifiers (<see cref="ToSnakeLower(string)"/>, <see cref="ToKebabLower(string)"/>),</description></item>
/// <item><description>tolerant comparisons of human-entered values (<see cref="NormalizedEquals(string?, string?)"/>),</description></item>
/// <item><description>simple placeholder substitution (<see cref="WithPlaceholderValues(string, IReadOnlyDictionary{string, JsonElement})"/>).</description></item>
/// </list>
/// <para>
/// Important: some of these operations are lossy by design (diacritics removed, punctuation ignored). Do not use them for
/// security-sensitive comparisons (passwords, cryptographic tokens).
/// </para>
/// </remarks>
public static class StringExtension
{
    private const int MaxStackStringLength = 256;

    /// <summary>
    /// Compares two values using ChatAIze "normalized" equality (case/diacritic/punctuation-insensitive).
    /// </summary>
    /// <param name="value">First value.</param>
    /// <param name="other">Second value.</param>
    /// <returns>
    /// <see langword="true"/> if both values are considered equal after normalization; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Normalization rules:
    /// <list type="bullet">
    /// <item><description>Characters are transliterated with <see cref="CharExtension.ToLatin(char)"/>.</description></item>
    /// <item><description>Only ASCII letters and digits participate in the comparison (everything else is ignored).</description></item>
    /// <item><description>Comparison is case-insensitive.</description></item>
    /// </list>
    /// This makes comparisons tolerant of spacing and punctuation: <c>"My-Shop"</c> equals <c>"my shop"</c>,
    /// and <c>"São Paulo"</c> equals <c>"sao paulo"</c>.
    /// </remarks>
    public static bool NormalizedEquals(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
    {
        int i = 0, j = 0;

        // Two-pointer scan: compare only ASCII letters/digits (after ToLatin()) and ignore everything else.
        // This keeps indices stable and avoids allocating normalized copies.
        while (i < value.Length && j < other.Length)
        {
            // Fast path: exact match avoids transliteration and casing work.
            if (value[i] == other[j])
            {
                ++i;
                ++j;

                continue;
            }

            var latinValue = value[i].ToLatin();
            var latinOther = other[j].ToLatin();

            // Skip punctuation/whitespace/non-Latin characters (after transliteration) on each side.
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

        // One side ended: the remaining characters must be ignorable (non-alphanumeric) for the values to match.
        while (i < value.Length)
        {
            var latinValue = value[i++].ToLatin();
            if (char.IsAsciiLetterOrDigit(latinValue))
            {
                return false;
            }
        }

        while (j < other.Length)
        {
            var latinOther = other[j++].ToLatin();
            if (char.IsAsciiLetterOrDigit(latinOther))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Null-safe wrapper for <see cref="NormalizedEquals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="value">First value.</param>
    /// <param name="other">Second value.</param>
    /// <returns>
    /// <see langword="true"/> if both are <see langword="null"/>, or if both are non-null and considered equal after normalization.
    /// </returns>
    public static bool NormalizedEquals(this string? value, string? other)
    {
        if (value is null || other is null)
        {
            return value == other;
        }

        return value.AsSpan().NormalizedEquals(other.AsSpan());
    }

    /// <summary>
    /// Returns <paramref name="value"/> when it is non-empty; otherwise returns the first non-empty fallback value.
    /// </summary>
    /// <param name="value">Primary value.</param>
    /// <param name="fallbackValues">Fallback values to try in order.</param>
    /// <returns>The first non-null, non-whitespace string found; otherwise an empty string.</returns>
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

    /// <summary>
    /// Removes characters that are not ASCII letters/digits (optionally allowing additional characters).
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="additionalChars">Extra characters to allow (for example <c>'-'</c> or <c>'_'</c>).</param>
    /// <returns>An ASCII-only string containing only allowed characters.</returns>
    /// <remarks>
    /// If <c>'-'</c> is allowed, whitespace and underscores are normalized to <c>'-'</c>. If only <c>'_'</c> is allowed,
    /// whitespace and dashes are normalized to <c>'_'</c>.
    /// </remarks>
    public static string ToAlphanumeric(this ReadOnlySpan<char> value, params char[] additionalChars)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        var newLength = 0;

        var containsDash = false;
        var containsUnderscore = false;

        for (var i = 0; i < additionalChars.Length; ++i)
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

        // If a character isn't allowed but is a common separator ('-', '_' or whitespace), normalize it to the preferred separator:
        // '-' when allowed (wins if both are allowed), otherwise '_' when allowed.
        for (var i = 0; i < value.Length; ++i)
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

    /// <summary>
    /// String overload for <see cref="ToAlphanumeric(ReadOnlySpan{char}, char[])"/>.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="additionalChars">Extra characters to allow (for example <c>'-'</c> or <c>'_'</c>).</param>
    /// <returns>An ASCII-only string containing only allowed characters.</returns>
    public static string ToAlphanumeric(this string value, params char[] additionalChars)
    {
        return value.AsSpan().ToAlphanumeric(additionalChars);
    }

    /// <summary>
    /// Transliterates common Latin characters with diacritics into ASCII-friendly equivalents.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>A string where known characters were mapped via <see cref="CharExtension.ToLatin(char)"/>.</returns>
    /// <remarks>
    /// This does not remove non-Latin characters; it only replaces characters that exist in the internal mapping table.
    /// </remarks>
    public static string ToLatin(this ReadOnlySpan<char> value)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        for (var i = 0; i < value.Length; ++i)
        {
            buffer[i] = value[i].ToLatin();
        }

        return new string(buffer);
    }

    /// <summary>
    /// String overload for <see cref="ToLatin(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>A transliterated string.</returns>
    public static string ToLatin(this string value)
    {
        return value.AsSpan().ToLatin();
    }

    /// <summary>
    /// Converts <paramref name="value"/> into a separator-delimited identifier (snake_case, kebab-case, etc.).
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="separator">Separator character to insert between tokens (for example <c>'_'</c> or <c>'-'</c>).</param>
    /// <param name="upper">When <see langword="true"/>, output letters are upper-cased.</param>
    /// <returns>A normalized identifier string containing only ASCII letters/digits and <paramref name="separator"/>.</returns>
    /// <remarks>
    /// The algorithm:
    /// <list type="bullet">
    /// <item><description>Normalizes whitespace, punctuation, <c>'_'</c>, <c>'-'</c>, and <c>'.'</c> into a single separator.</description></item>
    /// <item><description>Splits on transitions from lower-case to upper-case (so <c>"ChatBot"</c> becomes <c>"chat_bot"</c>).</description></item>
    /// <item><description>Transliterates diacritics using <see cref="CharExtension.ToLatin(char)"/> and drops non-ASCII letters/digits.</description></item>
    /// </list>
    /// </remarks>
    public static string ToSeparated(this ReadOnlySpan<char> value, char separator, bool upper = false)
    {
        // Worst-case we might insert ~1 separator for every 2 characters (camelCase transitions + explicit separators).
        var maxUnderscores = (value.Length / 2) + 1;
        Span<char> buffer = value.Length + maxUnderscores <= MaxStackStringLength ? stackalloc char[value.Length + maxUnderscores] : new char[value.Length + maxUnderscores];

        var newLength = 0;
        // Tracks the last emitted "real" character so we can drop trailing separators (e.g. input ends with whitespace).
        var actualLength = 0;

        var wasPreviousUpper = false;
        var wasPreviousUnderscore = false;

        for (var i = 0; i < value.Length; ++i)
        {
            if (value[i] == '_' || value[i] == '-' || value[i] == '.' || char.IsWhiteSpace(value[i]))
            {
                // Collapse runs of separators into a single one and never start with a separator.
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
                // Split on lower->upper transitions ("ChatBot" => "chat_bot"). Consecutive uppers stay in the same token.
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

    /// <summary>
    /// String overload for <see cref="ToSeparated(ReadOnlySpan{char}, char, bool)"/>.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="separator">Separator character to insert between tokens.</param>
    /// <param name="upper">When <see langword="true"/>, output letters are upper-cased.</param>
    /// <returns>A normalized identifier string.</returns>
    public static string ToSeparated(this string value, char separator, bool upper = false)
    {
        return value.AsSpan().ToSeparated(separator, upper);
    }

    /// <summary>
    /// Converts <paramref name="value"/> to <c>snake_case</c> using lowercase letters.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>A snake_case identifier.</returns>
    public static string ToSnakeLower(this string value)
    {
        return ToSeparated(value, '_', upper: false);
    }

    /// <summary>
    /// Converts <paramref name="value"/> to <c>SNAKE_CASE</c> using uppercase letters.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>An upper snake_case identifier.</returns>
    public static string ToSnakeUpper(this string value)
    {
        return ToSeparated(value, '_', upper: true);
    }

    /// <summary>
    /// Converts <paramref name="value"/> to <c>kebab-case</c> using lowercase letters.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>A kebab-case identifier.</returns>
    public static string ToKebabLower(this string value)
    {
        return ToSeparated(value, '-', upper: false);
    }

    /// <summary>
    /// Converts <paramref name="value"/> to <c>KEBAB-CASE</c> using uppercase letters.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>An upper kebab-case identifier.</returns>
    public static string ToKebabUpper(this string value)
    {
        return ToSeparated(value, '-', upper: true);
    }

    /// <summary>
    /// Replaces placeholders in the form <c>{key}</c> using the provided string values.
    /// </summary>
    /// <param name="value">Template string.</param>
    /// <param name="placeholders">Placeholder values (keys are normalized to snake_case before replacement).</param>
    /// <returns>The template string with placeholders replaced.</returns>
    /// <remarks>
    /// Keys are normalized with <see cref="ToSnakeLower(string)"/>, so a placeholder with key <c>"UserId"</c> is written as
    /// <c>{user_id}</c> in the template.
    /// <para>
    /// This performs plain text substitution without escaping.
    /// </para>
    /// </remarks>
    public static string WithPlaceholderValues(this string value, params IEnumerable<KeyValuePair<string, string>> placeholders)
    {
        var result = new StringBuilder(value);

        foreach (var placeholder in placeholders)
        {
            _ = result.Replace($"{{{placeholder.Key.ToSnakeLower()}}}", placeholder.Value);
        }

        return result.ToString();
    }

    /// <summary>
    /// Replaces placeholders in the form <c>{key}</c> using the provided values.
    /// </summary>
    /// <param name="value">Template string.</param>
    /// <param name="placeholders">Placeholder values (keys are normalized to snake_case before replacement).</param>
    /// <returns>The template string with placeholders replaced.</returns>
    /// <remarks>
    /// Non-string values are replaced using <see cref="object.ToString"/>. This performs plain text substitution without escaping.
    /// </remarks>
    public static string WithPlaceholderValues(this string value, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var result = new StringBuilder(value);

        foreach (var placeholder in placeholders)
        {
            if (placeholder.Value is string stringValue)
            {
                _ = result.Replace($"{{{placeholder.Key.ToSnakeLower()}}}", stringValue);
            }
            else
            {
                var placeholderValue = placeholder.Value?.ToString() ?? string.Empty;
                _ = result.Replace($"{{{placeholder.Key.ToSnakeLower()}}}", placeholderValue);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Replaces placeholders using values taken from a JSON element dictionary.
    /// </summary>
    /// <param name="value">Template string.</param>
    /// <param name="placeholders">Placeholder dictionary where each value is a JSON element.</param>
    /// <returns>The template string with placeholders replaced.</returns>
    /// <remarks>
    /// Supported placeholder syntax:
    /// <list type="bullet">
    /// <item><description><c>{key}</c> – replaced with the JSON value under <c>key</c>.</description></item>
    /// <item><description><c>{key.sub_property}</c> – for object values, traverses properties using snake_case normalization.</description></item>
    /// </list>
    /// <para>
    /// Values that are strings are inserted as-is. Non-string values are inserted using their raw JSON representation.
    /// This performs substitution without escaping.
    /// </para>
    /// </remarks>
    public static string WithPlaceholderValues(this string value, IReadOnlyDictionary<string, JsonElement> placeholders)
    {
        var begin = -1;
        var valueSpan = value.AsSpan();
        var result = new StringBuilder(value);

        // Scan the original template to find {...} ranges; replacements are applied to the StringBuilder.
        // Using the original span keeps indices stable even when replacements change the output length.
        for (var i = 0; i < valueSpan.Length; ++i)
        {
            if (valueSpan[i] == '{')
            {
                begin = i;
            }
            else if (valueSpan[i] == '}' && begin != -1)
            {
                var key = value[(begin + 1)..i];
                var path = key.Split(['.', ':'], StringSplitOptions.RemoveEmptyEntries);

                if (path.Length > 0 && placeholders.TryGetValue(path[0], out var element))
                {
                    // Support object property traversal: {user.name} reads placeholders["user"].name (properties normalized to snake_case).
                    var elementValue = GetElementValue(element, path[1..]);
                    if (elementValue is not null)
                    {
                        result.Replace(valueSpan[begin..(i + 1)], elementValue);
                    }
                }

                begin = -1;
            }
        }

        return result.ToString();
    }

    private static string? GetElementValue(JsonElement element, string[] path)
    {
        foreach (var part in path)
        {
            if (!element.TryGetProperty(part.ToSnakeLower(), out element))
            {
                return null;
            }
        }

        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (element.ValueKind is JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        return element.GetRawText();
    }
}
