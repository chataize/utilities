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

    public static string ToAlphanumeric(this string value, SpaceHandling spaceHandling = SpaceHandling.Keep)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];
        var newLength = 0;

        for (int i = 0; i < value.Length; ++i)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                buffer[newLength++] = value[i];
            }
            else if (char.IsWhiteSpace(value[i]))
            {
                switch (spaceHandling)
                {
                    case SpaceHandling.Keep:
                        buffer[newLength++] = value[i];
                        break;
                    case SpaceHandling.Underscore:
                        buffer[newLength++] = '_';
                        break;
                    case SpaceHandling.Remove:
                        break;
                }
            }
        }

        return new string(buffer[..newLength]);
    }

    public static string ToLatin(this string value)
    {
        Span<char> buffer = value.Length <= MaxStackStringLength ? stackalloc char[value.Length] : new char[value.Length];

        for (int i = 0; i < value.Length; ++i)
        {
            buffer[i] = transliterationMap.TryGetValue(value[i], out var replacement) ? replacement : value[i];
        }

        return new string(buffer);
    }

    public static string ToSnakeCase(this string value)
    {
        Span<char> buffer = value.Length <= 128 ? stackalloc char[value.Length * 2] : new char[value.Length * 2];

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
                    buffer[newLength++] = '_';
                    wasPreviousUnderscore = true;
                }

                continue;
            }

            if (!char.IsLetterOrDigit(value[i]))
            {
                continue;
            }

            var character = transliterationMap.TryGetValue(value[i], out var replacement) ? replacement : value[i];

            if (char.IsUpper(character))
            {
                if (!wasPreviousUpper && !wasPreviousUnderscore && newLength > 0)
                {
                    buffer[newLength++] = '_';
                }

                buffer[newLength++] = char.ToLower(character);

                actualLength = newLength;
                wasPreviousUpper = true;
                wasPreviousUnderscore = false;
            }
            else
            {
                buffer[newLength++] = character;

                actualLength = newLength;
                wasPreviousUpper = false;
                wasPreviousUnderscore = false;
            }
        }

        return new string(buffer[..actualLength]);
    }
}
