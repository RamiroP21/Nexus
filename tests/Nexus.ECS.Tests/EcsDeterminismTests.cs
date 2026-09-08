using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class EcsDeterminismTests
{
    [Fact]
    public void TwoIndependentHundredThousandEntityRunsMatchAtEveryTickBoundary()
    {
        ulong[] first = RunScenario();
        ulong[] second = RunScenario();

        Assert.Equal(first, second);
        Assert.NotEqual(first[0], first[^1]);
    }

    private static ulong[] RunScenario()
    {
        const int entityCount = 100_000;
        const int tickCount = 20;
        EcsWorld world = EcsTestWorld.Create(entityCount);
        for (int i = 0; i < entityCount; i++)
        {
            AddEntity(world, i);
        }

        var hashes = new ulong[tickCount];
        for (int tick = 0; tick < tickCount; tick++)
        {
            var action = new AdvanceAction(world, tick);
            world.Query<TestPosition, TestVelocity, TestValue, AdvanceAction>(ref action);
            world.Commands.Playback();
            if (tick == 10)
            {
                Assert.Equal(90_000, world.EntityCount);
                for (int i = 0; i < entityCount / 10; i++)
                {
                    AddEntity(world, entityCount + i);
                }
            }

            hashes[tick] = EcsTestWorld.Hash(world);
        }

        Assert.Equal(entityCount, world.EntityCount);
        Assert.Equal(entityCount, world.ComponentCount<TestPosition>());
        Assert.Equal(entityCount, world.ComponentCount<TestVelocity>());
        Assert.Equal(entityCount, world.ComponentCount<TestValue>());
        Assert.Equal(entityCount, world.ComponentCount<FidelityComponent>());
        Assert.Equal(0, world.Commands.Count);
        Assert.False(world.IsFaulted);
        var verify = new VerifyFinalValues(world);
        world.Query<TestPosition, TestVelocity, TestValue, VerifyFinalValues>(ref verify);
        return hashes;
    }

    private static void AddEntity(EcsWorld world, int value)
    {
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(value));
        world.AddComponent(entity, new TestVelocity((value % 7) + 1));
        world.AddComponent(entity, new TestValue(value));
        world.AddComponent(entity, new FidelityComponent(SimulationFidelity.Statistical));
    }

    private readonly struct VerifyFinalValues(EcsWorld world) : IQueryAction<TestPosition, TestVelocity, TestValue>
    {
        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second, ref TestValue third)
        {
            bool replacement = entity.Generation == 2;
            Assert.Equal(replacement ? 2U : 1U, entity.Generation);
            Assert.Equal((third.Value % 7) + 1, second.Value);
            Assert.Equal(third.Value + second.Value * (replacement ? 9 : 20), first.Value);
            SimulationFidelity expected = !replacement && third.Value % 7 == 0
                ? SimulationFidelity.Simplified : SimulationFidelity.Statistical;
            Assert.Equal(expected, world.GetComponent<FidelityComponent>(entity).Tier);
        }
    }

    private readonly struct AdvanceAction(EcsWorld world, int tick) : IQueryAction<TestPosition, TestVelocity, TestValue>
    {
        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second, ref TestValue third)
        {
            first.Value += second.Value;
            if (tick == 5 && third.Value % 7 == 0)
            {
                world.Commands.TransitionFidelity(entity, SimulationFidelity.Simplified);
            }

            if (tick == 10 && third.Value % 10 == 0)
            {
                world.Commands.DestroyEntity(entity);
            }
        }
    }
}
