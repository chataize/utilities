namespace ChatAIze.Utilities;

public static class DateTimeOffsetParser
{
    public static DateTimeOffset Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTimeOffset.UtcNow.Date;
        }

        var lowerValue = value.ToLowerInvariant();

        if (lowerValue is "now" or "today")
        {
            return DateTimeOffset.UtcNow;
        }

        if (lowerValue is "yesterday")
        {
            return DateTimeOffset.UtcNow.AddDays(-1);
        }

        if (lowerValue is "tomorrow")
        {
            return DateTimeOffset.UtcNow.AddDays(1);
        }

        if (DateTimeOffset.TryParse(value, out var parsedOffset))
        {
            return parsedOffset;
        }

        if (DateTime.TryParse(value, out var parsedDateTime))
        {
            return parsedDateTime;
        }

        if (TimeOnly.TryParse(value, out var parsedTime))
        {
            return DateTimeOffset.UtcNow.Date.Add(parsedTime.ToTimeSpan());
        }

        if (DateOnly.TryParse(value, out var parsedDate))
        {
            return new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, DateTimeOffset.UtcNow.Hour, DateTimeOffset.UtcNow.Minute, DateTimeOffset.UtcNow.Second, TimeSpan.Zero);
        }

        throw new ArgumentException("Invalid date/time format", nameof(value));
    }
}
