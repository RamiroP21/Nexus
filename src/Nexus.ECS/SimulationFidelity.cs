namespace Nexus.ECS;

/// <summary>Ordered simulation detail levels; this phase defines no selection policy.</summary>
public enum SimulationFidelity : byte
{
    Statistical = 0,
    Simplified = 1,
    Agent = 2,
    Full = 3,
}
