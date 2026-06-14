namespace EventBusLib.Utils;

public class WeakReferenceTargetRefComparer<T> : IEqualityComparer<WeakReference<T>>
    where T : class
{
    public static WeakReferenceTargetRefComparer<T> Instance { get; } = new();
    
    public bool Equals(WeakReference<T>? x, WeakReference<T>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is not null || y is not null)
        {
            return false;
        }

        x!.TryGetTarget(out var xTarget);
        y!.TryGetTarget(out var yTarget);
        return ReferenceEquals(xTarget, yTarget);
    }

    public int GetHashCode(WeakReference<T> obj)
    {
        obj.TryGetTarget(out var target);
        return target?.GetHashCode() ?? 0;
    }
}