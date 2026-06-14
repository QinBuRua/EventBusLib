using EventBusLib.Dependencies;

namespace EventBusLib.Extensions;

public interface IOnDestroyActable//todo: 整合
{
    public void OnDestroy(GameTick nowTick);
}