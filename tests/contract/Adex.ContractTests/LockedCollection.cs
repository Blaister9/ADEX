using System.Collections;

namespace Adex.ContractTests;

public sealed class LockedCollection<T> : ICollection<T>
{
    private readonly Lock _lock = new();
    private readonly List<T> _items = [];

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _items.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
        }
    }

    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _items.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock)
        {
            _items.CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(T item)
    {
        lock (_lock)
        {
            return _items.Remove(item);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return _items.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
