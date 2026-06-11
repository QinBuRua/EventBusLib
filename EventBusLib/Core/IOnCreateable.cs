using EventBusLib.Dependencies;

namespace EventBusLib.Core;

public interface IOnCreateable
{
    public void OnCreate(GameTick nowTick);
}