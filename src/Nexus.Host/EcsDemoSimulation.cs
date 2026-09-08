using System.Diagnostics;
using Nexus.Core;
using Nexus.ECS;
using Nexus.Simulation;

namespace Nexus.Host;

internal static class EcsDemoSimulation
{
    public static EcsDemoResult RunFresh(int entityCount, ulong tickCount)
    {
        long memoryBefore = GC.GetTotalMemory(false);
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long creationStarted = Stopwatch.GetTimestamp();
        var world = new EcsWorld("nexus.demo.ecs.world.v1", 10, entityCount);
        world.RegisterComponent<DemoValue>();
        world.RegisterComponent<DemoDelta>();
        world.RegisterComponent<FidelityComponent>();
        world.FreezeComponents();
        EntityId firstEntity = default;
        EntityId staleEntity = default;
        for (int i = 0; i < entityCount; i++)
        {
            EntityId entity = world.CreateEntity();
            EcsDemoSystem.InitializeEntity(world, entity);
            if (i == 0)
            {
                firstEntity = entity;
            }

            if (entity.Index == 10)
            {
                staleEntity = entity;
            }
        }

        TimeSpan creationElapsed = Stopwatch.GetElapsedTime(creationStarted);
        long creationAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var system = new EcsDemoSystem(world, entityCount);
        var pipeline = new SimulationPipeline(new SimulationOptions(new DeterministicSeed(123_456_789), 60, tickCount))
            .RegisterSystem(system)
            .RegisterStateContributor(world)
            .RegisterStateContributor(system);

        string initialHash = pipeline.ComputeStateHash().Hex;
        world.Commands.TransitionFidelity(firstEntity, SimulationFidelity.Simplified);
        string pendingHash = pipeline.ComputeStateHash().Hex;
        long simulationAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long simulationStarted = Stopwatch.GetTimestamp();
        SimulationRunResult result = pipeline.Run();
        TimeSpan simulationElapsed = Stopwatch.GetElapsedTime(simulationStarted);
        long simulationAllocated = GC.GetAllocatedBytesForCurrentThread() - simulationAllocatedBefore;

        var fidelityCounts = new CountFidelity();
        world.Query<FidelityComponent, CountFidelity>(ref fidelityCounts);
        var values = new VerifyValues(tickCount);
        world.Query<DemoValue, DemoDelta, VerifyValues>(ref values);
        bool lifecyclePassed = world.EntityCount == entityCount
            && system.DestroyedCount == entityCount / 10
            && system.ReusedCount == system.DestroyedCount
            && (entityCount < 10 || !world.IsAlive(staleEntity))
            && world.ComponentCount<DemoValue>() == entityCount
            && world.ComponentCount<DemoDelta>() == entityCount
            && world.ComponentCount<FidelityComponent>() == entityCount
            && fidelityCounts.Full == entityCount
            && values.Correct == entityCount
            && system.QueryRows == checked((long)tickCount * entityCount - entityCount / 10)
            && system.ObservedFidelityEvents == checked(entityCount * (tickCount == 5 ? 2 : 3))
            && world.Commands.Count == 0;

        return new EcsDemoResult(
            entityCount,
            tickCount,
            result.StateHashHex,
            pendingHash,
            !string.Equals(initialHash, pendingHash, StringComparison.Ordinal),
            lifecyclePassed,
            world.EntityCount,
            checked(entityCount * 3L),
            system.DestroyedCount,
            system.ReusedCount,
            system.ObservedFidelityEvents,
            system.QueryRows,
            creationElapsed,
            TimeSpan.FromSeconds(system.QueryTimestampTicks / (double)Stopwatch.Frequency),
            simulationElapsed,
            creationAllocated,
            system.QueryAllocatedBytes,
            simulationAllocated,
            GC.GetTotalMemory(false) - memoryBefore);
    }

    private struct VerifyValues(ulong ticks) : IQueryAction<DemoValue, DemoDelta>
    {
        public int Correct;

        public void Execute(EntityId entity, ref DemoValue value, ref DemoDelta delta)
        {
            // Independently derived sum of per-tick fidelity multipliers. Replacements miss ticks 0/1;
            // entity 1 alone is promoted at tick 0, other survivors at tick 2.
            long multiplier = checked(4 * (long)ticks - (entity.Generation == 2 ? 14 : entity.Index == 1 ? 10 : 12));
            long expectedDelta = entity.Index % 7 + 1;
            long expected = checked(entity.Index * 17L + entity.Generation + expectedDelta * multiplier);
            if (delta.Value == expectedDelta && value.Value == expected)
            {
                Correct++;
            }
        }
    }

    private struct CountFidelity : IQueryAction<FidelityComponent>
    {
        public int Full;

        public void Execute(EntityId entity, ref FidelityComponent fidelity)
        {
            if (fidelity.Tier == SimulationFidelity.Full)
            {
                Full++;
            }
        }
    }
}

internal sealed record EcsDemoResult(
    int InitialEntityCount,
    ulong TickCount,
    string FinalHash,
    string PendingHash,
    bool PendingHashChanged,
    bool LifecyclePassed,
    int AliveEntities,
    long ComponentCount,
    int DestroyedEntities,
    int ReusedSlots,
    int ObservedFidelityEvents,
    long QueryRows,
    TimeSpan CreationElapsed,
    TimeSpan QueryElapsed,
    TimeSpan SimulationElapsed,
    long CreationAllocatedBytes,
    long QueryAllocatedBytes,
    long SimulationAllocatedBytes,
    long ApproximateHeapDelta);
