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

    public partial record struct OnLoopExceptionSettings
    {
        public partial bool IsEmpty()
        {
            return
                (OnCheckAliveExceptions is null || OnCheckAliveExceptions.Count <= 0)
                && (OnHandleExceptions is null || OnHandleExceptions.Count <= 0)
                && (OnDestroyExceptions is null || OnDestroyExceptions.Count <= 0);
        }

        public partial bool TryGetOnCheckAliveExceptions(out List<SubscriberTokenExceptionPair>? onCheckAliveExceptions)
        {
            if (OnCheckAliveExceptions is { Count: > 0 })
            {
                onCheckAliveExceptions = OnCheckAliveExceptions;
                return true;
            }

            onCheckAliveExceptions = null;
            return false;
        }

        public partial bool TryGetOnHandleExceptions(out List<SubscriberTokenExceptionPair>? onHandleExceptions)
        {
            if (OnHandleExceptions is { Count: > 0 })
            {
                onHandleExceptions = OnHandleExceptions;
                return true;
            }

            onHandleExceptions = null;
            return false;
        }

        public partial bool TryGetOnDestroyExceptions(out List<SubscriberTokenExceptionPair>? onDestroyExceptions)
        {
            if (OnDestroyExceptions is { Count: > 0 })
            {
                onDestroyExceptions = OnDestroyExceptions;
                return true;
            }

            onDestroyExceptions = null;
            return false;
        }
    }
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

public partial class EventBus
{
    private class LoopOnceHelper(GameTick nowTick, EventBus eventBus)
    {
        /// <summary>
        /// Attempts to execute a single event loop iteration, recording any exceptions that occur.
        /// </summary>
        /// <param name="nowTick">The current game tick at which to process events.</param>
        /// <param name="eventBus">The event bus instance on which to run the loop.</param>
        /// <param name="exceptions">When this method returns <c>false</c>, contains the collected exceptions; otherwise, <c>null</c>.</param>
        /// <returns>
        /// <c>true</c> if the loop iteration completed without exceptions; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryLoopOnce(GameTick nowTick, EventBus eventBus,
            [NotNullWhen(false)] out OnLoopExceptionSettings? exceptions) //todo
        {
            var helper = new LoopOnceHelper(nowTick, eventBus);

            if (helper._exceptions.IsEmpty())
            {
                exceptions = null;
                return true;
            }
            else
            {
                exceptions = helper._exceptions;
                return false;
            }
        }

        private GameTick _nowTick = nowTick;
        private EventBus _busInstance = eventBus;

        private OnLoopExceptionSettings _exceptions = new()
        {
            OnCheckAliveExceptions = [],
            OnDestroyExceptions = [],
            OnHandleExceptions = [],
        };

        private void LoopOnce()
        {
            CheckAliveStatus();
        }

        private void CheckAliveStatus()
        {
            var onCheckAliveExceptions = _exceptions.OnCheckAliveExceptions!;
            var onDestroyExceptions = _exceptions.OnDestroyExceptions!;
            foreach (var subscriber in _busInstance._subscriberStrongRefSet)
            {
                if (subscriber is not IAliveCheckable aliveChecker)
                {
                    continue;
                }

                try
                {
                    var aliveStatus = aliveChecker.CheckAlive(_nowTick);

                    if (aliveStatus == AliveStatus.Alive)
                    {
                        continue;
                    }

                    _busInstance.TryRemoveSubscriber(subscriber, out var onDestroyException);
                    if (onDestroyException is not null)
                    {
                        onCheckAliveExceptions.Add(new SubscriberTokenExceptionPair(
                            new SubscriberToken(_busInstance, subscriber),
                            onDestroyException)
                        );
                    }
                }
                catch (Exception onCheckAliveException)
                {
                    onDestroyExceptions.Add(new SubscriberTokenExceptionPair(
                        new SubscriberToken(_busInstance, subscriber),
                        onCheckAliveException)
                    );
                }
            }
        }
    }
}
