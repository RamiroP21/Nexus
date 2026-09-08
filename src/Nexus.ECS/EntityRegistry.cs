using Nexus.Core;

namespace Nexus.ECS;

/// <summary>
/// Allocates generation-protected entities using compact slot arrays and a deterministic LIFO free list.
/// </summary>
public sealed class EntityRegistry
{
    private uint[] _generations;
    private bool[] _alive;
    private uint[] _freeSlots;
    private int _freeCount;
    private int _allocatedSlotCount;
    private int _aliveCount;
    private int _version;

    public EntityRegistry(int initialCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, Array.MaxLength - 1);
        _generations = new uint[initialCapacity + 1];
        _alive = new bool[initialCapacity + 1];
        _freeSlots = new uint[initialCapacity];
    }

    public int AliveCount => _aliveCount;

    /// <summary>
    /// Gets the number of slots ever allocated, including free and permanently retired slots.
    /// </summary>
    public int AllocatedSlotCount => _allocatedSlotCount;

    public EntityId CreateEntity()
    {
        uint slot;
        if (_freeCount > 0)
        {
            slot = _freeSlots[--_freeCount];
            _freeSlots[_freeCount] = 0;
        }
        else
        {
            EnsureSlotCapacity();
            slot = checked((uint)++_allocatedSlotCount);
            _generations[slot] = 1;
        }

        _alive[slot] = true;
        _aliveCount++;
        _version = unchecked(_version + 1);
        return EntityId.FromParts(slot, _generations[slot]);
    }

    public bool IsAlive(EntityId entity)
    {
        uint slot = entity.Index;
        return slot > 0
            && slot <= (uint)_allocatedSlotCount
            && _alive[slot]
            && _generations[slot] == entity.Generation;
    }

    /// <summary>
    /// Destroys a live identity. At the maximum generation its slot retires permanently instead of wrapping.
    /// </summary>
    public bool DestroyEntity(EntityId entity)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        uint slot = entity.Index;
        _alive[slot] = false;
        _aliveCount--;
        if (_generations[slot] < uint.MaxValue)
        {
            _generations[slot]++;
            _freeSlots[_freeCount++] = slot;
        }

        _version = unchecked(_version + 1);
        return true;
    }

    /// <summary>
    /// Enumerates living entities in ascending slot order without iterator allocation.
    /// Structural mutation invalidates an active enumerator.
    /// </summary>
    public AliveEntityEnumerable GetAliveEntities() => new(this);

    /// <summary>
    /// Hashes allocator state, including inactive generations and the free-list order affecting future IDs.
    /// Capacity is deliberately excluded because it does not affect observable execution.
    /// </summary>
    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add("Nexus.ECS.EntityRegistry.v1");
        hasher.Add(_allocatedSlotCount);
        hasher.Add(_aliveCount);
        for (int slot = 1; slot <= _allocatedSlotCount; slot++)
        {
            hasher.Add(slot);
            hasher.Add(_generations[slot]);
            hasher.Add(_alive[slot]);
        }

        hasher.Add(_freeCount);
        for (int index = 0; index < _freeCount; index++)
        {
            hasher.Add(index);
            hasher.Add(_freeSlots[index]);
        }
    }

    private void EnsureSlotCapacity()
    {
        int capacity = _generations.Length - 1;
        if (_allocatedSlotCount < capacity)
        {
            return;
        }

        if (capacity == Array.MaxLength - 1)
        {
            throw new InvalidOperationException("The entity registry has exhausted its supported slot capacity.");
        }

        int newCapacity = (int)Math.Min((long)capacity * 2, Array.MaxLength - 1L);
        Array.Resize(ref _generations, newCapacity + 1);
        Array.Resize(ref _alive, newCapacity + 1);
        Array.Resize(ref _freeSlots, newCapacity);
    }

    public readonly struct AliveEntityEnumerable
    {
        private readonly EntityRegistry _registry;

        internal AliveEntityEnumerable(EntityRegistry registry) => _registry = registry;

        public AliveEntityEnumerator GetEnumerator() => new(_registry);
    }

    public struct AliveEntityEnumerator
    {
        private readonly EntityRegistry _registry;
        private readonly int _version;
        private int _slot;

        internal AliveEntityEnumerator(EntityRegistry registry)
        {
            _registry = registry;
            _version = registry._version;
            _slot = 0;
        }

        public readonly EntityId Current
        {
            get
            {
                CheckVersion();
                if (_slot == 0 || _slot > _registry._allocatedSlotCount)
                {
                    throw new InvalidOperationException("The enumerator is not positioned on a live entity.");
                }

                return EntityId.FromParts((uint)_slot, _registry._generations[_slot]);
            }
        }

        public bool MoveNext()
        {
            CheckVersion();
            while (_slot < _registry._allocatedSlotCount)
            {
                _slot++;
                if (_registry._alive[_slot])
                {
                    return true;
                }
            }

            _slot = _registry._allocatedSlotCount + 1;
            return false;
        }

        private readonly void CheckVersion()
        {
            if (_registry._version != _version)
            {
                throw new InvalidOperationException("The entity registry changed during enumeration.");
            }
        }
    }
}
