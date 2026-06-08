namespace EventBusLib.Core;

public class SubscriberToken : IDisposable
{
    public required WeakReference<ISubscriber> Subscriber { get; set; }
    public required WeakReference<EventBus> EventBus { get; set; }

    void IDisposable.Dispose()
    {
        if (Subscriber.TryGetTarget(out var subscriber) && EventBus.TryGetTarget(out var eventBus))
        {
            eventBus.RemoveSubscriber(subscriber);
        }
    }
}