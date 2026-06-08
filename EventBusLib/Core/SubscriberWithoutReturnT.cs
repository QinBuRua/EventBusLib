namespace EventBusLib.Core;

public abstract class SubscriberWithoutReturn<TEvent> : ISubscriber
    where TEvent : Event
{
    public abstract void Handle(TEvent @event);


    public AliveStatus? HandleI(Event @event)
    {
        if (@event is not TEvent tEvent)
        {
            throw new InvalidOperationException();
        }

        Handle(tEvent);
        return null;
    }

    public bool HasReturn() => false;
}