using ChatAIze.Utilities;

var input = "  _  __ Hello,  World! UserID HTTP ąĘćÓłż RequestExample 1234.. FIRST.second.Third  _ ";
var snakeOutput = input.ToSnakeCase();
var kebabOutput = input.ToKebabCase();

Console.WriteLine(snakeOutput);
Console.WriteLine(kebabOutput);
