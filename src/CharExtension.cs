using System.Collections.Frozen;

namespace ChatAIze.Utilities;

public static class CharExtension
{
    private static readonly FrozenDictionary<char, char> transliterationMap = new Dictionary<char, char>
    {
        { 'ą', 'a' }, { 'ć', 'c' }, { 'ę', 'e' }, { 'ł', 'l' },
        { 'ń', 'n' }, { 'ó', 'o' }, { 'ś', 's' }, { 'ź', 'z' },
        { 'ż', 'z' }, { 'Ą', 'A' }, { 'Ć', 'C' }, { 'Ę', 'E' },
        { 'Ł', 'L' }, { 'Ń', 'N' }, { 'Ó', 'O' }, { 'Ś', 'S' },
        { 'Ź', 'Z' }, { 'Ż', 'Z' }
    }.ToFrozenDictionary();


    public static char ToLatin(this char value)
    {
        return transliterationMap.TryGetValue(value, out var replacement) ? replacement : value;
    }
}
