using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.AspNetCore.Analyzer.Utils;

internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _array = array;

    public int Length => _array?.Length ?? 0;
    
    public T this[int index] => _array is null
        ? throw new IndexOutOfRangeException()
        : _array[index];

    public bool Equals(EquatableArray<T> other) => _array.AsSpan().SequenceEqual(other._array.AsSpan());

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is null)
            return 0;

        HashCode hashCode = default;

        foreach (T item in _array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
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

internal static class ArrayExtensions
{
    public static EquatableArray<T> ToEquatable<T>(this T[] array)
        where T : IEquatable<T>
    {
        return new(array);
    }
}