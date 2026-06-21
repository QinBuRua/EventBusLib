using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ConcurrentCollections;
using ConcurrentPriorityQueue.Core;
using EventBusLib.Dependencies;
using EventBusLib.Exceptions;
using EventBusLib.Extensions;
using EventBusLib.Utils;

namespace EventBusLib.Core;

public partial class EventBus
{
    public partial void Clear()
    {
        _eventAliveQueue = new ConcurrentPriorityQueue<Event, GameTick>();
        _eventDelayQueue = new ConcurrentPriorityQueue<Event, GameTick>();
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
        if (subscriber is IManaged)
        {
            _subscriberStrongRefSet.Add(subscriber);
        }

        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        AddSubscriberToWeakSubscriberDic(weakSubscriber, subscriber.GetEventType());

        var nowTick = GameTick.Now;
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
        if (subscriber is IManaged && !_subscriberStrongRefSet.Contains(subscriber))
        {
            return false;
        }

        var eventType = subscriber.GetEventType();
        var weakSubscriber = new WeakReference<ISubscriber>(subscriber);
        return _weakSubscriberDic.TryGetValue(eventType, out var weakSubscriberSet)
               && weakSubscriberSet.Contains(weakSubscriber);
    }

    /// <summary>
    /// Attempts to remove a subscriber from the event bus.
    /// </summary>
    /// <param name="subscriber">The subscriber instance to remove.</param>
    /// <param name="onDestroyException">
    /// When this method returns <c>true</c>, contains any exception thrown during invocation of <see cref="IOnDestroyActable.OnDestroy"/> if the subscriber implements that interface; otherwise, <c>null</c>.
    /// When this method returns <c>false</c>, this parameter is always <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the subscriber existed and was successfully removed from the weak subscriber collection for its type; otherwise, <c>false</c>.
    /// </returns>
    public partial bool TryRemoveSubscriber(ISubscriber subscriber, out Exception? onDestroyException)
    {
        onDestroyException = null;

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
            onDestroyException = e;
        }

        return true;
    }

    private readonly WeakReference<EventBus> _weakSelf;

    private ConcurrentPriorityQueue<Event, GameTick> _eventDelayQueue = new();
    private ConcurrentPriorityQueue<Event, GameTick> _eventAliveQueue = new();

    private readonly ConcurrentDictionary<Type, HashSet<WeakReference<ISubscriber>>> _weakSubscriberDic = new();

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
        _eventAliveQueue.Enqueue(@event with { Priority = deadlineTick });
    }

    private void PushEventToDelayQueue<TEvent>(TEvent @event)
        where TEvent : Event
    {
        var deadlineTick = @event.CreateTime + @event.PushDelay;
        _eventDelayQueue.Enqueue(@event with { Priority = deadlineTick });
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
        public readonly bool IsEmpty() => OnDestroyException is null && OnCheckAliveException is null;
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
        public readonly partial bool IsEmpty()
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

        public partial void AddOnCheckAliveException(SubscriberTokenExceptionPair exceptionPair)
        {
            OnCheckAliveExceptions ??= [];
            OnCheckAliveExceptions.Add(exceptionPair);
        }

        public partial void AddOnHandleException(SubscriberTokenExceptionPair exceptionPair)
        {
            OnHandleExceptions ??= [];
            OnHandleExceptions.Add(exceptionPair);
        }

        public partial void AddOnDestroyException(SubscriberTokenExceptionPair exceptionPair)
        {
            OnDestroyExceptions ??= [];
            OnDestroyExceptions.Add(exceptionPair);
        }

        public partial void AddRangeOnCheckAliveExceptions(List<SubscriberTokenExceptionPair>? exceptionPairs)
        {
            if (exceptionPairs is null || exceptionPairs.Count <= 0)
            {
                return;
            }

            OnCheckAliveExceptions ??= [];
            OnCheckAliveExceptions.AddRange(exceptionPairs);
        }

        public partial void AddRangeOnHandleExceptions(List<SubscriberTokenExceptionPair>? exceptionPairs)
        {
            if (exceptionPairs is null || exceptionPairs.Count <= 0)
            {
                return;
            }

            OnHandleExceptions ??= [];
            OnHandleExceptions.AddRange(exceptionPairs);
        }

        public partial void AddRangeOnDestroyExceptions(List<SubscriberTokenExceptionPair>? exceptionPairs)
        {
            if (exceptionPairs is null || exceptionPairs.Count <= 0)
            {
                return;
            }

            OnDestroyExceptions ??= [];
            OnDestroyExceptions.AddRange(exceptionPairs);
        }
    }

    public partial bool TryLoopOnce(GameTick nowTick, out OnLoopExceptionSettings? onLoopExceptions)
    {
        return LoopOnceHelper.TryLoopOnce(nowTick, this, out onLoopExceptions);
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
            [NotNullWhen(false)] out OnLoopExceptionSettings? exceptions)
        {
            var helper = new LoopOnceHelper(nowTick, eventBus);
            helper.LoopOnce();

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
            UpdateDelayQueue();
            PushEventsToSubscribers(); //todo MaxProcess


            ClearWeakSubscribers();
        }

        private void ClearWeakSubscribers()
        {
            var weakDic = _busInstance._weakSubscriberDic;
            foreach (var (_, weakSet) in weakDic)
            {
                weakSet.RemoveWhere(weak => !weak.TryGetTarget(out _));
            }
        }

        private bool TryRemoveSubscribers(List<ISubscriber> subscribers,
            [NotNullWhen(false)] out List<SubscriberTokenExceptionPair>? onDestroyExceptions)
        {
            if (subscribers.Count <= 0)
            {
                onDestroyExceptions = null;
                return true;
            }

            onDestroyExceptions = [];
            foreach (var subscriber in subscribers)
            {
                _busInstance.TryRemoveSubscriber(subscriber, out var onDestroyException);
                if (onDestroyException is null)
                {
                    continue;
                }

                onDestroyExceptions.Add(new(new(_busInstance, subscriber), onDestroyException));
            }

            if (onDestroyExceptions.Count <= 0)
            {
                onDestroyExceptions = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CheckAliveStatus()
        {
            var removeList = new List<ISubscriber>();

            foreach (var subscriber in _busInstance._subscriberStrongRefSet)
            {
                if (subscriber is not IAliveCheckable aliveChecker)
                {
                    continue;
                }

                try
                {
                    if (aliveChecker.CheckAlive(_nowTick) == AliveStatus.Dead)
                    {
                        removeList.Add(subscriber);
                    }
                }
                catch (Exception onCheckAliveException)
                {
                    _exceptions.AddOnCheckAliveException(new SubscriberTokenExceptionPair(
                        new SubscriberToken(_busInstance, subscriber),
                        onCheckAliveException)
                    );
                }
            }

            if (!TryRemoveSubscribers(removeList, out var exceptionPairs))
            {
                _exceptions.AddRangeOnCheckAliveExceptions(exceptionPairs);
            }
        }

        private void UpdateDelayQueue()
        {
            var delayQueue = _busInstance._eventDelayQueue;
            var aliveQueue = _busInstance._eventAliveQueue;

            while (delayQueue.Peek().TryGetValue(out var delayEvent) && delayEvent.Priority >= _nowTick)
            {
                var newDeadline = delayEvent.CreateTime + delayEvent.PushDelay + delayEvent.MaxDelay;
                aliveQueue.Enqueue(delayEvent with { Priority = newDeadline });
            }
        }

        private void PushEventsToSubscribers()
        {
            var aliveQueue = _busInstance._eventAliveQueue;

            while (aliveQueue.Peek().TryGetValue(out var @event))
            {
                PushOneEventToSubscribers(@event);
            }
        }

        private void ForcePushEventsInDeadlineToSubscribers()
        {
            throw new NotImplementedException();
        }

        private void PushOneEventToSubscribers(Event @event)
        {
            if (!_busInstance._weakSubscriberDic.TryGetValue(@event.GetType(), out var weakSet) || weakSet.Count <= 0)
            {
                return;
            }

            var removeList = new List<ISubscriber>();
            foreach (var weakSubscriber in weakSet)
            {
                if (!weakSubscriber.TryGetTarget(out var subscriber))
                {
                    continue;
                }

                try
                {
                    if (!subscriber.HasReturn())
                    {
                        subscriber.HandelI(@event);
                        continue;
                    }

                    var result = subscriber.HandelI(@event)!;
                    if (result == AliveStatus.Dead)
                    {
                        removeList.Add(subscriber);
                    }
                }
                catch (Exception e)
                {
                    _exceptions.AddOnHandleException(new SubscriberTokenExceptionPair(
                        new SubscriberToken(_busInstance, subscriber), e));
                }
            }

            if (!TryRemoveSubscribers(removeList, out var onDestroyExceptions))
            {
                _exceptions.AddRangeOnDestroyExceptions(onDestroyExceptions);
            }
        }
    }
}
