using EventBusLib.Dependencies;

namespace EventBusLib.Core;

public class Event
{
    public GameTick CreateTime
    {
        get;
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    } = GameTick.Now;

    public GameTick MaxDelay
    {
        get;
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    } = DefaultMaxDelay;

    public GameTick PushDelay
    {
        get;
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    } = 0;

    public static GameTick DefaultMaxDelay
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    } = 10;
}