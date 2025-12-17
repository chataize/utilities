using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.Utilities.Extensions;

/// <summary>
/// Delegate helpers for ChatAIze function calling and workflow execution.
/// </summary>
/// <remarks>
/// These helpers are used throughout the ChatAIze stack to:
/// <list type="bullet">
/// <item><description>derive stable names from delegates (used by plugin/function registration),</description></item>
/// <item><description>invoke delegates using JSON arguments produced by LLM tool calls,</description></item>
/// <item><description>invoke workflow actions/conditions using settings dictionaries.</description></item>
/// </list>
/// <para>
/// The default argument binding rules are aligned with ChatAIze's conventions:
/// parameter names are matched using snake_case (<see cref="StringExtension.ToSnakeLower(string)"/>) and return values are serialized
/// using <see cref="JsonNamingPolicy.SnakeCaseLower"/> for model-friendly output.
/// </para>
/// </remarks>
public static class DelegateExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Returns a stable, human-friendly method name for <paramref name="callback"/>.
    /// </summary>
    /// <param name="callback">Delegate whose method name should be normalized.</param>
    /// <returns>The method name or a normalized name extracted from compiler-generated patterns.</returns>
    /// <remarks>
    /// Some compiler-generated method names (for lambdas/local functions) are not suitable as public identifiers.
    /// This helper attempts to extract a more meaningful name from those patterns.
    /// <para>
    /// If the method name format is not recognized, this method throws.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown when a compiler-generated method name cannot be normalized.</exception>
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

    /// <summary>
    /// Invokes a function/tool delegate using a JSON argument payload and returns a string result suitable for LLM tool output.
    /// </summary>
    /// <param name="callback">Delegate to invoke.</param>
    /// <param name="arguments">JSON object string containing arguments (keys are expected to be snake_case).</param>
    /// <param name="functionContext">Optional function context injected when the delegate accepts an <see cref="IFunctionContext"/> parameter.</param>
    /// <param name="cancellationToken">Cancellation token injected when the delegate accepts a <see cref="CancellationToken"/> parameter.</param>
    /// <returns>
    /// A string result. If the delegate returns a non-string object, the value is serialized to JSON using snake_case property naming.
    /// </returns>
    /// <remarks>
    /// Parameter binding rules:
    /// <list type="bullet">
    /// <item><description><see cref="IFunctionContext"/> parameters are bound to <paramref name="functionContext"/>.</description></item>
    /// <item><description><see cref="CancellationToken"/> parameters are bound to <paramref name="cancellationToken"/>.</description></item>
    /// <item><description>Other parameters are read from <paramref name="arguments"/> using <c>parameterName.ToSnakeLower()</c>.</description></item>
    /// </list>
    /// <para>
    /// Validation rules:
    /// <list type="bullet">
    /// <item><description><see cref="RequiredAttribute"/> on string parameters enforces non-empty input.</description></item>
    /// <item><description><see cref="MinLengthAttribute"/>, <see cref="MaxLengthAttribute"/>, and <see cref="StringLengthAttribute"/> are enforced on string parameters.</description></item>
    /// <item><description>Enum parameters are parsed case-insensitively and tolerate underscores.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method returns <c>"Error: ..."</c> strings for validation failures. Exceptions thrown by <paramref name="callback"/>
    /// itself are not caught and will propagate to the caller.
    /// </para>
    /// </remarks>
    public static async ValueTask<string> InvokeForStringResultAsync(this Delegate callback, string arguments, IFunctionContext? functionContext = null, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();
        using var argumentsDocument = JsonDocument.Parse(arguments);

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

                if (parameter.ParameterType == typeof(string))
                {
                    if (parameter.GetCustomAttribute<RequiredAttribute>() is not null && string.IsNullOrWhiteSpace(stringValue))
                    {
                        return $"Error: Value missing for required parameter '{parameter.Name}'.";
                    }

                    var (minLen, maxLen) = GetStringLengthConstraints(parameter);
                    var length = stringValue?.Length ?? 0;
                    if (minLen.HasValue && length < minLen.Value)
                    {
                        return $"Error: Value for parameter '{parameter.Name}' must be at least {minLen.Value} characters long.";
                    }

                    if (maxLen.HasValue && length > maxLen.Value)
                    {
                        return $"Error: Value for parameter '{parameter.Name}' must be at most {maxLen.Value} characters long.";
                    }
                }

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
                        var parsedValue = JsonSerializer.Deserialize(rawValue, parameter.ParameterType, JsonOptions);
                        parsedArguments.Add(parsedValue);
                    }
                }
                catch
                {
                    return $"Error: Value '{stringValue}' is not valid for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'.";
                }

                continue;
            }

            if (parameter.IsOptional)
            {
                parsedArguments.Add(parameter.DefaultValue);
                continue;
            }

            var defaultAttribute = parameter.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultAttribute is not null)
            {
                parsedArguments.Add(defaultAttribute.Value);
                continue;
            }

            return $"Error: Value missing for required parameter '{parameter.Name}'.";
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is null)
        {
            return "OK: Function executed successfully.";
        }

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult.GetType().IsGenericType && invocationResult.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            dynamic dynamicValueTask = invocationResult;
            invocationResult = await dynamicValueTask.ConfigureAwait(false);
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            invocationResult = null;
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

    /// <summary>
    /// Invokes a workflow action delegate using a settings dictionary and returns a string result.
    /// </summary>
    /// <param name="callback">Delegate to invoke.</param>
    /// <param name="arguments">Dictionary containing JSON values for action settings.</param>
    /// <param name="actionContext">Optional action context injected when the delegate accepts an <see cref="IActionContext"/> parameter.</param>
    /// <param name="cancellationToken">Cancellation token injected when the delegate accepts a <see cref="CancellationToken"/> parameter.</param>
    /// <returns>
    /// A string result. If the delegate returns a non-string object, the value is serialized to JSON using snake_case property naming.
    /// </returns>
    /// <remarks>
    /// Parameter binding rules:
    /// <list type="bullet">
    /// <item><description><see cref="IActionContext"/> parameters are bound to <paramref name="actionContext"/>.</description></item>
    /// <item><description><see cref="CancellationToken"/> parameters are bound to <paramref name="cancellationToken"/>.</description></item>
    /// <item><description>Other parameters are read from <paramref name="arguments"/> by exact name or snake_case name.</description></item>
    /// </list>
    /// <para>
    /// When a required value is missing or invalid, this method marks the action as failed via
    /// <see cref="IActionContext.SetActionResult"/> (when <paramref name="actionContext"/> is provided) and returns a human-readable
    /// error message.
    /// </para>
    /// </remarks>
    public static async ValueTask<string> InvokeForStringResultAsync(this Delegate callback, IReadOnlyDictionary<string, JsonElement> arguments, IActionContext? actionContext = null, CancellationToken cancellationToken = default)
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

            JsonElement argument = default;

            if (arguments.TryGetValue(parameter.Name!, out var argument1))
            {
                argument = argument1;
            }
            else if (arguments.TryGetValue(parameter.Name!.ToSnakeLower(), out var argument2))
            {
                argument = argument2;
            }

            if (argument.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            {
                if (parameter.ParameterType == typeof(string))
                {
                    var value = argument.GetString();
                    if (parameter.GetCustomAttribute<RequiredAttribute>() is not null && string.IsNullOrWhiteSpace(value))
                    {
                        actionContext?.SetActionResult(isSuccess: false, $"Value missing for required parameter '{parameter.Name}'.");
                        return $"Value missing for required parameter '{parameter.Name}'.";
                    }

                    var (minLen, maxLen) = GetStringLengthConstraints(parameter);
                    var length = value?.Length ?? 0;
                    if (minLen.HasValue && length < minLen.Value)
                    {
                        actionContext?.SetActionResult(isSuccess: false, $"Value for parameter '{parameter.Name}' must be at least {minLen.Value} characters long.");
                        return $"Value for parameter '{parameter.Name}' must be at least {minLen.Value} characters long.";
                    }

                    if (maxLen.HasValue && length > maxLen.Value)
                    {
                        actionContext?.SetActionResult(isSuccess: false, $"Value for parameter '{parameter.Name}' must be at most {maxLen.Value} characters long.");
                        return $"Value for parameter '{parameter.Name}' must be at most {maxLen.Value} characters long.";
                    }

                    parsedArguments.Add(value);
                }
                else
                {
                    parsedArguments.Add(argument.Deserialize(parameter.ParameterType, JsonOptions));
                }
                continue;
            }

            if (parameter.IsOptional)
            {
                parsedArguments.Add(parameter.DefaultValue);
                continue;
            }

            var defaultAttribute = parameter.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultAttribute is not null)
            {
                parsedArguments.Add(defaultAttribute.Value);
                continue;
            }

            actionContext?.SetActionResult(isSuccess: false, $"Value missing for required parameter '{parameter.Name}'.");
            return $"Value missing for required parameter '{parameter.Name}'.";
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is null)
        {
            return "OK: Action executed successfully.";
        }

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult.GetType().IsGenericType && invocationResult.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            dynamic dynamicValueTask = invocationResult;
            invocationResult = await dynamicValueTask.ConfigureAwait(false);
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            invocationResult = null;
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

    /// <summary>
    /// Invokes a workflow condition delegate and returns whether execution is allowed plus an optional denial reason.
    /// </summary>
    /// <param name="callback">Delegate to invoke.</param>
    /// <param name="arguments">Dictionary containing JSON values for condition settings.</param>
    /// <param name="conditionContext">Optional condition context injected when the delegate accepts an <see cref="IConditionContext"/> parameter.</param>
    /// <param name="cancellationToken">Cancellation token injected when the delegate accepts a <see cref="CancellationToken"/> parameter.</param>
    /// <returns>
    /// A tuple where the first element indicates whether the condition passed, and the second is an optional failure reason.
    /// </returns>
    /// <remarks>
    /// Return conventions:
    /// <list type="bullet">
    /// <item><description>Return <see langword="true"/> to allow execution.</description></item>
    /// <item><description>Return <see langword="false"/> to deny execution without a reason.</description></item>
    /// <item><description>Return a string (or any other value) to deny execution with a reason (non-string values are JSON-serialized).</description></item>
    /// </list>
    /// <para>
    /// Missing required arguments cause this method to throw. Callers typically catch and convert this to a generic
    /// "invalid/malformed settings" error.
    /// </para>
    /// </remarks>
    public static async ValueTask<(bool, string?)> InvokeForConditionResultAsync(this Delegate callback, IReadOnlyDictionary<string, JsonElement> arguments, IConditionContext? conditionContext = null, CancellationToken cancellationToken = default)
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

            JsonElement argument = default;

            if (arguments.TryGetValue(parameter.Name!, out var argument1))
            {
                argument = argument1;
            }
            else if (arguments.TryGetValue(parameter.Name!.ToSnakeLower(), out var argument2))
            {
                argument = argument2;
            }

            if (argument.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            {
                parsedArguments.Add(argument.Deserialize(parameter.ParameterType));
                continue;
            }

            if (parameter.IsOptional)
            {
                parsedArguments.Add(parameter.DefaultValue);
                continue;
            }

            throw new Exception($"Value missing for required parameter '{parameter.Name}'.");
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);

        if (invocationResult is null)
        {
            return (false, null);
        }

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty is not null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }
        else if (invocationResult.GetType().IsGenericType && invocationResult.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            dynamic dynamicValueTask = invocationResult;
            invocationResult = await dynamicValueTask.ConfigureAwait(false);
        }
        else if (invocationResult is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            invocationResult = null;
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

    private static (int? minLength, int? maxLength) GetStringLengthConstraints(ParameterInfo parameter)
    {
        int? minLength = null;
        int? maxLength = null;

        if (parameter.GetCustomAttribute<MinLengthAttribute>() is { } minLengthAttribute)
        {
            minLength = minLengthAttribute.Length;
        }

        if (parameter.GetCustomAttribute<StringLengthAttribute>() is { } stringLengthAttribute)
        {
            if (stringLengthAttribute.MinimumLength > 0)
            {
                minLength = minLength.HasValue ? Math.Max(minLength.Value, stringLengthAttribute.MinimumLength) : stringLengthAttribute.MinimumLength;
            }

            maxLength = maxLength.HasValue ? Math.Min(maxLength.Value, stringLengthAttribute.MaximumLength) : stringLengthAttribute.MaximumLength;
        }

        if (parameter.GetCustomAttribute<MaxLengthAttribute>() is { } maxLengthAttribute)
        {
            maxLength = maxLength.HasValue ? Math.Min(maxLength.Value, maxLengthAttribute.Length) : maxLengthAttribute.Length;
        }

        return (minLength, maxLength);
    }
}
