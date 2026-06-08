using EventBusLib.Dependencies;

namespace EventBusLib.Core;

public class Event
{
    public GameTick CreateTime { get; init; } = GameTick.Now;
    public GameTick PushDelay { get; init; } = 0;
    public GameTick MaxDelay { get; init; } = DefaultMaxDelay;

    public static GameTick DefaultMaxDelay { get; set; } = 10;
}