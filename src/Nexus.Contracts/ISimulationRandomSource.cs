namespace Nexus.Contracts;

/// <summary>
/// Exposes the deterministic random operations available to simulation code.
/// </summary>
public interface ISimulationRandomSource
{
    uint NextUInt32();

    ulong NextUInt64();

    int NextInt(int minInclusive, int maxExclusive);

    double NextDouble();
}
