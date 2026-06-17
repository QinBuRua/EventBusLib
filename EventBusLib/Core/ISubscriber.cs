namespace EventBusLib.Core;

public interface ISubscriber
{
    public AliveStatus? HandelI(Event @event);
    public bool HasReturn();

    public Type GetEventType();
}