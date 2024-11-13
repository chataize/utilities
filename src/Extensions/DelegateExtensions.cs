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
        var argumentsDictionary = ParseJsonArguments(arguments);
        var parsedArguments = ParseArguments(callback, argumentsDictionary, functionContext, cancellationToken);
        var invocationResult = await InvokeDelegateAsync(callback, [.. parsedArguments]);

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
        var parsedArguments = ParseArguments(callback, arguments, actionContext, cancellationToken);
        var invocationResult = await InvokeDelegateAsync(callback, [.. parsedArguments]);

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

    public static async ValueTask<(bool, string?)> InvokeForCallbackResultAsync(this Delegate callback, IDictionary<string, object?> arguments, IConditionContext? conditionContext = null, CancellationToken cancellationToken = default)
    {
        var parsedArguments = ParseArguments(callback, arguments, conditionContext, cancellationToken);
        var invocationResult = await InvokeDelegateAsync(callback, [.. parsedArguments]);

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

    private static Dictionary<string, object?> ParseJsonArguments(string? arguments)
    {
        using var document = arguments is not null ? JsonDocument.Parse(arguments) : JsonDocument.Parse("{}");
        var argumentsDictionary = new Dictionary<string, object?>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            argumentsDictionary[property.Name] = property.Value.Deserialize<object>();
        }

        return argumentsDictionary;
    }

    private static List<object?> ParseArguments(Delegate callback, IDictionary<string, object?> arguments, IConditionContext? context, CancellationToken cancellationToken)
    {
        var parsedArguments = new List<object?>();

        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType is IFunctionContext && context is IFunctionContext functionContext)
            {
                parsedArguments.Add(functionContext);
                continue;
            }

            if (parameter.ParameterType is IActionContext && context is IActionContext actionContext)
            {
                parsedArguments.Add(actionContext);
                continue;
            }

            if (parameter.ParameterType is IConditionContext && context is IConditionContext conditionContext)
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
                if (argument.GetType() != parameter.ParameterType)
                {
                    throw new Exception($"Argument type mismatch for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'.");
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

        return parsedArguments;
    }

    private static async ValueTask<object?> InvokeDelegateAsync(Delegate callback, object?[] parsedArguments)
    {
        var invocationResult = callback.DynamicInvoke(parsedArguments);

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty is not null)
            {
                return resultProperty.GetValue(task);
            }
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);

            var resultProperty = valueTask.GetType().GetProperty("Result");
            if (resultProperty is not null)
            {
                return resultProperty.GetValue(valueTask);
            }
        }

        return invocationResult;
    }
}
