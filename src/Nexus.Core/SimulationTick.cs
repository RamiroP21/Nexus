using System.Globalization;

namespace Nexus.Core;

/// <summary>
/// Identifies an exact, discrete step of the deterministic simulation.
/// </summary>
public readonly record struct SimulationTick(ulong Value) : IComparable<SimulationTick>
{
    /// <summary>
    /// Gets the first tick of a simulation.
    /// </summary>
    public static SimulationTick Initial => default;

    /// <summary>
    /// Returns the immediately following tick.
    /// </summary>
    /// <exception cref="OverflowException">The current tick is the largest representable tick.</exception>
    public SimulationTick Next() => new(checked(Value + 1UL));

    public int CompareTo(SimulationTick other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static SimulationTick operator +(SimulationTick tick, ulong count) =>
        new(checked(tick.Value + count));

    public static SimulationTick operator ++(SimulationTick tick) => tick.Next();

    public static bool operator <(SimulationTick left, SimulationTick right) => left.Value < right.Value;

    public static bool operator <=(SimulationTick left, SimulationTick right) => left.Value <= right.Value;

    public static bool operator >(SimulationTick left, SimulationTick right) => left.Value > right.Value;

    public static bool operator >=(SimulationTick left, SimulationTick right) => left.Value >= right.Value;
}
