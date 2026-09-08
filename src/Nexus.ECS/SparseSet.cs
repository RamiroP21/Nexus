using Nexus.Core;

namespace Nexus.ECS;

/// <summary>
/// Stores one unmanaged component type in contiguous dense arrays with constant-time sparse lookup.
/// Removal swaps the last dense entry into the gap, making physical order depend on structural history.
/// </summary>
internal sealed class SparseSet<T>
    where T : unmanaged, IComponent<T>
{
    private readonly string _stableId;
    private EntityId[] _entities;
    private T[] _components;
    private int[] _sparse;
    private int _count;

    public SparseSet(int initialCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, Array.MaxLength - 1);
        _stableId = T.StableId;
        if (string.IsNullOrWhiteSpace(_stableId))
        {
            throw new InvalidOperationException("A component requires a non-empty stable schema ID.");
        }

        _entities = new EntityId[initialCapacity];
        _components = new T[initialCapacity];
        _sparse = new int[initialCapacity + 1];
    }

    public int Count => _count;

    public int Capacity => _components.Length;

    public EntityId GetEntityAt(int denseIndex)
    {
        ValidateDenseIndex(denseIndex);
        return _entities[denseIndex];
    }

    /// <summary>
    /// Gets a mutable reference valid only until the next structural modification of this store.
    /// </summary>
    public ref T GetAt(int denseIndex)
    {
        ValidateDenseIndex(denseIndex);
        return ref _components[denseIndex];
    }

    public bool Has(EntityId entity) => FindDenseIndex(entity) >= 0;

    public void Add(EntityId entity, in T component)
    {
        if (entity.Index == 0 || entity.Generation == 0)
        {
            throw new ArgumentException("A component requires a nonzero entity index and generation.", nameof(entity));
        }

        if (entity.Index >= (uint)Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "The entity index exceeds supported sparse storage capacity.");
        }

        EnsureSparseCapacity((int)entity.Index);
        if (_sparse[entity.Index] != 0)
        {
            throw new InvalidOperationException("The entity slot already contains a component; remove its existing generation first.");
        }

        EnsureDenseCapacity();
        _entities[_count] = entity;
        _components[_count] = component;
        _sparse[entity.Index] = ++_count;
    }

    public bool Remove(EntityId entity)
    {
        int denseIndex = FindDenseIndex(entity);
        if (denseIndex < 0)
        {
            return false;
        }

        int lastIndex = --_count;
        if (denseIndex != lastIndex)
        {
            EntityId movedEntity = _entities[lastIndex];
            _entities[denseIndex] = movedEntity;
            _components[denseIndex] = _components[lastIndex];
            _sparse[movedEntity.Index] = denseIndex + 1;
        }

        _entities[lastIndex] = default;
        _components[lastIndex] = default;
        _sparse[entity.Index] = 0;
        return true;
    }

    /// <summary>
    /// Gets a mutable reference valid only until the next structural modification of this store.
    /// </summary>
    public ref T Get(EntityId entity)
    {
        int denseIndex = FindDenseIndex(entity);
        if (denseIndex < 0)
        {
            throw new KeyNotFoundException("The entity does not have this component.");
        }

        return ref _components[denseIndex];
    }

    public bool TryGet(EntityId entity, out T component)
    {
        int denseIndex = FindDenseIndex(entity);
        if (denseIndex < 0)
        {
            component = default;
            return false;
        }

        component = _components[denseIndex];
        return true;
    }

    internal void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add("Nexus.ECS.SparseSet.v1");
        hasher.Add(_stableId);
        hasher.Add(_count);
        var payloadHasher = new StableHasher64();
        for (int index = 0; index < _count; index++)
        {
            hasher.Add(index);
            hasher.Add(_entities[index]);
            payloadHasher.Reset();
            T.ContributeToHash(in _components[index], payloadHasher);
            hasher.Add(payloadHasher.Value);
        }
    }

    private int FindDenseIndex(EntityId entity)
    {
        uint slot = entity.Index;
        if (slot == 0 || slot >= (uint)_sparse.Length)
        {
            return -1;
        }

        int denseIndex = _sparse[slot] - 1;
        return denseIndex >= 0 && _entities[denseIndex] == entity ? denseIndex : -1;
    }

    private void ValidateDenseIndex(int denseIndex)
    {
        if ((uint)denseIndex >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(nameof(denseIndex));
        }
    }

    private void EnsureDenseCapacity()
    {
        if (_count < _components.Length)
        {
            return;
        }

        int capacity = GrowCapacity(_components.Length, checked(_count + 1), Array.MaxLength - 1);
        Array.Resize(ref _entities, capacity);
        Array.Resize(ref _components, capacity);
    }

    private void EnsureSparseCapacity(int slot)
    {
        if (slot < _sparse.Length)
        {
            return;
        }

        int capacity = GrowCapacity(_sparse.Length - 1, slot, Array.MaxLength - 1);
        Array.Resize(ref _sparse, capacity + 1);
    }

    private static int GrowCapacity(int current, int required, int maximum)
    {
        if (required > maximum)
        {
            throw new InvalidOperationException("The component store has exhausted its supported capacity.");
        }

        return (int)Math.Min(Math.Max((long)current * 2, required), maximum);
    }
}
