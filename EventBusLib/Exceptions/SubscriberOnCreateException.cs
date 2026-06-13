using EventBusLib.Core;
using EventBusLib.Extensions;

namespace EventBusLib.Exceptions;

public class SubscriberOnCreateException : Exception
{
    public required EventBus Bus { get; init; }

    public required ISubscriber Subscriber
    {
        get;
        init => field = value is IOnCreateActable ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public IOnCreateActable GetOnCreateActor()
    {
        return (Subscriber as IOnCreateActable)!;
    }
}