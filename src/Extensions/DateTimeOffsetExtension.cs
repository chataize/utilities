namespace ChatAIze.Utilities.Extensions;

public static class DateTimeOffsetExtension
{
    public static string ToNaturalString(this DateTimeOffset time, int offset = 0, bool includeTime = true)
    {
        return includeTime ? time.ToNaturalDateTimeString(offset) : time.ToNaturalDateOnlyString(offset);
    }

    private static string ToNaturalDateTimeString(this DateTimeOffset time, int offset = 0)
    {
        var targetTime = time.AddHours(offset);
        var currentTime = DateTimeOffset.UtcNow.AddHours(offset);

        if (targetTime.Date == currentTime.Date)
        {
            return targetTime > currentTime ? $"Today, {targetTime:HH:mm}" : targetTime.ToString("HH:mm");
        }

        if (targetTime.Date == currentTime.Date.AddDays(-1))
        {
            return $"Yesterday, {targetTime:HH:mm}";
        }

        if (targetTime.Date == currentTime.Date.AddDays(1))
        {
            return $"Tomorrow, {targetTime:HH:mm}";
        }

        if (targetTime.Date >= currentTime.Date.AddDays(-7) && targetTime.Date <= currentTime.Date.AddDays(7))
        {
            return targetTime.ToString("ddd, HH:mm");
        }

        if (targetTime.Year == currentTime.Year)
        {
            return targetTime.ToString("MMM dd, HH:mm");
        }

        return targetTime.ToString("yyyy-MM-dd, HH:mm");
    }

    private static string ToNaturalDateOnlyString(this DateTimeOffset time, int offset = 0)
    {
        var targetTime = time.AddHours(offset);
        var currentTime = DateTimeOffset.UtcNow.AddHours(offset);

        if (targetTime.Date == currentTime.Date)
        {
            return "Today";
        }

        if (targetTime.Date == currentTime.Date.AddDays(-1))
        {
            return "Yesterday";
        }

        if (targetTime.Date == currentTime.Date.AddDays(1))
        {
            return "Tomorrow";
        }

        if (targetTime.Date >= currentTime.Date.AddDays(-7) && targetTime.Date <= currentTime.Date.AddDays(7))
        {
            return targetTime.ToString("ddd");
        }

        if (targetTime.Year == currentTime.Year)
        {
            return targetTime.ToString("MMM dd");
        }

        return targetTime.ToString("yyyy-MM-dd");
    }
}
