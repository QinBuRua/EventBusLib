using EventBusLib.Dependencies;

namespace EventBusLib.Core;

public interface IAliveCheckable : IManaged
{
    public AliveStatus CheckAlive(GameTick nowTick);
}