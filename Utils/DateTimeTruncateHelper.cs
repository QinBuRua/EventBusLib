namespace EventBusLib.Utils;

public static class DateTimeTruncateHelper
{
    extension(DateTime dateTime)
    {
        public DateTime TruncateToSecond()
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
        }

        public DateTime TruncateToMinute()
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
        }

        public DateTime TruncateToHour()
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public DateTime TruncateToDay()
        {
            return dateTime.Date;
        }

        public DateTime TruncateToMonth()
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
        }

        public DateTime TruncateToYear()
        {
            return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
        }
    }
}
