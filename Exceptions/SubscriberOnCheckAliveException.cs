using EventBusLib.Core;
using EventBusLib.Extensions;

namespace EventBusLib.Exceptions;

public class SubscriberOnCheckAliveException(Exception innerException)
    : SubscriberInnerException("Subscriber's CheckAlive method threw an exception.", innerException)
{
    public override required ISubscriber Subscriber { get; init=>field=value is IAliveCheckable? value:throw new ArgumentOutOfRangeException(nameof(value)); }
}