using System.Diagnostics.CodeAnalysis;
using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public class SubscriberNotFoundException() : InvalidOperationException("Subscriber Not Found")
{
    public required EventBus Bus { get; init; }
    public required ISubscriber Subscriber { get; init; }

    [SetsRequiredMembers]
    public SubscriberNotFoundException(EventBus bus, ISubscriber subscriber) : this()
    {
        Bus = bus;
        Subscriber = subscriber;
    }
}