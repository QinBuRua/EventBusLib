using EventBusLib.Core;

namespace EventBusLib.Extensions;

public abstract class SubscriberWithReturn<TEvent> : ISubscriber, IManaged
{
    public AliveStatus? HandelI(Event @event)
    {
        return @event is not TEvent tEvent
            ? throw new ArgumentException($"{nameof(@event)} is not TEvent")
            : Handle(tEvent);
    }

    public Type GetEventType()
    {
        return typeof(TEvent);
    }

    public bool HasReturn()
    {
        return true;
    }

    public abstract AliveStatus Handle(TEvent @event);
}
