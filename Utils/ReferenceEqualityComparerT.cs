using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EventBusLib.Utils;

public class ReferenceEqualityComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}
