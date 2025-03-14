using System.Text;
using System.Text.RegularExpressions;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.Utilities;

public static partial class DateTimeOffsetParser
{
    public static DateTimeOffset Parse(string s)
    {
        s = TranslateTime(s);

        if (DateTimeOffset.TryParse(s, out var result))
        {
            return result;
        }

        if (s == "now")
        {
            return DateTimeOffset.UtcNow;
        }

        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var day = DateTime.UtcNow.Day;
        var hour = DateTime.UtcNow.Hour;
        var minute = 0;
        var second = 0;
        var offset = TimeSpan.Zero;

        var yearMatch = YearRegex().Match(s);
        if (yearMatch.Success)
        {
            year = int.Parse(yearMatch.Value);
        }

        var atIndex = s.IndexOf("at");
        var beforeAt = s;

        if (atIndex != -1)
        {
            beforeAt = s[..atIndex];

            var afterAt = s[(atIndex + 2)..];
            var numbers = NumbersRegex().Matches(afterAt);

            if (numbers.Count > 0)
            {
                hour = int.Parse(numbers[0].Value);
            }

            if (numbers.Count > 1)
            {
                minute = int.Parse(numbers[1].Value);
            }

            if (numbers.Count > 2)
            {
                second = int.Parse(numbers[2].Value);
            }
        }

        var dayMatch = DayRegex().Match(beforeAt);
        if (dayMatch.Success)
        {
            day = int.Parse(dayMatch.Value);
        }

        var isLast = s.Contains("last");
        var isNext = s.Contains("next");

        var dayNames = new Dictionary<string, DayOfWeek>
        {
            { "monday", DayOfWeek.Monday },
            { "tuesday", DayOfWeek.Tuesday },
            { "wednesday", DayOfWeek.Wednesday },
            { "thursday", DayOfWeek.Thursday },
            { "friday", DayOfWeek.Friday },
            { "saturday", DayOfWeek.Saturday },
            { "sunday", DayOfWeek.Sunday },
            { "weekend", DayOfWeek.Saturday }
        };

        foreach (var value in dayNames)
        {
            if (s.Contains(value.Key))
            {
                var current = (int)DateTime.UtcNow.DayOfWeek;
                var target = (int)value.Value;
                var difference = target - current;

                if (isLast)
                {
                    day = DateTime.UtcNow.Day + difference - 7;
                }
                else if (isNext)
                {
                    day = DateTime.UtcNow.Day + difference + 7;
                }
                else
                {
                    day = DateTime.UtcNow.Day + difference;
                }

                break;
            }
        }

        var slashDateMatch = SlashDateRegex().Match(s);
        if (slashDateMatch.Success)
        {
            var parts = slashDateMatch.Value.Split('/');

            day = int.Parse(parts[1]);
            month = int.Parse(parts[0]);
            year = int.Parse(parts[2]);
        }

        var shortDotDateMatch = ShortDotDateRegex().Match(s);
        if (shortDotDateMatch.Success)
        {
            var parts = shortDotDateMatch.Value.Split('.');

            day = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
        }

        var dotDateMatch = DotDateRegex().Match(s);
        if (dotDateMatch.Success)
        {
            var parts = dotDateMatch.Value.Split('.');

            day = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
            year = int.Parse(parts[2]);
        }

        var reverseDotDateMatch = ReverseDotDateRegex().Match(s);
        if (reverseDotDateMatch.Success)
        {
            var parts = reverseDotDateMatch.Value.Split('.');

            day = int.Parse(parts[2]);
            month = int.Parse(parts[1]);
            year = int.Parse(parts[0]);
        }

        var hyphenatedDateMatch = HyphenatedDateRegex().Match(s);
        if (hyphenatedDateMatch.Success)
        {
            var parts = hyphenatedDateMatch.Value.Split('-');

            year = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
            day = int.Parse(parts[2]);
        }

        var reverseHyphenatedDateMatch = ReverseHyphenatedDateRegex().Match(s);
        if (reverseHyphenatedDateMatch.Success)
        {
            var parts = reverseHyphenatedDateMatch.Value.Split('-');

            day = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
            year = int.Parse(parts[2]);
        }

        if (s.Contains("january") || s.Contains("jan"))
        {
            month = 1;
        }

        if (s.Contains("february") || s.Contains("feb"))
        {
            month = 2;
        }

        if (s.Contains("march") || s.Contains("mar"))
        {
            month = 3;
        }

        if (s.Contains("april") || s.Contains("apr"))
        {
            month = 4;
        }

        if (s.Contains("may") || s.Contains("may"))
        {
            month = 5;
        }

        if (s.Contains("june") || s.Contains("jun"))
        {
            month = 6;
        }

        if (s.Contains("july") || s.Contains("jul"))
        {
            month = 7;
        }

        if (s.Contains("august") || s.Contains("aug"))
        {
            month = 8;
        }

        if (s.Contains("september") || s.Contains("sep"))
        {
            month = 9;
        }

        if (s.Contains("october") || s.Contains("oct"))
        {
            month = 10;
        }

        if (s.Contains("november") || s.Contains("nov"))
        {
            month = 11;
        }

        if (s.Contains("december") || s.Contains("dec"))
        {
            month = 12;
        }

        if (s.Contains("1st"))
        {
            day = 1;
        }

        if (s.Contains("2nd"))
        {
            day = 2;
        }

        if (s.Contains("3rd"))
        {
            day = 3;
        }

        var nthDayMatch = NthDayRegex().Match(s);
        if (nthDayMatch.Success)
        {
            day = int.Parse(nthDayMatch.Value[..^2]);
        }

        if (s.Contains("yesterday"))
        {
            day = DateTime.UtcNow.Day - 1;
        }

        if (s.Contains("today"))
        {
            day = DateTime.UtcNow.Day;
        }

        if (s.Contains("tomorrow"))
        {
            day = DateTime.UtcNow.Day + 1;
        }

        if (s.Contains("morning"))
        {
            hour = 8;
            minute = 0;
            second = 0;
        }

        if (s.Contains("noon"))
        {
            hour = 12;
            minute = 0;
            second = 0;
        }

        if (s.Contains("afternoon"))
        {
            hour = 14;
            minute = 0;
            second = 0;
        }

        if (s.Contains("evening"))
        {
            hour = 18;
            minute = 0;
            second = 0;
        }

        if (s.Contains("night"))
        {
            hour = 22;
            minute = 0;
            second = 0;
        }

        if (s.Contains("midnight"))
        {
            hour = 0;
            minute = 0;
            second = 0;
        }

        var timeMatch = TimeRegex().Match(s);
        if (timeMatch.Success)
        {
            var parts = timeMatch.Value.Split(':');

            hour = int.Parse(parts[0]);
            minute = int.Parse(parts[1]);

            if (parts.Length == 3)
            {
                second = int.Parse(parts[2]);
            }
        }

        if (s.Contains(" am") && hour == 12)
        {
            hour = 0;
        }

        if (s.Contains(" pm") && hour < 12)
        {
            hour += 12;
        }

        if (s.Contains(" utc") || s.Contains(" gmt"))
        {
            offset = TimeSpan.Zero;
        }

        if (s.Contains(" pst"))
        {
            offset = TimeSpan.FromHours(-8);
        }

        if (s.Contains(" mst"))
        {
            offset = TimeSpan.FromHours(-7);
        }

        if (s.Contains(" cst"))
        {
            offset = TimeSpan.FromHours(-6);
        }

        if (s.Contains(" est"))
        {
            offset = TimeSpan.FromHours(-5);
        }

        if (s.Contains(" bst"))
        {
            offset = TimeSpan.FromHours(1);
        }

        if (s.Contains(" cest"))
        {
            offset = TimeSpan.FromHours(2);
        }

        var gmtMatch = GmtRegex().Match(s);
        if (gmtMatch.Success)
        {
            var offsetString = gmtMatch.Value[3..];
            var offsetHours = int.Parse(offsetString);

            offset = TimeSpan.FromHours(offsetHours);
        }

        var utcMatch = UtcRegex().Match(s);
        if (utcMatch.Success)
        {
            var offsetString = utcMatch.Value[3..];
            var offsetHours = int.Parse(offsetString);

            offset = TimeSpan.FromHours(offsetHours);
        }

        while (true)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (day > daysInMonth)
            {
                day -= daysInMonth;
                month++;
                if (month > 12)
                {
                    month -= 12;
                    year++;
                }
            }
            else
            {
                break;
            }
        }

        return new DateTimeOffset(year, month, day, hour, minute, second, offset);
    }

    private static string TranslateTime(string s)
    {
        s = s.Trim().ToLowerInvariant().ToLatin();

        var map = new Dictionary<string, string>
        {
            ["styczen"] = "january",
            ["luty"] = "february",
            ["marzec"] = "march",
            ["kwiecien"] = "april",
            ["maj"] = "may",
            ["czerwiec"] = "june",
            ["lipiec"] = "july",
            ["sierpien"] = "august",
            ["wrzesien"] = "september",
            ["pazdziernik"] = "october",
            ["listopad"] = "november",
            ["grudzien"] = "december",
            ["poniedzialek"] = "monday",
            ["wtorek"] = "tuesday",
            ["sroda"] = "wednesday",
            ["srode"] = "wednesday",
            ["czwartek"] = "thursday",
            ["piatek"] = "friday",
            ["sobota"] = "saturday",
            ["sobote"] = "saturday",
            ["niedziela"] = "sunday",
            ["niedziele"] = "sunday",
            ["wczoraj"] = "yesterday",
            ["dzisiaj"] = "today",
            ["dzis"] = "today",
            ["jutro"] = "tomorrow",
            ["rano"] = "morning",
            ["poludnie"] = "noon",
            ["poludnia"] = "noon",
            ["poludniu"] = "noon",
            ["wieczor"] = "evening",
            ["noc"] = "night",
            ["polnoc"] = "midnight",
            ["kolo"] = "at",
            ["okolo"] = "at",
            ["w okolicy"] = "at",
            ["przed"] = " at ",
            ["o"] = " at ",
            ["po"] = " at",
            ["teraz"] = "now",
            ["ostatni"] = "last",
            ["ostatna"] = "last",
            ["poprzedni"] = "last",
            ["poprzedna"] = "last",
            ["nastepny"] = "next",
            ["nastepna"] = "next",
            ["przyszly"] = "next",
            ["przyszla"] = "next"
        };

        var pattern = @"\b(" + string.Join("|", map.Keys.Select(Regex.Escape)) + @")\b";
        s = Regex.Replace(s, pattern, match => map[match.Value], RegexOptions.IgnoreCase);

        return s;
    }

    [GeneratedRegex(@"\b\d{1,2}/\d{1,2}/\d{4}\b")]
    private static partial Regex SlashDateRegex();

    [GeneratedRegex(@"\b\d{1,2}\.\d{1,2}\b")]
    private static partial Regex ShortDotDateRegex();

    [GeneratedRegex(@"\b\d{1,2}\.\d{1,2}\.\d{4}\b")]
    private static partial Regex DotDateRegex();

    [GeneratedRegex(@"\b\d{4}\.\d{1,2}\.\d{1,2}\b")]
    private static partial Regex ReverseDotDateRegex();

    [GeneratedRegex(@"\b\d{4}-\d{1,2}-\d{1,2}\b")]
    private static partial Regex HyphenatedDateRegex();

    [GeneratedRegex(@"\b\d{1,2}-\d{1,2}-\d{4}\b")]
    private static partial Regex ReverseHyphenatedDateRegex();

    [GeneratedRegex(@"\b\d{4}\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b\d{1,2}\b")]
    private static partial Regex DayRegex();

    [GeneratedRegex(@"\b\d{1,2}(st|nd|rd|th)\b")]
    private static partial Regex NthDayRegex();

    [GeneratedRegex(@"\b\d{1,2}:\d{1,2}(:\d{1,2})?\b")]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"gmt[+-]\d{1,2}")]
    private static partial Regex GmtRegex();

    [GeneratedRegex(@"utc[+-]\d{1,2}")]
    private static partial Regex UtcRegex();

    [GeneratedRegex(@"\d{1,2}")]
    private static partial Regex NumbersRegex();
}
