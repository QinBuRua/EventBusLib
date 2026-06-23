using EventBusLib.Core;
using EventBusLib.Extensions;

namespace EventBusLib.Exceptions;

public class SubscriberOnDestroyException(Exception innerException)
    : SubscriberInnerException("Subscriber's OnDestroy method threw an exception.", innerException)
{
    public override required ISubscriber Subscriber
    {
        get;
        init => field = value is IOnDestroyActable ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public IOnDestroyActable GetOnDestroyActor()
    {
        return (Subscriber as IOnDestroyActable)!;
    }
}
