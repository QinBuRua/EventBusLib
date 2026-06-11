using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public class SubscriberNotFoundException : InvalidOperationException
{
    public required EventBusLib.Core.EventBus Bus { get; init; }
    public required ISubscriber Subscriber{ get; init; }
}