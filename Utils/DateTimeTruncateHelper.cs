namespace EventBusLib.Utils;

using System;

public static class DateTimeTruncateHelper
{
    extension(DateTime dateTime)
    {
        public DateTime TruncateToSecond() => new(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);

        public DateTime TruncateToMinute() => new(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);

        public DateTime TruncateToHour() => new(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, 0, 0, dateTime.Kind);

        public DateTime TruncateToDay() => dateTime.Date;

        public DateTime TruncateToMonth() =>
            new(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);

        public DateTime TruncateToYear() => new(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
    }
}