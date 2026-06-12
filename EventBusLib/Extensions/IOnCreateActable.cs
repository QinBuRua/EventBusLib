using EventBusLib.Dependencies;

namespace EventBusLib.Extensions;

public interface IOnCreateActable//todo: 整合
{
    public void OnCreate(GameTick nowTick);
}