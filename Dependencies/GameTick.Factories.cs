namespace EventBusLib.Dependencies;

public partial struct GameTick
{
    public static GameTick Now => new(DateTime.Now);
    public static GameTick FromSeconds(long seconds) => new(seconds * 50);
    public static GameTick FromMilliseconds(long milliseconds) => new(milliseconds / 20);
}