using EventBusLib.Core;
using EventBusLib.Extensions;

namespace EventBusLib.Exceptions;

public class SubscriberOnCreateException(Exception innerException)
    : SubscriberInnerException("Subscriber's OnCreate method threw an exception.", innerException)
{
    public override required ISubscriber Subscriber
    {
        get;
        init => field = value is IOnCreateActable ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public IOnCreateActable GetOnCreateActor()
    {
        return (Subscriber as IOnCreateActable)!;
    }
}