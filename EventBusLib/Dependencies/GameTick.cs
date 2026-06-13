using EventBusLib.Utils;

namespace EventBusLib.Dependencies;

public struct GameTick : IComparable<GameTick>, IEquatable<GameTick>
{
    public long Ticks { get; set; }

    public GameTick(long ticks)
    {
        Ticks = ticks;
    }

    public GameTick(DateTime dateTime)
    {
        Ticks = DateTimeTicksToGameTicks(dateTime.Ticks - BeginTime.Ticks);
    }

    public static GameTick Now => new(DateTime.Now);

    public static implicit operator GameTick(long ticks) => new(ticks);
    public static implicit operator long(in GameTick ticks) => ticks.Ticks;

    public static bool operator ==(in GameTick left, in GameTick right) => left.Ticks == right.Ticks;
    public static bool operator !=(in GameTick left, in GameTick right) => left.Ticks != right.Ticks;
    public static bool operator <(in GameTick left, in GameTick right) => left.Ticks < right.Ticks;
    public static bool operator >(in GameTick left, in GameTick right) => left.Ticks > right.Ticks;
    public static bool operator <=(in GameTick left, in GameTick right) => left.Ticks <= right.Ticks;
    public static bool operator >=(GameTick left, GameTick right) => left.Ticks >= right.Ticks;

    public static GameTick operator +(in GameTick ticks, in GameTick otherTicks) => new(ticks.Ticks + otherTicks.Ticks);
    public static GameTick operator -(in GameTick ticks, in GameTick otherTicks) => new(ticks.Ticks - otherTicks.Ticks);
    public static GameTick operator -(in GameTick ticks) => new(-ticks.Ticks);
    public static GameTick operator +(in GameTick ticks) => new(ticks.Ticks);

    public static GameTick operator ++(in GameTick ticks) => new(ticks.Ticks + 1);
    public static GameTick operator --(in GameTick ticks) => new(ticks.Ticks - 1);

    private static long DateTimeTicksToGameTicks(long dateTimeTicks)
        => dateTimeTicks / 20 * 10000;

    public static DateTime BeginTime { get; } = DateTimeHelper.TruncateToSeconds(DateTime.Now);

    public int CompareTo(GameTick other)
    {
        return Ticks.CompareTo(other.Ticks);
    }

    public bool Equals(GameTick other)
    {
        return Ticks == other.Ticks;
    }

    public override bool Equals(object? obj)
    {
        return obj is GameTick other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Ticks.GetHashCode();
    }
}