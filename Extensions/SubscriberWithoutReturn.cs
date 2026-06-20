using EventBusLib.Core;

namespace EventBusLib.Extensions;

public abstract class SubscriberWithoutReturn<TEvent> : ISubscriber
    where TEvent : Event
{
    public abstract void Handle(TEvent @event);

    public AliveStatus? HandelI(Event @event)
    {
        if (@event is not TEvent tEvent)
        {
            throw new ArgumentException($"{nameof(@event)} is not TEvent");
        }

        Handle(tEvent);
        return null;
    }

    public Type GetEventType() => typeof(TEvent);

    public bool HasReturn() => false;
}