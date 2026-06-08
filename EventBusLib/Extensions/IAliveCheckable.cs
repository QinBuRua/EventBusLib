using EventBusLib.Core;

namespace EventBusLib.Extensions;

public interface IAliveCheckable : IManaged
{
    public AliveStatus CheckAlive();
}