using EventBusLib.Dependencies;
using EventBusLib.Utils;

namespace EventBusLib.Core;

public class EventBus
{
    public uint MaxProcessCount
    {
        get;
        set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(value)} must be greater than 0");
    } = 32;

    public void Clear()
    {
        _eventDelayQueue.Clear();
        _eventAliveQueue.Clear();
    }

    public void PushEvent<TEvent>(TEvent @event)
        where TEvent : Event
    {
        if (@event.PushDelay == 0)
        {
            PushEventToAliveQueue(@event);
        }
        else
        {
            PushEventToDelayQueue(@event);
        }
    }

    public SubscriberToken AddSubscriber(ISubscriber subscriber)
    {
        if (subscriber is IManaged)
        {
            _subscriberStrongRefSet.Add(subscriber);
        }

        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        if (subscriber.HasReturn())
        {
            AddSubscriberToWeakSubscriberWithReturnDic(weakSubscriber, subscriber.GetEventType());
        }
        else
        {
            AddSubscriberToWeakSubscriberWithoutReturnDic(weakSubscriber, subscriber.GetEventType());
        }

        return new SubscriberToken()
        {
            EventBus = _weakSelf,
            Subscriber = weakSubscriber,
        };
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        if (subscriber is IManaged)
        {
            _subscriberStrongRefSet.Remove(subscriber);
        }

        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        var subscriberDic = subscriber.HasReturn()
            ? _weakSubscriberWithReturnDic
            : _weakSubscriberWithoutReturnDic;

        subscriberDic.TryGetValue(subscriber.GetEventType(), out var subscriberSet);
        if (subscriberSet is null || !subscriberSet.Contains(weakSubscriber))
        {
            throw new InvalidOperationException();
        }

        subscriberSet.Remove(weakSubscriber);
    }

    public void LoopOnce()
    {
        var nowTick = GameTick.Now();

        RemoveSubscriberIfDead(nowTick);
        UpdateDelayQueue(nowTick);
        PushEventToSubscriberNow();
    }

    private readonly WeakReference<EventBus> _weakSelf;

    private readonly PriorityQueue<Event, GameTick> _eventDelayQueue = new();
    private readonly PriorityQueue<Event, GameTick> _eventAliveQueue = new();

    private readonly Dictionary<Type, HashSet<WeakReference<ISubscriber>>> _weakSubscriberWithReturnDic = new();
    private readonly Dictionary<Type, HashSet<WeakReference<ISubscriber>>> _weakSubscriberWithoutReturnDic = new();
    private readonly HashSet<ISubscriber> _subscriberStrongRefSet = new(ReferenceComparer<ISubscriber>.Instance);

    public EventBus()
    {
        _weakSelf = new WeakReference<EventBus>(this);
    }

    private void PushEventToAliveQueue<TEvent>(TEvent @event)
        where TEvent : Event
    {
        var deadlineTick = @event.CreateTime + @event.MaxDelay;
        _eventAliveQueue.Enqueue(@event, deadlineTick);
    }

    private void PushEventToDelayQueue<TEvent>(TEvent @event)
        where TEvent : Event
    {
        var deadlineTick = @event.CreateTime + @event.PushDelay;
        _eventDelayQueue.Enqueue(@event, deadlineTick);
    }

    private static HashSet<WeakReference<ISubscriber>> GetEmptyWeakSubscriberSet()
        => new(WeakReferenceTargetRefComparer<ISubscriber>.Instance);

    private void AddSubscriberToWeakSubscriberWithoutReturnDic(WeakReference<ISubscriber> weakSubscriber,
        Type eventType)
    {
        if (!_weakSubscriberWithoutReturnDic.TryGetValue(eventType, out var value))
        {
            value = GetEmptyWeakSubscriberSet();
            _weakSubscriberWithoutReturnDic[eventType] = value;
        }

        value.Add(weakSubscriber);
    }

    private void AddSubscriberToWeakSubscriberWithReturnDic(WeakReference<ISubscriber> weakSubscriber, Type eventType)
    {
        if (!_weakSubscriberWithReturnDic.TryGetValue(eventType, out var value))
        {
            value = GetEmptyWeakSubscriberSet();
            _weakSubscriberWithReturnDic[eventType] = value;
        }

        value.Add(weakSubscriber);
    }

    private void RemoveSubscriberIfDead(GameTick nowTick)
    {
        foreach (var subscriber in _subscriberStrongRefSet)
        {
            if (subscriber is not IAliveCheckable subscriberCheckable) continue;

            if (subscriberCheckable.CheckAlive(nowTick) == AliveStatus.Dead)
            {
                RemoveSubscriber(subscriber);
            }
        }
    }

    private void UpdateDelayQueue(GameTick nowTick)
    {
        while (_eventDelayQueue.TryPeek(out var @event, out var deadlineTick) && deadlineTick <= nowTick)
        {
            _eventDelayQueue.Dequeue();
            _eventAliveQueue.Enqueue(@event, deadlineTick + @event.MaxDelay);
        }
    }

    private void PushEventToSubscriberNow(uint? maxProcessCount = null)
    {
        maxProcessCount ??= MaxProcessCount;

        for (var i = 0; i < maxProcessCount; i++)
        {
            if (!_eventAliveQueue.TryPeek(out _, out _))
            {
                break;
            }

            throw new NotImplementedException();
        }
    }
}