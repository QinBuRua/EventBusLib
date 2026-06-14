namespace EventBusLib.Core;

public readonly record struct SubscriberToken(
    WeakReference<EventBusLib.Core.EventBus> EventBus,
    WeakReference<ISubscriber> Subscriber
) : IDisposable
{
    public void Dispose()
    {
        if (!EventBus.TryGetTarget(out var bus) || !Subscriber.TryGetTarget(out var subscriber))
        {
            return;
        }

        bus.RemoveSubscriber(subscriber);
    }
}