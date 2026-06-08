namespace EventBusLib.Core;

public interface ISubscriber
{
    public AliveStatus? HandleI(Event @event);
    public bool HasReturn();
}