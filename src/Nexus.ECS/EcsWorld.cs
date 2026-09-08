using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.ECS;

/// <summary>
/// Owns entity lifetimes, typed storage and structural boundaries for one deterministic ECS.
/// </summary>
public sealed partial class EcsWorld : IStateHashContributor
{
    private readonly EntityRegistry _entities;
    private readonly int _initialCapacity;
    private readonly Dictionary<Type, IComponentStorage> _storageByType = [];
    private readonly Dictionary<string, IComponentStorage> _storageByStableId = new(StringComparer.Ordinal);
    private readonly List<IComponentStorage> _stores = [];
    private IComponentStorage[] _orderedStores = [];
    private int _activeQueries;
    private bool _isHashing;

    public EcsWorld(string id, int order, int initialCapacity = 16)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        Id = id;
        Order = order;
        _initialCapacity = initialCapacity;
        _entities = new EntityRegistry(initialCapacity);
        Commands = new StructuralCommandBuffer(this);
    }

    public string Id { get; }

    public int Order { get; }

    public int EntityCount => _entities.AliveCount;

    public bool IsFrozen { get; private set; }

    public bool IsFaulted { get; private set; }

    public StructuralCommandBuffer Commands { get; }

    /// <summary>Registers one explicit schema before any entity, query or hash is created.</summary>
    public void RegisterComponent<T>() where T : unmanaged, IComponent<T>
    {
        EnsureStructuralMutationAllowed();
        if (IsFrozen)
        {
            throw new InvalidOperationException("Component registration is frozen.");
        }

        string stableId = T.StableId;
        if (string.IsNullOrWhiteSpace(stableId))
        {
            throw new InvalidOperationException("A component requires a non-empty stable schema ID.");
        }

        if (_storageByType.ContainsKey(typeof(T)) || _storageByStableId.ContainsKey(stableId))
        {
            throw new InvalidOperationException("The component type or stable schema ID is already registered.");
        }

        var store = new ComponentStorage<T>(stableId, _stores.Count, _initialCapacity);
        _storageByType.Add(typeof(T), store);
        _storageByStableId.Add(stableId, store);
        _stores.Add(store);
    }

    public void FreezeComponents()
    {
        EnsureUsable();
        if (IsFrozen)
        {
            return;
        }

        EnsureStructuralMutationAllowed();
        _orderedStores = _stores.ToArray();
        Array.Sort(_orderedStores, static (left, right) =>
            StringComparer.Ordinal.Compare(left.StableId, right.StableId));
        IsFrozen = true;
    }

    public EntityId CreateEntity()
    {
        EnsureStructuralMutationAllowed();
        FreezeComponents();
        return _entities.CreateEntity();
    }

    public bool DestroyEntity(EntityId entity)
    {
        EnsureStructuralMutationAllowed();
        if (!_entities.IsAlive(entity))
        {
            return false;
        }

        foreach (IComponentStorage store in _stores)
        {
            store.Remove(entity);
        }

        return _entities.DestroyEntity(entity);
    }

    public bool IsAlive(EntityId entity)
    {
        EnsureUsable();
        return _entities.IsAlive(entity);
    }

    public int CopyEntities(Span<EntityId> destination)
    {
        EnsureUsable();
        if (destination.Length < EntityCount)
        {
            throw new ArgumentException("The destination must fit all live entities.", nameof(destination));
        }

        int index = 0;
        foreach (EntityId entity in _entities.GetAliveEntities())
        {
            destination[index++] = entity;
        }

        return index;
    }

    public void AddComponent<T>(EntityId entity, in T value) where T : unmanaged, IComponent<T>
    {
        EnsureStructuralMutationAllowed();
        EnsureAlive(entity);
        GetStorage<T>().Data.Add(entity, value);
    }

    public bool RemoveComponent<T>(EntityId entity) where T : unmanaged, IComponent<T>
    {
        EnsureStructuralMutationAllowed();
        ComponentStorage<T> store = GetStorage<T>();
        return _entities.IsAlive(entity) && store.Data.Remove(entity);
    }

    public bool HasComponent<T>(EntityId entity) where T : unmanaged, IComponent<T>
    {
        ComponentStorage<T> store = GetStorage<T>();
        return _entities.IsAlive(entity) && store.Data.Has(entity);
    }

    /// <summary>Returns a copy; use SetComponent or a query callback to update stored data.</summary>
    public T GetComponent<T>(EntityId entity) where T : unmanaged, IComponent<T>
    {
        EnsureAlive(entity);
        return GetStorage<T>().Data.Get(entity);
    }

    public bool TryGetComponent<T>(EntityId entity, out T value) where T : unmanaged, IComponent<T>
    {
        ComponentStorage<T> store = GetStorage<T>();
        if (!_entities.IsAlive(entity))
        {
            value = default;
            return false;
        }

        return store.Data.TryGet(entity, out value);
    }

    public void SetComponent<T>(EntityId entity, in T value) where T : unmanaged, IComponent<T>
    {
        EnsureMutationAllowed();
        EnsureAlive(entity);
        GetStorage<T>().Data.Get(entity) = value;
    }

    public int ComponentCount<T>() where T : unmanaged, IComponent<T> => GetStorage<T>().Data.Count;

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        EnsureUsable();
        if (_activeQueries != 0 || Commands.IsApplying || _isHashing)
        {
            throw new InvalidOperationException("An ECS hash requires a complete, idle structural boundary.");
        }

        FreezeComponents();
        _isHashing = true;
        try
        {
            hasher.Add("Nexus.ECS.World.v1");
            hasher.Add(Id);
            hasher.Add(Order);
            _entities.ContributeToHash(hasher);
            hasher.Add(checked((ulong)_orderedStores.Length));
            var child = new StableHasher64();
            for (int index = 0; index < _orderedStores.Length; index++)
            {
                IComponentStorage store = _orderedStores[index];
                child.Reset();
                child.Add("Nexus.ECS.ComponentPartition.v1");
                child.Add(store.StableId);
                store.ContributeToHash(child);
                hasher.Add(checked((ulong)index));
                hasher.Add(child.Value);
            }

            Commands.ContributeToHash(hasher);
        }
        finally
        {
            _isHashing = false;
        }
    }

    internal ComponentStorage<T> GetStorage<T>() where T : unmanaged, IComponent<T>
    {
        EnsureUsable();
        // Runtime type identity is only an O(1) lookup; it never enters canonical ordering or hashing.
        if (!_storageByType.TryGetValue(typeof(T), out IComponentStorage? store))
        {
            throw new InvalidOperationException("The component schema has not been registered.");
        }

        return (ComponentStorage<T>)store;
    }

    internal IComponentStorage GetStorage(int runtimeId) => _stores[runtimeId];

    internal void ClearPendingPayloads()
    {
        foreach (IComponentStorage store in _stores)
        {
            store.ClearPendingPayloads();
        }
    }

    internal void EnsureAlive(EntityId entity)
    {
        EnsureUsable();
        if (!_entities.IsAlive(entity))
        {
            throw new InvalidOperationException("The entity is not alive in this ECS or its generation is stale.");
        }
    }

    internal void EnsureMutationAllowed()
    {
        EnsureUsable();
        if (_isHashing || Commands.IsPublishingEvent)
        {
            throw new InvalidOperationException("State cannot be changed during hashing or an event publication callback.");
        }
    }

    internal void EnsureStructuralMutationAllowed()
    {
        EnsureMutationAllowed();
        if (_activeQueries != 0)
        {
            throw new InvalidOperationException("Structural changes must be deferred until all queries have completed.");
        }
    }

    internal void MarkFaulted() => IsFaulted = true;

    private void EnsureUsable()
    {
        if (IsFaulted)
        {
            throw new InvalidOperationException("The ECS faulted during structural playback and cannot continue.");
        }
    }

    /// <summary>
    /// Visits the first component's dense order. References only live within the callback;
    /// structural mutations are rejected for the entire query, including nested queries.
    /// </summary>
    public void Query<T, TAction>(ref TAction action)
        where T : unmanaged, IComponent<T>
        where TAction : struct, IQueryAction<T>
    {
        EnsureMutationAllowed();
        FreezeComponents();
        SparseSet<T> data = GetStorage<T>().Data;
        _activeQueries++;
        try
        {
            for (int index = 0; index < data.Count; index++)
            {
                action.Execute(data.GetEntityAt(index), ref data.GetAt(index));
            }
        }
        finally
        {
            _activeQueries--;
        }
    }

    public void Query<TFirst, TSecond, TAction>(ref TAction action)
        where TFirst : unmanaged, IComponent<TFirst>
        where TSecond : unmanaged, IComponent<TSecond>
        where TAction : struct, IQueryAction<TFirst, TSecond>
    {
        EnsureMutationAllowed();
        FreezeComponents();
        SparseSet<TFirst> first = GetStorage<TFirst>().Data;
        SparseSet<TSecond> second = GetStorage<TSecond>().Data;
        _activeQueries++;
        try
        {
            for (int index = 0; index < first.Count; index++)
            {
                EntityId entity = first.GetEntityAt(index);
                if (second.Has(entity))
                {
                    action.Execute(entity, ref first.GetAt(index), ref second.Get(entity));
                }
            }
        }
        finally
        {
            _activeQueries--;
        }
    }

    public void Query<TFirst, TSecond, TThird, TAction>(ref TAction action)
        where TFirst : unmanaged, IComponent<TFirst>
        where TSecond : unmanaged, IComponent<TSecond>
        where TThird : unmanaged, IComponent<TThird>
        where TAction : struct, IQueryAction<TFirst, TSecond, TThird>
    {
        EnsureMutationAllowed();
        FreezeComponents();
        SparseSet<TFirst> first = GetStorage<TFirst>().Data;
        SparseSet<TSecond> second = GetStorage<TSecond>().Data;
        SparseSet<TThird> third = GetStorage<TThird>().Data;
        _activeQueries++;
        try
        {
            for (int index = 0; index < first.Count; index++)
            {
                EntityId entity = first.GetEntityAt(index);
                if (second.Has(entity) && third.Has(entity))
                {
                    action.Execute(entity, ref first.GetAt(index), ref second.Get(entity), ref third.Get(entity));
                }
            }
        }
        finally
        {
            _activeQueries--;
        }
    }
}
