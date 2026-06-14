using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public class SubscriberNotFoundException : InvalidOperationException
{
    public required EventBus Bus { get; init; }
    public required ISubscriber Subscriber{ get; init; }
}