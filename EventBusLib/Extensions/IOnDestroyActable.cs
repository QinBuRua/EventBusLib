using EventBusLib.Dependencies;

namespace EventBusLib.Extensions;

public interface IOnDestroyActable
{
    public void OnDestroy(GameTick nowTick);
}