using System.Diagnostics.CodeAnalysis;

namespace EventBusLib.Core;

public partial class EventBus //todo: 线程安全
{
    public partial EventBus();

    public partial uint DefaultMaxPushEventCount { get; set; } = 32;

    public partial void Clear();
    public partial long SubscriberCount { get; }
    public partial EventCountSetting EventCount { get; }

    public record struct SubscriberTokenExceptionPair(SubscriberToken SubscriberToken, Exception Exception);

    public partial record struct OnLoopExceptionSettings(
        List<SubscriberTokenExceptionPair>? OnCheckAliveExceptions,
        List<SubscriberTokenExceptionPair>? OnHandleExceptions,
        List<SubscriberTokenExceptionPair>? OnDestroyExceptions)
    {
        public partial bool IsEmpty();

        public partial bool TryGetOnCheckAliveExceptions(out List<SubscriberTokenExceptionPair>? onCheckAliveExceptions);
        public partial bool TryGetOnHandleExceptions(out List<SubscriberTokenExceptionPair>? onHandleExceptions);
        public partial bool TryGetOnDestroyExceptions(out List<SubscriberTokenExceptionPair>? onDestroyExceptions);
    }

    /// <summary>
    /// Check whether the subscriber exists in this EventBus.
    /// </summary>
    /// <param name="subscriber">the subscriber you want to check, can be null.</param>
    /// <returns>
    /// <c>true</c> means it exists.
    /// <c>false</c> means it does NOT exist, or <paramref name="subscriber"/> is null.
    /// </returns>
    public partial bool ContainsSubscriber(ISubscriber subscriber);

    public partial void PushEvent<TEvent>(TEvent @event)
        where TEvent : Event;

    public partial SubscriberToken AddSubscriber(ISubscriber subscriber);

    public partial bool TryRemoveSubscriber(ISubscriber subscriber, out Exception? exception);


    public partial record struct EventCountSetting(long Delay, long Alive)
    {
        public partial long Total { get; }
    }

    public partial record struct SubscriberCountSetting(long Managed, long Total)
    {
        public partial long Unmanaged { get; }
    }
}
