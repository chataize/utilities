using System.Text.Json;
using ChatAIze.Utilities.Extensions;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true
};

var text = "{The quick {animal.color} {animal:name} {animal.x} {action} the lazy {age} years {} old {xx.l} dog}";
var animal = new { Name = "fox", Color = "brown" };
var animalElement = JsonSerializer.SerializeToElement(animal, jsonOptions);
var actionElement = JsonSerializer.SerializeToElement("jumps over", jsonOptions);
var ageElement = JsonSerializer.SerializeToElement(5, jsonOptions);

// case insensitive
var placeholders = new Dictionary<string, JsonElement>(StringComparer.InvariantCultureIgnoreCase)
{
    { "animal", animalElement },
    { "action", actionElement },
    { "age", ageElement }
};

var result = text.WithPlaceholderValues(placeholders);
Console.WriteLine(result);
