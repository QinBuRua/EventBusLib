namespace EventBusLib.Utils;

public class DateTimeHelper
{
    public static DateTime TruncateToSeconds(DateTime dateTime)
        => dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
}