using EventBusLib.Dependencies;
using EventBusLib.Exceptions;

namespace EventBusLib.Core;

public partial class EventBus //todo: 线程安全
{
    public partial EventBus();

    public partial uint DefaultMaxPushEventCount { get; set; } = 32;
    public partial long SubscriberCount { get; }
    public partial EventCountSetting EventCount { get; }

    public partial void Clear();

    public partial bool ContainsSubscriber(ISubscriber subscriber);

    public partial void PushEvent<TEvent>(TEvent @event)
        where TEvent : Event;

    public partial SubscriberToken AddSubscriber(ISubscriber subscriber);

    public partial bool TryRemoveSubscriber(ISubscriber subscriber, out Exception? onDestroyException);
    public partial void DisposeSubscriber(ISubscriber subscriber);

    /// <summary>
    ///     Attempts to execute a single processing loop iteration for the event bus up to the specified game tick.
    /// </summary>
    /// <param name="nowTick">The current game tick to use for determining which events to process.</param>
    /// <param name="subscriberInnerExceptions">
    ///     When this method returns <c>false</c>, contains any exceptions that occurred
    ///     during the loop execution; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the loop completed without exceptions; otherwise, <c>false</c> and exceptions are provided via
    ///     <paramref name="subscriberInnerExceptions" />.
    /// </returns>
    public partial bool TryLoopOnce(GameTick nowTick, out List<SubscriberInnerException> subscriberInnerExceptions);

    public record struct SubscriberTokenExceptionPair(SubscriberToken SubscriberToken, Exception Exception);


    public partial record struct EventCountSetting(long Delay, long Alive)
    {
        public partial long Total { get; }
    }

    public partial record struct SubscriberCountSetting(long Managed, long Total)
    {
        public partial long Unmanaged { get; }
    }
}
