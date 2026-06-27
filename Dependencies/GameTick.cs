using EventBusLib.Utils;

namespace EventBusLib.Dependencies;

public partial struct GameTick : IComparable<GameTick>, IEquatable<GameTick>
{
    public long Ticks { get; set; }

    public static DateTime BeginTime { get; } = DateTime.Now.TruncateToSecond();

    public static GameTick operator +(GameTick a, GameTick b)
    {
        return new GameTick(a.Ticks + b.Ticks);
    }

    public static GameTick operator -(GameTick a, GameTick b)
    {
        return new GameTick(a.Ticks - b.Ticks);
    }

    public static GameTick operator +(GameTick value)
    {
        return value;
    }

    public static GameTick operator -(GameTick value)
    {
        return new GameTick(-value.Ticks);
    }

    public static GameTick operator *(GameTick tick, long multiplier)
    {
        return new GameTick(tick.Ticks * multiplier);
    }

    public static GameTick operator *(long multiplier, GameTick tick)
    {
        return new GameTick(tick.Ticks * multiplier);
    }

    public static bool operator ==(GameTick left, GameTick right)
    {
        return left.Ticks == right.Ticks;
    }

    public static bool operator !=(GameTick left, GameTick right)
    {
        return left.Ticks != right.Ticks;
    }

    public static bool operator <(GameTick left, GameTick right)
    {
        return left.Ticks < right.Ticks;
    }

    public static bool operator >(GameTick left, GameTick right)
    {
        return left.Ticks > right.Ticks;
    }

    public static bool operator <=(GameTick left, GameTick right)
    {
        return left.Ticks <= right.Ticks;
    }

    public static bool operator >=(GameTick left, GameTick right)
    {
        return left.Ticks >= right.Ticks;
    }

    int IComparable<GameTick>.CompareTo(GameTick other)
    {
        return Ticks.CompareTo(other.Ticks);
    }

    bool IEquatable<GameTick>.Equals(GameTick other)
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

    public static long ScaleFactor
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    } = 20 * 10000;

    public GameTick(DateTime dateTime)
    {
        var pastTicks = dateTime.Ticks - BeginTime.Ticks;
        Ticks = pastTicks / ScaleFactor;
    }

    public GameTick(long ticks)
    {
        Ticks = ticks;
    }

    public static implicit operator GameTick(long ticks)
    {
        return new GameTick(ticks);
    }
}
