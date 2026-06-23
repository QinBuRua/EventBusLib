using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public partial class SubscriberInnerException
{
    public partial SubscriberToken GetToken => new(Bus, Subscriber);
}
