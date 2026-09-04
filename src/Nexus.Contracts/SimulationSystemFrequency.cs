using Nexus.Core;

namespace Nexus.Contracts;

/// <summary>
/// Defines a system cadence as a positive integer number of ticks.
/// Tick zero is always on cadence.
/// </summary>
public readonly struct SimulationSystemFrequency : IEquatable<SimulationSystemFrequency>
{
    public SimulationSystemFrequency(uint interval)
    {
        ArgumentOutOfRangeException.ThrowIfZero(interval);
        Interval = interval;
    }

    public uint Interval { get; }

    public static SimulationSystemFrequency EveryTick { get; } = new(1);

    public static SimulationSystemFrequency EveryTwoTicks { get; } = new(2);

    public static SimulationSystemFrequency EveryTenTicks { get; } = new(10);

    public static SimulationSystemFrequency Every(uint interval) => new(interval);

    public bool IsDue(SimulationTick tick)
    {
        EnsureValid();
        return tick.Value % Interval == 0;
    }

    public bool Equals(SimulationSystemFrequency other) => Interval == other.Interval;

    public override bool Equals(object? obj) =>
        obj is SimulationSystemFrequency other && Equals(other);

    public override int GetHashCode() => Interval.GetHashCode();

    public override string ToString()
    {
        EnsureValid();
        return Interval == 1 ? "Every tick" : $"Every {Interval} ticks";
    }

    public static bool operator ==(
        SimulationSystemFrequency left,
        SimulationSystemFrequency right) => left.Equals(right);

    public static bool operator !=(
        SimulationSystemFrequency left,
        SimulationSystemFrequency right) => !left.Equals(right);

    private void EnsureValid()
    {
        if (Interval == 0)
        {
            throw new InvalidOperationException(
                "The default simulation-system frequency is invalid. Use EveryTick or Every(n).");
        }
    }
}
