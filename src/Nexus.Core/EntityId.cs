using System.Globalization;

namespace Nexus.Core;

/// <summary>
/// A stable identity for an entity. Value zero is reserved for an unassigned identity.
/// </summary>
public readonly record struct EntityId(ulong Value) : IComparable<EntityId>
{
    /// <summary>
    /// Gets the unassigned entity identity.
    /// </summary>
    public static EntityId Invalid => default;

    /// <summary>
    /// Gets whether this identity is assigned.
    /// </summary>
    public bool IsValid => Value != 0UL;

    /// <summary>
    /// Gets the low 32-bit slot index. Registry-managed identities reserve index zero.
    /// </summary>
    public uint Index => unchecked((uint)Value);

    /// <summary>
    /// Gets the high 32-bit slot generation. Registry-managed identities start at generation one.
    /// </summary>
    public uint Generation => (uint)(Value >> 32);

    /// <summary>
    /// Creates a generation-protected registry identity without changing the existing raw-value encoding.
    /// </summary>
    public static EntityId FromParts(uint index, uint generation)
    {
        ArgumentOutOfRangeException.ThrowIfZero(index);
        ArgumentOutOfRangeException.ThrowIfZero(generation);
        return new EntityId(((ulong)generation << 32) | index);
    }

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static bool operator <(EntityId left, EntityId right) => left.Value < right.Value;

    public static bool operator <=(EntityId left, EntityId right) => left.Value <= right.Value;

    public static bool operator >(EntityId left, EntityId right) => left.Value > right.Value;

    public static bool operator >=(EntityId left, EntityId right) => left.Value >= right.Value;
}
