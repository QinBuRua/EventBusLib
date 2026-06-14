using System.Diagnostics.CodeAnalysis;
using ConcurrentCollections;
using EventBusLib.Dependencies;
using EventBusLib.Exceptions;
using EventBusLib.Extensions;
using EventBusLib.Utils;

namespace EventBusLib.Core;

public class EventBus //todo: 线程安全
{
    public uint DefaultMaxPushEventCount
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

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        if (subscriber is IOnDestroyActable onDestroyActable)
        {
            onDestroyActable.OnDestroy(GameTick.Now);
        }

        if (subscriber is IManaged)
        {
            if (!_subscriberStrongRefSet.TryRemove(subscriber))
            {
                throw new SubscriberNotFoundException(this, subscriber);
            }
        }

        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);

        _weakSubscriberDic.TryGetValue(subscriber.GetEventType(), out var subscriberSet);
        if (subscriberSet is null || !subscriberSet.Contains(weakSubscriber))
        {
            throw new SubscriberNotFoundException(this, subscriber);
        }

        subscriberSet.Remove(weakSubscriber);
    }

    public void LoopOnce()
    {
        var nowTick = GameTick.Now;

        CheckSubscriberAliveStatus(nowTick);
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

    private void AddSubscriberToWeakSubscriberDic(WeakReference<ISubscriber> weakSubscriber, Type eventType)
    {
        if (!_weakSubscriberDic.TryGetValue(eventType, out var value))
        {
            value = GetEmptyWeakSubscriberSet();
            _weakSubscriberDic[eventType] = value;
        }

        value.Add(weakSubscriber);
    }

    private void CheckSubscriberAliveStatus(GameTick nowTick)
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

    private void PushOneEventToSubscriberNow(Event @event)//todo 重构
    {
        var eventType = @event.GetType();
        if (!_weakSubscriberDic.TryGetValue(eventType, out var weakSubscriberSet)) return;

        weakSubscriberSet.RemoveWhere(weakSubscriber =>
        {
            if (!weakSubscriber.TryGetTarget(out var subscriber)) return true;
            bool shouldRemove;
            if (subscriber.HasReturn())
            {
                shouldRemove = subscriber.HandelI(@event)! == AliveStatus.Dead;
            }
            else
            {
                shouldRemove = subscriber.HandelI(@event) is null
                    ? false
                    : throw new InvalidOperationException();
            }

            if (!shouldRemove) return shouldRemove;

            if (subscriber is IOnDestroyActable onDestroyActable)
            {
                onDestroyActable.OnDestroy(GameTick.Now);
            }

            if (subscriber is IManaged)
            {
                if (!_subscriberStrongRefSet.TryRemove(subscriber))
                {
                    throw new SubscriberNotFoundException(this, subscriber);//todo 危险的异常
                }
            }

            return shouldRemove;
        });
    }
}