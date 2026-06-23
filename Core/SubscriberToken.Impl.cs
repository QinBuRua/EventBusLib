using System.Diagnostics.CodeAnalysis;

namespace EventBusLib.Core;

public partial record struct SubscriberToken
{
    public partial void Dispose()
    {
        if (!EventBus.TryGetTarget(out var bus) || !Subscriber.TryGetTarget(out var subscriber))
        {
            return;
        }

        bus.TryRemoveSubscriber(subscriber, out _); //todo exception
    }

    public partial bool IsAvailable
        => EventBus.TryGetTarget(out _) && Subscriber.TryGetTarget(out _);

    public partial bool TryGetBusAndSubscriber(out EventBus? eventBus, out ISubscriber? subscriber)
    {
        subscriber = null;
        return EventBus.TryGetTarget(out eventBus) && Subscriber.TryGetTarget(out subscriber);
    }
}