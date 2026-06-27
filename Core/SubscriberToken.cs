using System.Diagnostics.CodeAnalysis;

namespace EventBusLib.Core;

public readonly partial record struct SubscriberToken(
    WeakReference<EventBus> EventBus,
    WeakReference<ISubscriber> Subscriber
) : IDisposable
{
    public SubscriberToken(EventBus bus, ISubscriber subscriber)
        : this(new WeakReference<EventBus>(bus), new WeakReference<ISubscriber>(subscriber))
    {
    }

    public partial bool IsAvailable { get; }
    public partial void Dispose();
    public partial bool TryGetBusAndSubscriber([NotNullWhen(true)] out EventBus? eventBus, [NotNullWhen(true)] out ISubscriber? subscriber);
}
