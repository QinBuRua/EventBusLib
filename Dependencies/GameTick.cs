using EventBusLib.Utils;

namespace EventBusLib.Dependencies;

public partial struct GameTick : IComparable<GameTick>, IEquatable<GameTick>
{
    public long Ticks { get; set; }

    public static DateTime BeginTime { get; } = DateTime.Now.TruncateToSecond();

    public static GameTick operator +(GameTick a, GameTick b) => new GameTick(a.Ticks + b.Ticks);
    public static GameTick operator -(GameTick a, GameTick b) => new GameTick(a.Ticks - b.Ticks);

    public static GameTick operator +(GameTick value) => value;
    public static GameTick operator -(GameTick value) => new GameTick(-value.Ticks);

    public static GameTick operator *(GameTick tick, long multiplier) => new GameTick(tick.Ticks * multiplier);
    public static GameTick operator *(long multiplier, GameTick tick) => new GameTick(tick.Ticks * multiplier);

    public static bool operator ==(GameTick left, GameTick right) => left.Ticks == right.Ticks;
    public static bool operator !=(GameTick left, GameTick right) => left.Ticks != right.Ticks;
    public static bool operator <(GameTick left, GameTick right) => left.Ticks < right.Ticks;
    public static bool operator >(GameTick left, GameTick right) => left.Ticks > right.Ticks;
    public static bool operator <=(GameTick left, GameTick right) => left.Ticks <= right.Ticks;
    public static bool operator >=(GameTick left, GameTick right) => left.Ticks >= right.Ticks;

    int IComparable<GameTick>.CompareTo(GameTick other) => Ticks.CompareTo(other.Ticks);
    bool IEquatable<GameTick>.Equals(GameTick other) => Ticks == other.Ticks;
    public override bool Equals(object? obj) => obj is GameTick other && Equals(other);
    public override int GetHashCode() => Ticks.GetHashCode();

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
        => Ticks = ticks;

    public static implicit operator GameTick(long ticks)
        => new GameTick(ticks);
}