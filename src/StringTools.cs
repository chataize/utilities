using System.Collections.Frozen;

namespace ChatAIze.Utilities;

public static class StringTools
{
    private static readonly FrozenDictionary<char, char> transliterationMap = new Dictionary<char, char>
    {
        { 'ą', 'a' }, { 'ć', 'c' }, { 'ę', 'e' }, { 'ł', 'l' },
        { 'ń', 'n' }, { 'ó', 'o' }, { 'ś', 's' }, { 'ź', 'z' },
        { 'ż', 'z' }, { 'Ą', 'A' }, { 'Ć', 'C' }, { 'Ę', 'E' },
        { 'Ł', 'L' }, { 'Ń', 'N' }, { 'Ó', 'O' }, { 'Ś', 'S' },
        { 'Ź', 'Z' }, { 'Ż', 'Z' }
    }.ToFrozenDictionary();

    public static string Transliterate(this string value)
    {
        Span<char> buffer = stackalloc char[value.Length];

        for (int i = 0; i < value.Length; i++)
        {
            buffer[i] = transliterationMap.TryGetValue(value[i], out var replacement) ? replacement : value[i];
        }

        return new string(buffer);
    }
}
