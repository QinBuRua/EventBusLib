using System.Diagnostics.CodeAnalysis;

namespace EventBusLib.Utils;

public class ReferenceComparer<T> :IEqualityComparer<T>
{
    public static ReferenceComparer<T> Instance { get; } = new();
    
    public bool Equals(T? x, T? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return obj.GetHashCode();
    }
}