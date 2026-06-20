namespace EventBusLib.Core;

public readonly record struct SubscriberToken(
    WeakReference<EventBus> EventBus,
    WeakReference<ISubscriber> Subscriber
) : IDisposable
{
    public SubscriberToken(EventBus bus, ISubscriber subscriber)
        : this(new WeakReference<EventBus>(bus), new WeakReference<ISubscriber>(subscriber))
    {
    }

    public void Dispose()
    {
        if (!EventBus.TryGetTarget(out var bus) || !Subscriber.TryGetTarget(out var subscriber))
        {
            return;
        }

        bus.TryRemoveSubscriber(subscriber, out _); //todo exception
    }
}
