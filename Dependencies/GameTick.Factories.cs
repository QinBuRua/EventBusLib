namespace EventBusLib.Dependencies;

public partial struct GameTick
{
    public static GameTick Now => new(DateTime.Now);

    public static GameTick FromSeconds(long seconds)
    {
        return new GameTick(seconds * 50);
    }

    public static GameTick FromMilliseconds(long milliseconds)
    {
        return new GameTick(milliseconds / 20);
    }
}
