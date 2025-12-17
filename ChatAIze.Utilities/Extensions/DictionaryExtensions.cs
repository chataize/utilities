using System.Text.Json;

namespace ChatAIze.Utilities.Extensions;

/// <summary>
/// Helper extensions for working with settings/value dictionaries used throughout ChatAIze.
/// </summary>
/// <remarks>
/// These helpers are commonly used in ChatAIze.Chatbot when composing action settings, condition settings, and placeholder-aware
/// templates.
/// </remarks>
public static class DictionaryExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Attempts to read a setting value from a JSON dictionary and deserialize it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected value type.</typeparam>
    /// <param name="settings">Dictionary containing JSON values.</param>
    /// <param name="key">Key to look up.</param>
    /// <param name="defaultValue">Fallback value returned when the key is missing or cannot be deserialized.</param>
    /// <returns>The deserialized value or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// This method swallows deserialization errors and returns <paramref name="defaultValue"/>. Use it for tolerant reads from
    /// user-configured settings.
    /// </remarks>
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

    /// <summary>
    /// Returns a copy of <paramref name="values"/> with placeholders expanded in any string values.
    /// </summary>
    /// <param name="values">Input dictionary.</param>
    /// <param name="placeholders">Placeholder values consumed by <c>StringExtension.WithPlaceholderValues</c>.</param>
    /// <returns>A new dictionary instance with substituted values.</returns>
    /// <remarks>
    /// Only string values are processed; non-string values are copied unchanged.
    /// </remarks>
    public static Dictionary<string, object> WithPlaceholderValues(this IReadOnlyDictionary<string, object> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        // Work on a copy so callers can keep their original dictionary unchanged.
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

    /// <summary>
    /// Returns a copy of <paramref name="values"/> with placeholders expanded inside string/object/array JSON values.
    /// </summary>
    /// <param name="values">Input dictionary.</param>
    /// <param name="placeholders">Placeholder values consumed by <c>StringExtension.WithPlaceholderValues</c>.</param>
    /// <returns>A new dictionary instance with substituted values.</returns>
    /// <remarks>
    /// For JSON strings, placeholder substitution is performed directly in the string value.
    /// For JSON objects/arrays, substitution is performed on the raw JSON text and then reparsed.
    /// <para>
    /// Important: because substitution is plain text, your placeholders must produce valid JSON after replacement (no escaping is performed).
    /// </para>
    /// </remarks>
    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IReadOnlyDictionary<string, JsonElement> values, params IEnumerable<KeyValuePair<string, object>> placeholders)
    {
        // Work on a copy so callers can keep their original dictionary unchanged.
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
                // For structured JSON, substitute into the raw JSON text and then reparse.
                // Callers must ensure the placeholder values result in valid JSON (no escaping is performed).
                var rawText = newValues[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                using var document = JsonDocument.Parse(newText);
                // Clone because JsonDocument will be disposed at the end of the using scope.
                newValues[key] = document.RootElement.Clone();
            }
        }

        return newValues;
    }

    /// <summary>
    /// Returns a copy of <paramref name="values"/> with placeholders expanded using JSON values as the placeholder source.
    /// </summary>
    /// <param name="values">Input dictionary.</param>
    /// <param name="placeholders">Placeholder values used by <see cref="StringExtension.WithPlaceholderValues(string, IReadOnlyDictionary{string, JsonElement})"/>.</param>
    /// <returns>A new dictionary instance with substituted values.</returns>
    /// <remarks>
    /// For JSON strings, placeholder substitution is performed directly in the string value.
    /// For JSON objects/arrays, substitution is performed on the raw JSON text and then reparsed.
    /// Your placeholders must produce valid JSON after replacement.
    /// </remarks>
    public static Dictionary<string, JsonElement> WithPlaceholderValues(this IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement> placeholders)
    {
        // Work on a copy so callers can keep their original dictionary unchanged.
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
                // Substitute into raw JSON and reparse (placeholders must preserve JSON validity).
                var rawText = newValues[key].GetRawText() ?? string.Empty;
                var newText = rawText.WithPlaceholderValues(placeholders);

                using var document = JsonDocument.Parse(newText);
                // Clone because JsonDocument will be disposed at the end of the using scope.
                newValues[key] = document.RootElement.Clone();
            }
        }

        return newValues;
    }
}
