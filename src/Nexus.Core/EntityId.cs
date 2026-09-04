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

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static bool operator <(EntityId left, EntityId right) => left.Value < right.Value;

    public static bool operator <=(EntityId left, EntityId right) => left.Value <= right.Value;

    public static bool operator >(EntityId left, EntityId right) => left.Value > right.Value;

    public static bool operator >=(EntityId left, EntityId right) => left.Value >= right.Value;
}
