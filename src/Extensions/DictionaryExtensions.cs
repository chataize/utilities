using System.Text.Json;

namespace ChatAIze.Utilities.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<string, object> WithPlaceholderValues(this IDictionary<string, object> dictionary, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var newDictionary = new Dictionary<string, object>(dictionary);

        foreach (var key in newDictionary.Keys)
        {
            if (newDictionary[key] is string stringValue)
            {
                newDictionary[key] = stringValue.WithPlaceholderValues(placeholders);
            }
        }

        return newDictionary;
    }

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IDictionary<string, JsonElement> dictionary, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var newDictionary = new Dictionary<string, JsonElement>(dictionary);

        foreach (var key in newDictionary.Keys)
        {
            if (newDictionary[key].ValueKind == JsonValueKind.String)
            {
                var rawText = newDictionary[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText);
            }
        }

        return newDictionary;
    }

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IDictionary<string, JsonElement> dictionary, IReadOnlyDictionary<string, JsonElement> placeholders)
    {
        var newDictionary = new Dictionary<string, JsonElement>(dictionary);

        foreach (var key in newDictionary.Keys)
        {
            if (newDictionary[key].ValueKind == JsonValueKind.String)
            {
                var rawText = newDictionary[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText);
            }
        }

        return newDictionary;
    }
}
