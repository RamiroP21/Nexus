using Nexus.Core;

namespace Nexus.ECS;

public sealed partial class EcsWorld
{
    /// <summary>
    /// Visits matching entities in ascending full EntityId order, independently of dense layout.
    /// Scratch must fit the first store and is exclusively borrowed until this call returns.
    /// Sorting costs O(N log N); caller-owned reusable scratch avoids result allocations.
    /// References and structural guards follow the same rules as dense queries.
    /// </summary>
    public void QueryOrdered<T, TAction>(Span<EntityId> scratch, ref TAction action)
        where T : unmanaged, IComponent<T>
        where TAction : struct, IQueryAction<T>
    {
        EnsureMutationAllowed();
        FreezeComponents();
        SparseSet<T> data = GetStorage<T>().Data;
        Span<EntityId> entities = CopySortedDriver(data, scratch);
        _activeQueries++;
        try
        {
            foreach (EntityId entity in entities)
            {
                action.Execute(entity, ref data.Get(entity));
            }
        }
        finally
        {
            _activeQueries--;
        }
    }

    /// <summary>Ordered two-component intersection; scratch and lifetime rules match the single-component overload.</summary>
    public void QueryOrdered<TFirst, TSecond, TAction>(Span<EntityId> scratch, ref TAction action)
        where TFirst : unmanaged, IComponent<TFirst>
        where TSecond : unmanaged, IComponent<TSecond>
        where TAction : struct, IQueryAction<TFirst, TSecond>
    {
        EnsureMutationAllowed();
        FreezeComponents();
        SparseSet<TFirst> first = GetStorage<TFirst>().Data;
        SparseSet<TSecond> second = GetStorage<TSecond>().Data;
        Span<EntityId> entities = CopySortedDriver(first, scratch);
        _activeQueries++;
        try
        {
            foreach (EntityId entity in entities)
            {
                if (second.Has(entity))
                {
                    action.Execute(entity, ref first.Get(entity), ref second.Get(entity));
                }
            }
        }
        finally
        {
            _activeQueries--;
        }
    }

    /// <summary>Ordered three-component intersection; scratch and lifetime rules match the single-component overload.</summary>
    public void QueryOrdered<TFirst, TSecond, TThird, TAction>(Span<EntityId> scratch, ref TAction action)
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
        Span<EntityId> entities = CopySortedDriver(first, scratch);
        _activeQueries++;
        try
        {
            foreach (EntityId entity in entities)
            {
                if (second.Has(entity) && third.Has(entity))
                {
                    action.Execute(entity, ref first.Get(entity), ref second.Get(entity), ref third.Get(entity));
                }
            }
        }
        finally
        {
            _activeQueries--;
        }
    }

    private static Span<EntityId> CopySortedDriver<T>(SparseSet<T> driver, Span<EntityId> scratch)
        where T : unmanaged, IComponent<T>
    {
        if (scratch.Length < driver.Count)
        {
            throw new ArgumentException("Scratch must fit every entity in the first component store.", nameof(scratch));
        }

        Span<EntityId> entities = scratch[..driver.Count];
        for (int index = 0; index < entities.Length; index++)
        {
            entities[index] = driver.GetEntityAt(index);
        }

        entities.Sort();
        return entities;
    }
}
