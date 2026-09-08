using Nexus.Core;

namespace Nexus.ECS;

/// <summary>
/// Provides a semantic, versioned component identity and explicit deterministic payload hashing.
/// Static dispatch preserves unmanaged contiguous storage without boxing component values.
/// </summary>
/// <typeparam name="TSelf">The unmanaged component implementing this contract.</typeparam>
public interface IComponent<TSelf>
    where TSelf : unmanaged, IComponent<TSelf>
{
    /// <summary>
    /// Gets the persistent, unique, versioned identity of this component schema.
    /// </summary>
    static abstract string StableId { get; }

    /// <summary>
    /// Adds every future-affecting field in an explicit order, without mutation or side effects.
    /// </summary>
    static abstract void ContributeToHash(in TSelf value, StableHasher64 hasher);
}
