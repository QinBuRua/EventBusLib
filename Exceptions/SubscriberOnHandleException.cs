namespace EventBusLib.Exceptions;

public class SubscriberOnHandleException(Exception innerException)
    : SubscriberInnerException("Subscriber HandleI method threw an exception.", innerException)
{
}
