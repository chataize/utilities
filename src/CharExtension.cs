using System.Collections.Frozen;

namespace ChatAIze.Utilities;

public static class CharExtension
{
    private static readonly FrozenDictionary<char, char> transliterationMap = new Dictionary<char, char>
    {
        { 'I', 'İ' },
        { 'À', 'A' },
        { 'Á', 'A' },
        { 'Â', 'A' },
        { 'Ä', 'A' },
        { 'Å', 'A' },
        { 'Æ', 'A' },
        { 'Ç', 'C' },
        { 'È', 'E' },
        { 'É', 'E' },
        { 'Ê', 'E' },
        { 'Ë', 'E' },
        { 'Ì', 'I' },
        { 'Í', 'I' },
        { 'Î', 'I' },
        { 'Ï', 'I' },
        { 'Ñ', 'N' },
        { 'Ò', 'O' },
        { 'Ó', 'O' },
        { 'Ô', 'O' },
        { 'Ö', 'O' },
        { 'Ø', 'O' },
        { 'Ù', 'U' },
        { 'Ú', 'U' },
        { 'Û', 'U' },
        { 'Ü', 'U' },
        { 'ß', 's' },
        { 'à', 'a' },
        { 'á', 'a' },
        { 'â', 'a' },
        { 'ä', 'a' },
        { 'å', 'a' },
        { 'æ', 'a' },
        { 'ç', 'c' },
        { 'è', 'e' },
        { 'é', 'e' },
        { 'ê', 'e' },
        { 'ë', 'e' },
        { 'ì', 'i' },
        { 'í', 'i' },
        { 'î', 'i' },
        { 'ï', 'i' },
        { 'ñ', 'n' },
        { 'ò', 'o' },
        { 'ó', 'o' },
        { 'ô', 'o' },
        { 'ö', 'o' },
        { 'ø', 'o' },
        { 'ù', 'u' },
        { 'ú', 'u' },
        { 'û', 'u' },
        { 'ü', 'u' },
        { 'Ā', 'A' },
        { 'ā', 'a' },
        { 'Ă', 'A' },
        { 'ă', 'a' },
        { 'Ą', 'A' },
        { 'ą', 'a' },
        { 'Ć', 'C' },
        { 'ć', 'c' },
        { 'Č', 'C' },
        { 'č', 'c' },
        { 'Ď', 'D' },
        { 'ď', 'd' },
        { 'Đ', 'D' },
        { 'đ', 'd' },
        { 'Ē', 'E' },
        { 'ē', 'e' },
        { 'Ė', 'E' },
        { 'ė', 'e' },
        { 'Ę', 'E' },
        { 'ę', 'e' },
        { 'Ě', 'E' },
        { 'ě', 'e' },
        { 'Ğ', 'G' },
        { 'ğ', 'g' },
        { 'Ģ', 'G' },
        { 'ģ', 'g' },
        { 'Ī', 'I' },
        { 'ī', 'i' },
        { 'Į', 'I' },
        { 'į', 'i' },
        { 'ı', 'i' },
        { 'Ķ', 'K' },
        { 'ķ', 'k' },
        { 'Ĺ', 'L' },
        { 'ĺ', 'l' },
        { 'Ļ', 'L' },
        { 'ļ', 'l' },
        { 'Ľ', 'L' },
        { 'ľ', 'l' },
        { 'Ł', 'L' },
        { 'ł', 'l' },
        { 'Ń', 'N' },
        { 'ń', 'n' },
        { 'Ņ', 'N' },
        { 'ņ', 'n' },
        { 'Ň', 'N' },
        { 'ň', 'n' },
        { 'Ő', 'O' },
        { 'ő', 'o' },
        { 'Ŕ', 'R' },
        { 'ŕ', 'r' },
        { 'Ř', 'R' },
        { 'ř', 'r' },
        { 'Ś', 'S' },
        { 'ś', 's' },
        { 'Ş', 'S' },
        { 'ş', 's' },
        { 'Š', 'S' },
        { 'š', 's' },
        { 'Ť', 'T' },
        { 'ť', 't' },
        { 'Ū', 'U' },
        { 'ū', 'u' },
        { 'Ů', 'U' },
        { 'ů', 'u' },
        { 'Ű', 'U' },
        { 'ű', 'u' },
        { 'Ų', 'U' },
        { 'ų', 'u' },
        { 'Ź', 'Z' },
        { 'ź', 'z' },
        { 'Ż', 'Z' },
        { 'ż', 'z' },
        { 'Ž', 'Z' },
        { 'ž', 'z' },
        { 'Ș', 'S' },
        { 'ș', 's' },
        { 'Ț', 'T' },
        { 'ț', 't' }
    }.ToFrozenDictionary();

    public static char ToLatin(this char value)
    {
        return transliterationMap.TryGetValue(value, out var replacement) ? replacement : value;
    }
}
