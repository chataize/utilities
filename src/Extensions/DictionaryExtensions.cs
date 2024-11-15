namespace ChatAIze.Utilities.Extensions;

public static class DictionaryExtensions
{
    public static IDictionary<string, object> WithPlaceholderValues(this IDictionary<string, object> dictionary, params IEnumerable<KeyValuePair<string, object>> placeholders)
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
}
