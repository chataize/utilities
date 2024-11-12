using ChatAIze.Utilities.Extensions;

var input = "  _  __ Hello, %%&XX *Y  World! UserID HTTP ąĘćÓłż RequestExample 1234.. FIRST.second.Third  _ ";

var snakeLower = input.ToSnakeLower();
var snakeUpper = input.ToSnakeUpper();
var kebabLower = input.ToKebabLower();
var ToKebabUpper = input.ToKebabUpper();

Console.WriteLine($"Input: {input}");
Console.WriteLine($"Snake lower: {snakeLower}");
Console.WriteLine($"Snake upper: {snakeUpper}");
Console.WriteLine($"Kebab lower: {kebabLower}");
Console.WriteLine($"Kebab upper: {ToKebabUpper}");

Console.WriteLine($"Equal: {input.NormalizedEquals(snakeLower)}");
