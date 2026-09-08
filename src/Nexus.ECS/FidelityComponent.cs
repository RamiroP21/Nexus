using Nexus.Core;

namespace Nexus.ECS;

/// <summary>An entity's detail level. The default is Statistical.</summary>
public readonly record struct FidelityComponent : IComponent<FidelityComponent>
{
    public FidelityComponent(SimulationFidelity tier)
    {
        FidelityTransitions.ValidateTier(tier);
        Tier = tier;
    }

    public SimulationFidelity Tier { get; }

    public static string StableId => "nexus.ecs.fidelity.v1";

    public static void ContributeToHash(in FidelityComponent value, StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add((byte)value.Tier);
    }
}
