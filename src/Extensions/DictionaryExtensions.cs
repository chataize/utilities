using System.Text.Json;

namespace ChatAIze.Utilities.Extensions;

public static class DictionaryExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static T TryGetSettingValue<T>(this IReadOnlyDictionary<string, JsonElement> settings, string key, T defaultValue)
    {
        try
        {
            if (settings.TryGetValue(key, out var value))
            {
                return value.Deserialize<T>() ?? defaultValue;
            }
        }
        catch { }
        return defaultValue;
    }

    public static Dictionary<string, object> WithPlaceholderValues(this IReadOnlyDictionary<string, object> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
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

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IReadOnlyDictionary<string, JsonElement> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
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

    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement> placeholders)
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
