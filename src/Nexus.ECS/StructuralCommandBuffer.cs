using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.ECS;

/// <summary>
/// Stores ordered structural intents compactly. Payloads live in typed arrays per schema;
/// a simulation command can flush this buffer in the existing end-of-tick command phase.
/// </summary>
public sealed class StructuralCommandBuffer
{
    private readonly EcsWorld _world;
    private readonly List<Operation> _operations = [];
    private readonly PlaybackCommand _playbackCommand;

    internal StructuralCommandBuffer(EcsWorld world)
    {
        _world = world;
        _playbackCommand = new PlaybackCommand(this);
    }

    public int Count => _operations.Count;

    public bool IsApplying { get; private set; }

    internal bool IsPublishingEvent { get; private set; }

    /// <summary>
    /// Enqueues creation of an empty entity. Its ID is assigned at playback, in FIFO order.
    /// No provisional EntityId is issued; live IDs can be copied at the next boundary.
    /// </summary>
    public void CreateEntity()
    {
        EnsureEnqueueAllowed();
        _operations.Add(new Operation(OperationKind.Create, EntityId.Invalid, -1, -1, default));
    }

    public void DestroyEntity(EntityId entity)
    {
        EnsureEnqueueAllowed();
        _operations.Add(new Operation(OperationKind.Destroy, entity, -1, -1, default));
    }

    public void AddComponent<T>(EntityId entity, in T value) where T : unmanaged, IComponent<T>
    {
        EnsureEnqueueAllowed();
        ComponentStorage<T> store = _world.GetStorage<T>();
        int payload = store.EnqueuePayload(value);
        _operations.Add(new Operation(OperationKind.Add, entity, store.RuntimeId, payload, default));
    }

    public void RemoveComponent<T>(EntityId entity) where T : unmanaged, IComponent<T>
    {
        EnsureEnqueueAllowed();
        ComponentStorage<T> store = _world.GetStorage<T>();
        _operations.Add(new Operation(OperationKind.Remove, entity, store.RuntimeId, -1, default));
    }

    /// <summary>Validates the current-to-target transition at playback, after prior operations.</summary>
    public void TransitionFidelity(EntityId entity, SimulationFidelity target)
    {
        EnsureEnqueueAllowed();
        FidelityTransitions.ValidateTier(target);
        _ = _world.GetStorage<FidelityComponent>();
        _operations.Add(new Operation(OperationKind.Fidelity, entity, -1, -1, target));
    }

    /// <summary>
    /// Applies FIFO at an idle structural boundary. When a simulation context is provided,
    /// fidelity events follow its existing next-tick publication policy.
    /// A failed operation faults the ECS without pretending to roll back earlier changes.
    /// </summary>
    public int Playback(ISimulationContext? context = null)
    {
        _world.EnsureStructuralMutationAllowed();
        if (IsApplying)
        {
            throw new InvalidOperationException("Structural playback cannot be reentered.");
        }

        _world.FreezeComponents();
        IsApplying = true;
        try
        {
            int count = _operations.Count;
            for (int index = 0; index < count; index++)
            {
                Apply(_operations[index], context);
            }

            _operations.Clear();
            _world.ClearPendingPayloads();
            return count;
        }
        catch
        {
            _world.MarkFaulted();
            throw;
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <summary>
    /// The returned command targets this world by stable ID; register the world as a state
    /// contributor so its complete queue is included in the simulation hash.
    /// </summary>
    public ISimulationCommand AsSimulationCommand() => _playbackCommand;

    internal void ContributeToHash(StableHasher64 hasher)
    {
        if (IsApplying)
        {
            throw new InvalidOperationException("Structural commands cannot be hashed during playback.");
        }

        hasher.Add("Nexus.ECS.StructuralCommands.v1");
        hasher.Add(checked((ulong)_operations.Count));
        var child = new StableHasher64();
        for (int index = 0; index < _operations.Count; index++)
        {
            Operation operation = _operations[index];
            child.Reset();
            child.Add((byte)operation.Kind);
            child.Add(operation.Entity);
            if (operation.Kind is OperationKind.Add or OperationKind.Remove)
            {
                IComponentStorage store = _world.GetStorage(operation.ComponentId);
                child.Add(store.StableId);
                if (operation.Kind == OperationKind.Add)
                {
                    store.ContributePendingPayload(operation.PayloadIndex, child);
                }
            }
            else if (operation.Kind == OperationKind.Fidelity)
            {
                child.Add((byte)operation.TargetFidelity);
            }

            hasher.Add(checked((ulong)index));
            hasher.Add(child.Value);
        }
    }

    private void EnsureEnqueueAllowed()
    {
        _world.EnsureMutationAllowed();
        if (IsApplying)
        {
            throw new InvalidOperationException("Structural commands cannot be enqueued during playback.");
        }
    }

    private void Apply(Operation operation, ISimulationContext? context)
    {
        switch (operation.Kind)
        {
            case OperationKind.Create:
                _world.CreateEntity();
                break;
            case OperationKind.Destroy:
                _world.DestroyEntity(operation.Entity);
                break;
            case OperationKind.Add:
                _world.EnsureAlive(operation.Entity);
                _world.GetStorage(operation.ComponentId).ApplyPendingAdd(operation.Entity, operation.PayloadIndex);
                break;
            case OperationKind.Remove:
                if (_world.IsAlive(operation.Entity))
                {
                    _world.GetStorage(operation.ComponentId).Remove(operation.Entity);
                }

                break;
            case OperationKind.Fidelity:
                SimulationFidelity current = _world.GetComponent<FidelityComponent>(operation.Entity).Tier;
                if (!FidelityTransitions.IsValid(current, operation.TargetFidelity))
                {
                    throw new InvalidOperationException("Fidelity transitions must move exactly one adjacent tier.");
                }

                _world.SetComponent(operation.Entity, new FidelityComponent(operation.TargetFidelity));
                if (context is not null)
                {
                    IsPublishingEvent = true;
                    try
                    {
                        context.PublishEvent(new FidelityChangedEvent(
                            _world.Id, operation.Entity, current, operation.TargetFidelity));
                    }
                    finally
                    {
                        IsPublishingEvent = false;
                    }
                }
                break;
            default:
                throw new InvalidOperationException("Unknown structural operation.");
        }
    }

    private enum OperationKind : byte
    {
        Create = 1,
        Destroy = 2,
        Add = 3,
        Remove = 4,
        Fidelity = 5,
    }

    private readonly record struct Operation(
        OperationKind Kind,
        EntityId Entity,
        int ComponentId,
        int PayloadIndex,
        SimulationFidelity TargetFidelity);

    private sealed class PlaybackCommand(StructuralCommandBuffer buffer) : ISimulationCommand
    {
        public string StableTypeId => "nexus.ecs.structural-playback-command.v1";

        public void Execute(ISimulationContext context) => buffer.Playback(context);

        public void ContributeToHash(StableHasher64 hasher)
        {
            ArgumentNullException.ThrowIfNull(hasher);
            hasher.Add(buffer._world.Id);
        }
    }
}
