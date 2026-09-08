using System.Diagnostics;
using Nexus.Contracts;
using Nexus.Core;
using Nexus.ECS;

namespace Nexus.Host;

/// <summary>Exercises neutral component data and structural boundaries without gameplay.</summary>
internal sealed class EcsDemoSystem : ISimulationSystem, IStateHashContributor
{
    private readonly EcsWorld _world;
    private readonly int _targetPopulation;
    private readonly ISimulationCommand _playback;
    private readonly RestorePopulationCommand _restore;

    public EcsDemoSystem(EcsWorld world, int targetPopulation)
    {
        _world = world;
        _targetPopulation = targetPopulation;
        _playback = world.Commands.AsSimulationCommand();
        _restore = new RestorePopulationCommand(world, targetPopulation);
    }

    public string Id => "nexus.demo.ecs.update.v1";

    public int Order => 100;

    public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

    public long QueryTimestampTicks { get; private set; }

    public long QueryAllocatedBytes { get; private set; }

    public long QueryRows { get; private set; }

    public int DestroyedCount { get; private set; }

    public int ReusedCount => _restore.ReusedCount;

    public int ObservedFidelityEvents { get; private set; }

    public void Execute(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var update = new UpdateValues();
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        _world.Query<DemoValue, DemoDelta, FidelityComponent, UpdateValues>(ref update);
        QueryTimestampTicks += Stopwatch.GetTimestamp() - started;
        QueryAllocatedBytes += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        QueryRows += update.Rows;

        for (int i = 0; i < context.Events.Count; i++)
        {
            if (context.Events[i] is FidelityChangedEvent)
            {
                ObservedFidelityEvents++;
            }
        }

        switch (context.CurrentTick.Value)
        {
            case 0:
                var destroy = new RequestDestruction(_world);
                _world.Query<DemoValue, RequestDestruction>(ref destroy);
                DestroyedCount = destroy.Count;
                context.EnqueueCommand(_playback);
                break;
            case 1:
                context.EnqueueCommand(_restore);
                break;
            case 2:
                RequestPromotion(context, SimulationFidelity.Statistical, SimulationFidelity.Simplified);
                break;
            case 3:
                RequestPromotion(context, SimulationFidelity.Simplified, SimulationFidelity.Agent);
                break;
            case 4:
                RequestPromotion(context, SimulationFidelity.Agent, SimulationFidelity.Full);
                break;
        }
    }

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(_world.Id);
        hasher.Add(_targetPopulation);
    }

    public static void InitializeEntity(EcsWorld world, EntityId entity)
    {
        world.AddComponent(entity, new DemoValue(checked((long)entity.Index * 17 + entity.Generation)));
        world.AddComponent(entity, new DemoDelta(entity.Index % 7 + 1));
        world.AddComponent(entity, new FidelityComponent(SimulationFidelity.Statistical));
    }

    private void RequestPromotion(
        ISimulationContext context,
        SimulationFidelity from,
        SimulationFidelity to)
    {
        var promote = new RequestFidelityTransition(_world, from, to);
        _world.Query<FidelityComponent, RequestFidelityTransition>(ref promote);
        context.EnqueueCommand(_playback);
    }

    private struct UpdateValues : IQueryAction<DemoValue, DemoDelta, FidelityComponent>
    {
        public long Rows;

        public void Execute(EntityId entity, ref DemoValue value, ref DemoDelta delta, ref FidelityComponent fidelity)
        {
            value.Value = checked(value.Value + delta.Value * ((int)fidelity.Tier + 1));
            Rows++;
        }
    }

    private struct RequestDestruction(EcsWorld world) : IQueryAction<DemoValue>
    {
        public int Count;

        public void Execute(EntityId entity, ref DemoValue value)
        {
            if (entity.Index % 10 == 0)
            {
                world.Commands.DestroyEntity(entity);
                Count++;
            }
        }
    }

    private readonly struct RequestFidelityTransition(
        EcsWorld world,
        SimulationFidelity from,
        SimulationFidelity to) : IQueryAction<FidelityComponent>
    {
        public void Execute(EntityId entity, ref FidelityComponent fidelity)
        {
            if (fidelity.Tier == from)
            {
                world.Commands.TransitionFidelity(entity, to);
            }
        }
    }

    private sealed class RestorePopulationCommand(EcsWorld world, int targetPopulation) : ISimulationCommand
    {
        public string StableTypeId => "nexus.demo.ecs.restore-population.v1";

        public int ReusedCount { get; private set; }

        public void Execute(ISimulationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            while (world.EntityCount < targetPopulation)
            {
                EntityId entity = world.CreateEntity();
                InitializeEntity(world, entity);
                if (entity.Generation > 1)
                {
                    ReusedCount++;
                }
            }
        }

        public void ContributeToHash(StableHasher64 hasher)
        {
            ArgumentNullException.ThrowIfNull(hasher);
            hasher.Add(world.Id);
            hasher.Add(targetPopulation);
        }
    }
}
