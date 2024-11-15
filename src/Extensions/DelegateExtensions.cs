using System.Text.Encodings.Web;
using System.Text.Json;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.Utilities.Extensions;

public static class DelegateExtensions
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static string GetNormalizedMethodName(this Delegate callback)
    {
        var name = callback.Method.Name;

        if (!name.Contains("<<"))
        {
            return name;
        }

        var start = name.IndexOf("__");
        var end = name.IndexOf('|');

        if (start < 0 || end < 0 || start >= end)
        {
            throw new Exception($"Unable to get normalized method name from '{name}'.");
        }

        return name[(start + 2)..end];
    }

    public static async ValueTask<string> InvokeForStringResultAsync(this Delegate callback, string? arguments, IFunctionContext? functionContext = null, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();
        using var argumentsDocument = arguments is not null ? JsonDocument.Parse(arguments) : JsonDocument.Parse("{}");

        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(IFunctionContext))
            {
                parsedArguments.Add(functionContext);
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (argumentsDocument.RootElement.TryGetProperty(parameter.Name!.ToSnakeLower(), out var argument) && argument.ValueKind != JsonValueKind.Null)
            {
                var rawValue = argument.GetRawText();
                var stringValue = argument.ValueKind == JsonValueKind.String ? argument.GetString() : rawValue;

                try
                {
                    if (parameter.ParameterType.IsEnum)
                    {
                        if (!Enum.TryParse(parameter.ParameterType, stringValue!.Replace("_", ""), true, out var enumValue))
                        {
                            return $"Error: Value '{stringValue}' is not a valid enum member for parameter '{parameter.Name}'.";
                        }

                        parsedArguments.Add(enumValue);
                    }
                    else
                    {
                        var parsedValue = JsonSerializer.Deserialize(rawValue, parameter.ParameterType);
                        parsedArguments.Add(parsedValue);
                    }
                }
                catch
                {
                    return $"Error: Value '{stringValue}' is not valid for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'.";
                }
            }
            else if (parameter.IsOptional && parameter.DefaultValue != DBNull.Value)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                return $"Error: You must provide a value for the required parameter '{parameter.Name}'.";
            }
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);

            var taskResultProperty = valueTask.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(valueTask);
            }
        }

        if (invocationResult is null)
        {
            return "OK: Function executed successfully.";
        }

        if (invocationResult is string stringResult)
        {
            return stringResult;
        }

        return JsonSerializer.Serialize(invocationResult, JsonOptions);
    }

    public static async ValueTask<string> InvokeForStringResultAsync(this Delegate callback, IDictionary<string, object?> arguments, IActionContext? actionContext = null, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();

        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(IActionContext))
            {
                parsedArguments.Add(actionContext);
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (arguments.TryGetValue(parameter.Name!.ToSnakeLower(), out var argument) && argument is not null)
            {
                if (argument.GetType() != parameter.GetType())
                {
                    throw new Exception($"Argument type mismatch for parameter '{parameter.Name}'. Expected type: '{parameter.GetType().Name}'.");
                }

                parsedArguments.Add(argument);
            }
            else if (parameter.IsOptional && parameter.DefaultValue != DBNull.Value)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                throw new Exception($"Argument missing for non-nullable parameter '{parameter.Name}'.");
            }
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);

            var taskResultProperty = valueTask.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(valueTask);
            }
        }

        if (invocationResult is null)
        {
            return "OK: Action executed successfully.";
        }

        if (invocationResult is string stringResult)
        {
            return stringResult;
        }

        return JsonSerializer.Serialize(invocationResult, JsonOptions);
    }

    public static async ValueTask<(bool, string?)> InvokeForConditionResultAsync(this Delegate callback, IDictionary<string, object?> arguments, IFunctionCondition? conditionContext = null, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();

        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(IConditionContext))
            {
                parsedArguments.Add(conditionContext);
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (arguments.TryGetValue(parameter.Name!.ToSnakeLower(), out var argument) && argument is not null)
            {
                if (argument.GetType() != parameter.GetType())
                {
                    throw new Exception($"Argument type mismatch for parameter '{parameter.Name}'. Expected type: '{parameter.GetType().Name}'.");
                }

                parsedArguments.Add(argument);
            }
            else if (parameter.IsOptional && parameter.DefaultValue != DBNull.Value)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                throw new Exception($"Argument missing for non-nullable parameter '{parameter.Name}'.");
            }
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);

            var taskResultProperty = valueTask.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(valueTask);
            }
        }

        if (invocationResult is true)
        {
            return (true, null);
        }

        if (invocationResult is null)
        {
            return (false, null);
        }

        if (invocationResult is string stringResult)
        {
            return (false, stringResult);
        }

        return (false, JsonSerializer.Serialize(invocationResult, JsonOptions));
    }
}
