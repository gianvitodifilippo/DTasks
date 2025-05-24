using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Analyzer.Utils;

internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array = array;

    public int Length => _array.IsDefault
        ? 0
        : _array.Length;
    
    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other) => _array.AsSpan().SequenceEqual(other._array.AsSpan());

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array.IsDefaultOrEmpty)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (T item in _array)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
    
    public struct Enumerator(EquatableArray<T> array) : IEnumerator<T>
    {
        private int _index = -1;

        public T Current
        {
            get
            {
                if (_index < 0 || _index >= array.Length)
                    throw new InvalidOperationException();
                
                return array[_index];
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < array.Length;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}

internal static class ImmutableArrayExtensions
{
    public static EquatableArray<T> ToEquatable<T>(this ImmutableArray<T> array)
        where T : IEquatable<T>
    {
        return new(array);
    }
}