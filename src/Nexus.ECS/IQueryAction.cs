using Nexus.Core;

namespace Nexus.ECS;

/// <summary>Receives a row for the duration of a synchronous query callback.</summary>
public interface IQueryAction<T> where T : unmanaged, IComponent<T>
{
    void Execute(EntityId entity, ref T component);
}

/// <summary>Receives two related components without boxing or per-row allocations.</summary>
public interface IQueryAction<TFirst, TSecond>
    where TFirst : unmanaged, IComponent<TFirst>
    where TSecond : unmanaged, IComponent<TSecond>
{
    void Execute(EntityId entity, ref TFirst first, ref TSecond second);
}

/// <summary>Receives three related components without boxing or per-row allocations.</summary>
public interface IQueryAction<TFirst, TSecond, TThird>
    where TFirst : unmanaged, IComponent<TFirst>
    where TSecond : unmanaged, IComponent<TSecond>
    where TThird : unmanaged, IComponent<TThird>
{
    void Execute(EntityId entity, ref TFirst first, ref TSecond second, ref TThird third);
}
