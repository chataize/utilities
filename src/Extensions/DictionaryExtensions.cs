using System.Text.Encodings.Web;
using System.Text.Json;

namespace ChatAIze.Utilities.Extensions;

public static class DictionaryExtensions
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

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
                var currentText = newDictionary[key].GetString() ?? string.Empty;
                var newText = currentText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
            }
            else
            {
                var rawText = newDictionary[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
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
                var currentText = newDictionary[key].GetString() ?? string.Empty;
                var newText = currentText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
            }
            else
            {
                var rawText = newDictionary[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                newDictionary[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
            }
        }

        return newDictionary;
    }
}
