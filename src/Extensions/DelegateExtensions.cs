namespace ChatAIze.Utilities.Extensions;

public static class DelegateExtensions
{
    public static string GetNormalizedMethodName(this Delegate @delegate)
    {
        var name = @delegate.Method.Name;

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
}
