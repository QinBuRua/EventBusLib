namespace EventBusLib.Core;

public abstract class SubscriberWithReturn<TEvent> : ISubscriber, IManaged
    where TEvent : Event
{
    public abstract AliveStatus Handle(TEvent @event);

    public AliveStatus? HandleI(Event @event)
    {
        return @event is TEvent tEvent
            ? Handle(tEvent)
            : throw new InvalidOperationException();
    }

    public bool HasReturn() => true;
}