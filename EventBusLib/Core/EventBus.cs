using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConcurrentCollections;
using EventBusLib.Dependencies;
using EventBusLib.Exceptions;
using EventBusLib.Extensions;
using EventBusLib.Utils;

namespace EventBusLib.Core;

public partial class EventBus //todo: 线程安全
{
    public partial EventBus();

    public partial uint DefaultMaxPushEventCount { get; set; } = 32;

    public partial void Clear();
    public partial long SubscriberCount { get; }
    public partial EventCountSetting EventCount { get; }

    public partial bool ContainsSubscriber(ISubscriber subscriber);

    public partial void PushEvent<TEvent>(TEvent @event)
        where TEvent : Event;

    public partial SubscriberToken AddSubscriber(ISubscriber subscriber);
    
    public partial bool TryRemoveSubscriber(ISubscriber subscriber, out Exception? exception);

    public partial void TryLoopOnce();

    public partial record struct EventCountSetting(long Delay, long Alive)
    {
        public partial long Total { get; }
    }

    public partial record struct SubscriberCountSetting(long Managed, long Total)
    {
        public partial long Unmanaged { get; }
    }
}