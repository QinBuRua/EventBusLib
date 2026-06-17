using EventBusLib.Dependencies;

namespace EventBusLib.Extensions;

public interface IOnCreateActable
{
    public void OnCreate(GameTick nowTick);
}