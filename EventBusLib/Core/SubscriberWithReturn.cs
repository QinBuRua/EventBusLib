namespace EventBusLib.Core;

public abstract class SubscriberWithReturn<TEvent> : ISubscriber, IManaged
{
    public abstract AliveStatus Handle(TEvent @event);

    public AliveStatus? HandelI(Event @event)
    {
        return @event is not TEvent tEvent
            ? throw new ArgumentException($"{nameof(@event)} is not TEvent")
            : Handle(tEvent);
    }

    public Type GetEventType() => typeof(TEvent);

    public bool HasReturn() => true;
}