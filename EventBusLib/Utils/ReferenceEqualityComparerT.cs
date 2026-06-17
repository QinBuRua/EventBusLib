using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EventBusLib.Utils;

public class ReferenceEqualityComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
        => ReferenceEquals(x, y);

    public int GetHashCode([DisallowNull] T obj)
        => RuntimeHelpers.GetHashCode(obj);
}