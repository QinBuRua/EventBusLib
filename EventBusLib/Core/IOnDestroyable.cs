using EventBusLib.Dependencies;

namespace EventBusLib.Core;

public interface IOnDestroyable
{
    public void OnDestroy(GameTick nowTick);
}