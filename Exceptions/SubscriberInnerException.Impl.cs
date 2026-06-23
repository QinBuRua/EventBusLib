using System.Diagnostics.CodeAnalysis;
using EventBusLib.Core;

namespace EventBusLib.Exceptions;

public partial class SubscriberInnerException
{
    public partial SubscriberToken GetToken => new SubscriberToken(Bus, Subscriber);
}
