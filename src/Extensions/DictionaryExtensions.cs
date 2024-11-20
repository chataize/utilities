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

    public static Dictionary<string, object> WithPlaceholderValues(this IDictionary<string, object> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var newValues = new Dictionary<string, object>(values);

        foreach (var key in newValues.Keys)
        {
            if (newValues[key] is string stringValue)
            {
                newValues[key] = stringValue.WithPlaceholderValues(placeholders);
            }
        }

        return newValues;
    }

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IDictionary<string, JsonElement> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        var newValues = new Dictionary<string, JsonElement>(values);

        foreach (var key in newValues.Keys)
        {
            if (newValues[key].ValueKind == JsonValueKind.String)
            {
                var currentText = newValues[key].GetString() ?? string.Empty;
                var newText = currentText.WithPlaceholderValues(placeholders);

                newValues[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
            }
            else if (newValues[key].ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                var rawText = newValues[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                using var document = JsonDocument.Parse(newText);
                newValues[key] = document.RootElement.Clone();
            }
        }

        return newValues;
    }

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement> placeholders)
    {
        var newValues = new Dictionary<string, JsonElement>(values);

        foreach (var key in newValues.Keys)
        {
            if (newValues[key].ValueKind == JsonValueKind.String)
            {
                var currentText = newValues[key].GetString() ?? string.Empty;
                var newText = currentText.WithPlaceholderValues(placeholders);

                newValues[key] = JsonSerializer.SerializeToElement(newText, JsonOptions);
            }
            else if (newValues[key].ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                var rawText = newValues[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                using var document = JsonDocument.Parse(newText);
                newValues[key] = document.RootElement.Clone();
            }
        }

        return newValues;
    }
}
