using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConcurrentCollections;
using EventBusLib.Dependencies;
using EventBusLib.Exceptions;
using EventBusLib.Extensions;
using EventBusLib.Utils;

namespace EventBusLib.Core;

public partial class EventBus
{
    public partial void Clear()
    {
        _eventAliveQueue.Clear();
        _eventDelayQueue.Clear();
        _subscriberStrongRefSet.Clear();
        _weakSubscriberDic.Clear();
    }

    public partial void PushEvent<TEvent>(TEvent @event)
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

    public partial SubscriberToken AddSubscriber(ISubscriber subscriber)
    {
        var nowTick = GameTick.Now;

        if (subscriber is IManaged)
        {
            _subscriberStrongRefSet.Add(subscriber);
        }

        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        AddSubscriberToWeakSubscriberDic(weakSubscriber, subscriber.GetEventType());

        try
        {
            if (subscriber is IOnCreateActable onCreateActable)
            {
                onCreateActable.OnCreate(nowTick);
            }
        }
        catch (Exception e)
        {
            throw new SubscriberOnCreateException(e)
            {
                Bus = this,
                Subscriber = subscriber,
            };
        }

        return new SubscriberToken()
        {
            EventBus = _weakSelf,
            Subscriber = weakSubscriber,
        };
    }

    public partial bool ContainsSubscriber(ISubscriber subscriber)
    {
        if (subscriber is null)
        {
            return false;
        }
        
        if (subscriber is IManaged && !_subscriberStrongRefSet.Contains(subscriber))
        {
            return false;
        }

        var eventType = subscriber.GetEventType();
        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        return _weakSubscriberDic.TryGetValue(eventType, out var weakSubscriberSet)
               && weakSubscriberSet.Contains(weakSubscriber);
    }

    public partial bool TryRemoveSubscriber(ISubscriber subscriber, out Exception? exception)
    {
        exception = null;

        _subscriberStrongRefSet.TryRemove(subscriber);

        if (!_weakSubscriberDic.TryGetValue(subscriber.GetType(), out var weakSubscriberSet)
            || !weakSubscriberSet.Remove(new WeakReference<ISubscriber>(subscriber)))
        {
            return false;
        }

        if (subscriber is not IOnDestroyActable onDestroyActor)
        {
            return true;
        }

        try
        {
            onDestroyActor.OnDestroy(GameTick.Now);
        }
        catch (Exception e)
        {
            exception = e;
        }

        return true;
    }

    public partial void TryLoopOnce()//todo exception
    {
        var nowTick = GameTick.Now;

        TryCheckSubscriberAliveStatus(nowTick, out _);
        UpdateDelayQueue(nowTick);
        PushEventToSubscriberNow();
        PushEventAtDeadline(nowTick);
    }

    private readonly WeakReference<EventBus> _weakSelf;

    private readonly PriorityQueue<Event, GameTick> _eventDelayQueue = new();
    private readonly PriorityQueue<Event, GameTick> _eventAliveQueue = new();

    private readonly Dictionary<Type, HashSet<WeakReference<ISubscriber>>> _weakSubscriberDic = new();

    private readonly ConcurrentHashSet<ISubscriber> _subscriberStrongRefSet =
        new(ReferenceComparer<ISubscriber>.Instance);

    public partial EventBus()
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

    private void AddSubscriberToWeakSubscriberDic(WeakReference<ISubscriber> weakSubscriber, Type eventType)
    {
        if (!_weakSubscriberDic.TryGetValue(eventType, out var value))
        {
            value = GetEmptyWeakSubscriberSet();
            _weakSubscriberDic[eventType] = value;
        }

        value.Add(weakSubscriber);
    }

    private record struct CheckAliveExceptionPair(Exception? OnCheckAliveException, Exception? OnDestroyException)
    {
        public bool IsEmpty() => OnDestroyException is null && OnCheckAliveException is null;
    }
    
    private bool TryCheckSubscriberAliveStatus(GameTick nowTick, [NotNullWhen(false)]out List<CheckAliveExceptionPair>? checkAliveExceptionPairs)
    {
        var list = new List<CheckAliveExceptionPair>();
        
        foreach (var subscriber in _subscriberStrongRefSet)
        {
            if (subscriber is not IAliveCheckable subscriberCheckable) continue;
            
            var pair = new CheckAliveExceptionPair(null, null);
            try
            {
                if (subscriberCheckable.CheckAlive(nowTick) == AliveStatus.Dead)
                {
                    TryRemoveSubscriber(subscriber, out var exception); //todo exception
                    pair.OnDestroyException = exception;
                }
            }
            catch (Exception e)
            {
                pair.OnCheckAliveException = e;
            }

            if (!pair.IsEmpty())
            {
                list.Add(pair);
            }
        }

        if (list.Count==0)
        {
            checkAliveExceptionPairs = null;
            return true;
        }
        else
        {
            checkAliveExceptionPairs = list;
            return false;
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

    private void PushEventToSubscriberNow(uint? maxPushEventCount = null)
    {
        maxPushEventCount ??= DefaultMaxPushEventCount;

        for (var i = 0; i < maxPushEventCount && _eventAliveQueue.TryDequeue(out var @event, out _); i++)
        {
            PushOneEventToSubscriberNow(@event);
        }
    }

    private void PushEventAtDeadline(GameTick nowTick)
    {
        while (_eventAliveQueue.TryPeek(out var @event, out var deadlineTick) && deadlineTick >= nowTick)
        {
            _eventAliveQueue.Dequeue();
            PushOneEventToSubscriberNow(@event);
        }
    }

    private void PushOneEventToSubscriberNow(Event @event) //todo 重构
    {
        if (!TryExtractSubscriberListWhileRemove(@event, out var subscriberList))
        {
            return;
        }
    }

    private bool TryExtractSubscriberListWhileRemove(Event @event,
        [NotNullWhen(true)] out List<ISubscriber>? subscriberList)
    {
        if (!_weakSubscriberDic.TryGetValue(@event.GetType(), out var weakSubscriberSet))
        {
            subscriberList = null;
            return false;
        }

        var list = new List<ISubscriber>();
        weakSubscriberSet.RemoveWhere(weakSubscriber =>
        {
            if (!weakSubscriber.TryGetTarget(out var subscriber)) return true;

            list.Add(subscriber);
            return false;
        });

        if (list.Count == 0)
        {
            subscriberList = null;
            return false;
        }
        else
        {
            subscriberList = list;
            return true;
        }
    }

    public partial uint DefaultMaxPushEventCount
    {
        get;
        set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(value)} must be greater than 0");
    }

    public partial long SubscriberCount
    {
        get
        {
            long count = 0;
            foreach (var (_, weakSet) in _weakSubscriberDic)
            {
                count += weakSet.Count;
            }

            return count;
        }
    }

    public partial EventCountSetting EventCount => new(_eventDelayQueue.Count, _eventAliveQueue.Count);
}

public partial class EventBus
{
    public partial record struct EventCountSetting
    {
        public partial long Total => Delay + Alive;
    }
}

public partial class EventBus
{
    public partial record struct SubscriberCountSetting
    {
        public partial long Unmanaged => Total - Managed;
    }
}