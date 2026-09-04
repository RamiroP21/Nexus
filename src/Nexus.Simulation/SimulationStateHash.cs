using System.Globalization;

namespace Nexus.Simulation;

public readonly record struct SimulationStateHash
{
    public SimulationStateHash(ulong value) => Value = value;

    public ulong Value { get; }

    public string Hex => Value.ToString("X16", CultureInfo.InvariantCulture);
}
