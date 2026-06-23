using EventBusLib.Core;
using EventBusLib.Dependencies;

namespace EventBusLib.Extensions;

public interface IAliveCheckable : IManaged
{
    public AliveStatus CheckAlive(GameTick nowTick);
}
