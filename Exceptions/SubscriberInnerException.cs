using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public partial class SubscriberInnerException(string? message, Exception innerException)
    : Exception(message ?? "Subscriber threw a unhandled exception", innerException)
{
    public required EventBus Bus { get; init; }
    public virtual required ISubscriber Subscriber { get; init; }

    public partial SubscriberToken GetToken { get; }
}
