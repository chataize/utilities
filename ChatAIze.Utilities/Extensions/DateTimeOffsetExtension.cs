namespace ChatAIze.Utilities.Extensions;

/// <summary>
/// Date/time formatting helpers used by ChatAIze UI components.
/// </summary>
/// <remarks>
/// These helpers are designed for human-friendly display (English strings, 24-hour time) and are intentionally simple.
/// <para>
/// The <c>offset</c> parameters are expressed as whole hours from UTC and do not model daylight saving time transitions.
/// </para>
/// </remarks>
public static class DateTimeOffsetExtension
{
    /// <summary>
    /// Formats <paramref name="time"/> into a human-friendly, relative string (for example: <c>"Today, 13:37"</c>).
    /// </summary>
    /// <param name="time">The time value to format.</param>
    /// <param name="offset">Hour offset from UTC to apply before formatting.</param>
    /// <param name="includeTime">When <see langword="true"/>, includes the time-of-day where applicable.</param>
    /// <returns>A natural language representation of the timestamp.</returns>
    /// <remarks>
    /// Formatting rules:
    /// <list type="bullet">
    /// <item><description>Same day: <c>"HH:mm"</c> (or <c>"Today, HH:mm"</c> for future times).</description></item>
    /// <item><description>Yesterday/Tomorrow: <c>"Yesterday, HH:mm"</c> / <c>"Tomorrow, HH:mm"</c> (or <c>"Yesterday"</c>/<c>"Tomorrow"</c> if <paramref name="includeTime"/> is false).</description></item>
    /// <item><description>Within ±7 days: day-of-week abbreviation (<c>"Mon"</c>) with optional time.</description></item>
    /// <item><description>Same year: <c>"MMM dd"</c> with optional time.</description></item>
    /// <item><description>Different year: <c>"yyyy-MM-dd"</c> with optional time.</description></item>
    /// </list>
    /// </remarks>
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
