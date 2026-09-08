using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.ECS;

/// <summary>Immutable evidence of a completed structural transition.</summary>
public readonly record struct FidelityChangedEvent(
    string WorldId,
    EntityId Entity,
    SimulationFidelity From,
    SimulationFidelity To) : ISimulationEvent
{
    public string StableTypeId => "nexus.ecs.fidelity-changed-event.v1";

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(WorldId);
        hasher.Add(Entity);
        hasher.Add((byte)From);
        hasher.Add((byte)To);
    }
}
