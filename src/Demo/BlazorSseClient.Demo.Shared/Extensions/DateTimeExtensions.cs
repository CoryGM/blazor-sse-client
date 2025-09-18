using System;
using System.Runtime.CompilerServices;

namespace BlazorSseClient.Demo.Shared.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToReadableDuration(this DateTime dateTime, 
            DateTime? targetDate = null, int depth = 2, LabelStyle style = LabelStyle.Long)
        {
            targetDate ??= DateTime.Now;

            return ToReadableDurationInternal(dateTime, targetDate.Value, depth, style);
        }

        /// <summary>
        /// Converts a TimeSpan into a display string that accounts for the
        /// relative value of the TimeSpan. For example, if the TimeSpan is null,
        /// it returns "N/A". If the TimeSpan is less than a minute, it returns
        /// the number of seconds. If the TimeSpan is less than an hour, it returns
        /// the minutes and seconds. If the TimeSpan is more than a day, it returns
        /// the hours, minutes. If the TimeSpan is more than a week, it returns the weeks
        /// days. If the TimeSpan is more than a month, it returns the months and days.
        /// If the TimeSpan is more than a year, it returns the years, months and days.
        /// If the TimeSpan is more than a century, it returns the centuries, years and days.
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="longUnits"> If true, uses long unit names (e.g. "years" instead of "y").</param>
        /// <param name="maxDetail">
        ///     The maximum number of time units to include in the display string. 
        ///     For example, if the TimeSpan is 1 year, 2 months, 3 days, 4 hours, 
        ///     5 minutes and 6 seconds, and maxDetail is 2, the display string will 
        ///     be "1 year, 2 months".</param>
        /// <returns></returns>
        private static string ToReadableDurationInternal(DateTime startDate, DateTime endDate, int depth = 2, LabelStyle style = LabelStyle.Long)
        {
            var components = new List<(int value, string longLabel, string shortLabel)>();

            if (startDate == endDate)
                return style == LabelStyle.Long ? "0 Seconds" : "0s";

            var workingEndDate = endDate;
            var workingStartDate = startDate;

            if (workingEndDate < workingStartDate)
                (workingStartDate, workingEndDate) = (workingEndDate, workingStartDate);

            // Centuries
            int centuries = (workingEndDate.Year - workingStartDate.Year) / 100;

            if (centuries > 0)
            {
                components.Add((centuries, $"centur{(centuries > 1 ? "ies" : "y")}", "c"));
                workingEndDate = workingEndDate.AddYears(centuries * 100 * -1);
            }

            // Years
            int years = workingEndDate.Year - workingStartDate.Year;

            if (years > 0)
            {
                components.Add((years, $"year{(years > 1 ? "s" : "")}", "y"));
                workingEndDate = workingEndDate.AddYears(years * -1);
            }

            // Months
            int months = 0;

            while (workingEndDate.AddMonths(-1) > workingStartDate)
            {
                workingEndDate = workingEndDate.AddMonths(-1);
                months++;
            }

            if (months > 0)
                components.Add((months, $"month{(months > 1 ? "s" : "")}", "m"));

            // Days
            int days = 0;

            while (workingEndDate.AddDays(-1) > workingStartDate)
            {
                workingEndDate = workingEndDate.AddDays(-1);
                days++;
            }

            if (days > 0)
                components.Add((days, $"day{(days > 1 ? "s" : "")}", "d"));

            // Hours
            int hours = 0;

            while (workingEndDate.AddHours(-1) > workingStartDate)
            {
                workingEndDate = workingEndDate.AddHours(-1);
                hours++;
            }

            if (hours > 0)
                components.Add((hours, $"hour{(hours > 1 ? "s" : "")}", "h"));

            // Minutes
            int minutes = 0;

            while (workingEndDate.AddMinutes(-1) > workingStartDate)
            {
                workingEndDate = workingEndDate.AddMinutes(-1);
                minutes++;
            }
            if (minutes > 0)
                components.Add((minutes, $"minute{(minutes > 1 ? "s" : "")}", "min"));

            // Seconds
            int seconds = (workingEndDate - workingStartDate).Seconds;

            if (seconds > 0 || components.Count == 0)
                components.Add((seconds, $"second{(seconds > 1 ? "s" : "")}", "s"));

            return string.Join(", ",
                components.Take(depth)
                          .Select(c => $"{c.value}{(style == LabelStyle.Long ? $" {c.longLabel}" : c.shortLabel)}"));
        }
    }
}