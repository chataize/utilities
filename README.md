# ChatAIze.Utilities
Shared helpers and extensions used across the ChatAIze stack. This package is intentionally small and focused: it provides
string normalization, placeholder substitution, delegate invocation helpers for tool calls, and lightweight date parsing.

It is referenced by:
- `ChatAIze.Chatbot` (host runtime and dashboard)
- `ChatAIze.GenerativeCS` (tool schema and provider integration)
- `ChatAIze.PluginApi` (plugin authoring helpers)
- first-party plugins like `law-firm-plugin`

## Install
```bash
dotnet add package ChatAIze.Utilities
```

Target framework: `net10.0`. The package depends on `ChatAIze.Abstractions` for tool and workflow context types.

## What this package contains
Namespaces:
- `ChatAIze.Utilities` for standalone helpers (for example `DateTimeOffsetParser`).
- `ChatAIze.Utilities.Extensions` for extension methods used across the host and plugins.

Key modules:
- `StringExtension`: normalization, identifier casing, placeholder substitution.
- `CharExtension`: simple Latin transliteration for diacritics.
- `DictionaryExtensions`: tolerant JSON reads and placeholder expansion for dictionaries.
- `DelegateExtensions`: tool/action/condition invocation helpers with validation.
- `DateTimeOffsetExtension`: "natural" UI-friendly date formatting.
- `DateTimeOffsetParser`: small natural-language date/time parser.

## Where it is used in ChatAIze
Concrete examples from the current codebase:
- `ToSnakeLower` is used to normalize database titles and properties in `ChatAIze.Chatbot` and to generate tool schemas in
  `ChatAIze.GenerativeCS`.
- `NormalizedEquals` is used to match tool calls coming back from providers (OpenAI/Gemini) to registered functions.
- `WithPlaceholderValues` is used to expand action settings and confirmation text in the workflow engine.
- `TryGetSettingValue` is used in the action/condition UI builders to read settings with safe defaults.
- `ToNaturalString` is used in dashboard UI timestamps (chat cards, account metadata, backups).
- `DateTimeOffsetParser` is used in `law-firm-plugin` and in condition evaluation to parse date/time expressions.
- `DelegateExtensions` is the standard path for invoking tool functions, workflow actions, and conditions.

## Quick start
```csharp
using ChatAIze.Utilities.Extensions;

var normalized = "My Project Name".ToSnakeLower(); // "my_project_name"
var equal = "Sao Paulo".NormalizedEquals("Sao-Paulo"); // true

var template = "Hello {user.name}, your order is {order_id}.";
var placeholders = new Dictionary<string, string>
{
    ["UserName"] = "Marcel",
    ["order_id"] = "A-123"
};
var message = template.WithPlaceholderValues(placeholders);
```

## String and character normalization
`StringExtension` and `CharExtension` provide the normalization rules used throughout ChatAIze.

Highlights:
- `NormalizedEquals` compares strings by transliterating diacritics, ignoring punctuation/whitespace, and matching
  ASCII letters/digits case-insensitively.
- `ToSeparated`, `ToSnakeLower`, `ToKebabLower` normalize identifiers for use in tool schemas and settings keys.
- `ToAlphanumeric` strips non-ASCII characters and optionally preserves `-` or `_` (whitespace normalizes to the chosen separator).
- `ToLatin` maps a curated set of diacritics to ASCII-friendly characters.

Warnings:
- This is intentionally lossy. Do not use `NormalizedEquals` for security-sensitive comparisons (passwords, tokens).
- Strings containing only punctuation normalize to an empty value, so they compare equal.
- `ToSeparated` drops non-ASCII characters and splits on lower-to-upper transitions (for example "ChatBot" -> "chat_bot").

## Placeholder substitution
`WithPlaceholderValues` exists for string templates and for dictionaries that contain JSON values.

String templates:
- Placeholder keys are normalized to snake_case.
- Supported syntax includes `{key}`, `{key.sub_property}`, and `{key:sub_property}`.
- Replacement is plain text, no escaping is performed.

Dictionary placeholders:
- For `IReadOnlyDictionary<string, JsonElement>`, string values are replaced directly.
- For JSON objects/arrays, substitution occurs on raw JSON and then the JSON is reparsed.

Warnings:
- Placeholder replacement is not JSON-escaped. Make sure your placeholder values produce valid JSON after replacement.
- Placeholders are simple and do not evaluate expressions; they just substitute strings.

## Dictionary helpers
`DictionaryExtensions` is designed for settings dictionaries used by actions and conditions.

Key helpers:
- `TryGetSettingValue<T>`: tolerant JSON read that returns a default value on deserialization errors.
- `WithPlaceholderValues` overloads: apply placeholder substitution to strings and JSON data.

Tip: `TryGetSettingValue` is used heavily in `ChatAIze.Chatbot` action/condition builders to keep UI logic resilient to
missing or invalid settings values.

## Delegate invocation helpers
`DelegateExtensions` provides the standard binding and validation rules for ChatAIze tools and workflows.

Main features:
- `GetNormalizedMethodName` extracts stable names for delegates. Prefer named methods; lambdas can throw if the compiler
  name is not recognized.
- `InvokeForStringResultAsync` binds JSON arguments to function delegates, injects `IFunctionContext`/`CancellationToken`,
  and serializes non-string results to snake_case JSON.
- For actions, missing/invalid values mark the action as failed via `IActionContext.SetActionResult`.
- For conditions, missing required values throw; callers should validate settings before invocation.

Validation rules:
- `Required`, `MinLength`, `MaxLength`, `StringLength` attributes are enforced on string parameters.
- Enum parameters are parsed case-insensitively and tolerate underscores.

Warnings:
- Delegate exceptions are not swallowed; they propagate to the caller.
- Tool invocation returns `"Error: ..."` strings for validation failures (not exceptions).

## Date and time helpers
### `DateTimeOffsetExtension.ToNaturalString`
Formats timestamps for UI display:
- English output, 24-hour time.
- Uses a simple hour offset from UTC, not a time zone with DST rules.
- Returns values like `Today, 13:37`, `Yesterday`, `Mon, 15:00`, or `2025-01-31`.

### `DateTimeOffsetParser.Parse`
A lightweight parser for human-friendly date/time expressions.

Supported examples:
- `now`, `today`, `tomorrow`, `next monday at 14:30`
- `2025-01-31`, `31.01.2025`, `12/31/2025`
- `utc+2`, `gmt-5`, `cest`

Limitations and gotchas:
- It is not a general NLP parser. Catch exceptions for untrusted input.
- Parsing is anchored to `DateTimeOffset.UtcNow` and uses fixed offsets for time zones (no DST).
- Ambiguous formats like `01/02/2025` are interpreted as `month/day/year`.
- Day underflow (for example "yesterday" on the 1st) is not normalized and can throw.
- The `at` keyword is detected by substring and can match inside other words.
- A small Polish-to-English keyword map is applied after `ToLatin()` normalization.

## Preview project
`ChatAIze.Utilities.Preview` is a tiny console app used to demo placeholder replacement with `JsonElement` values.
It is not published as a package.

## Related packages
- `ChatAIze.Abstractions`: base interfaces referenced by `DelegateExtensions`.
- `ChatAIze.PluginApi`: concrete helper implementations for plugin authors.
- `ChatAIze.GenerativeCS`: tool schema generation, uses `ToSnakeLower` and `NormalizedEquals`.

## License
GPL-3.0-or-later. See `LICENSE` for details.
