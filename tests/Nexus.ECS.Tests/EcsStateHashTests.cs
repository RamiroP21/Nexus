using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class EcsStateHashTests
{
    [Fact]
    public void SameSequenceHasSameHashRegardlessOfInitialCapacity()
    {
        EcsWorld first = BuildWorld(1);
        EcsWorld second = BuildWorld(512);

        Assert.Equal(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void ComponentRegistrationOrderIsNotPersistentIdentity()
    {
        var first = new EcsWorld("same", 10);
        first.RegisterComponent<TestPosition>();
        first.RegisterComponent<TestVelocity>();
        var second = new EcsWorld("same", 10);
        second.RegisterComponent<TestVelocity>();
        second.RegisterComponent<TestPosition>();
        first.AddComponent(first.CreateEntity(), new TestPosition(8));
        second.AddComponent(second.CreateEntity(), new TestPosition(8));

        Assert.Equal(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void DifferentComponentPayloadChangesHash()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        first.AddComponent(first.CreateEntity(), new TestPosition(1));
        second.AddComponent(second.CreateEntity(), new TestPosition(2));

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void DifferentComponentTypeWithSamePayloadChangesHash()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        first.AddComponent(first.CreateEntity(), new TestPosition(7));
        second.AddComponent(second.CreateEntity(), new TestVelocity(7));

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void RegisteredEmptySchemasContributeToHash()
    {
        var first = new EcsWorld("same", 10);
        var second = new EcsWorld("same", 10);
        first.RegisterComponent<TestPosition>();
        second.RegisterComponent<TestVelocity>();

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void DeadSlotGenerationAffectsHashAndNextAllocation()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        first.DestroyEntity(first.CreateEntity());
        second.DestroyEntity(second.CreateEntity());
        second.DestroyEntity(second.CreateEntity());

        Assert.Equal(0, first.EntityCount);
        Assert.Equal(0, second.EntityCount);
        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
        Assert.NotEqual(first.CreateEntity(), second.CreateEntity());
    }

    [Fact]
    public void FreeListOrderingAffectsHashAndFutureIdSequence()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        EntityId firstA = first.CreateEntity();
        EntityId firstB = first.CreateEntity();
        EntityId secondA = second.CreateEntity();
        EntityId secondB = second.CreateEntity();
        first.DestroyEntity(firstA);
        first.DestroyEntity(firstB);
        second.DestroyEntity(secondB);
        second.DestroyEntity(secondA);

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
        Assert.NotEqual(first.CreateEntity(), second.CreateEntity());
    }

    [Fact]
    public void ComponentDenseOrderIsHashedBecauseQueriesUseIt()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        EntityId firstA = first.CreateEntity();
        EntityId firstB = first.CreateEntity();
        EntityId secondA = second.CreateEntity();
        EntityId secondB = second.CreateEntity();
        first.AddComponent(firstA, new TestPosition(1));
        first.AddComponent(firstB, new TestPosition(2));
        second.AddComponent(secondB, new TestPosition(2));
        second.AddComponent(secondA, new TestPosition(1));

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PendingStructuralCommandPayloadTypeTargetAndOperationAreHashed(int difference)
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        EntityId firstA = first.CreateEntity();
        first.CreateEntity();
        EntityId secondA = second.CreateEntity();
        EntityId secondB = second.CreateEntity();
        first.Commands.AddComponent(firstA, new TestPosition(7));
        switch (difference)
        {
            case 0:
                second.Commands.AddComponent(secondA, new TestPosition(8));
                break;
            case 1:
                second.Commands.AddComponent(secondA, new TestVelocity(7));
                break;
            case 2:
                second.Commands.AddComponent(secondB, new TestPosition(7));
                break;
            default:
                second.Commands.RemoveComponent<TestPosition>(secondA);
                break;
        }

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void PendingStructuralCommandOrderChangesHashAndFutureState()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        EntityId firstEntity = first.CreateEntity();
        EntityId secondEntity = second.CreateEntity();
        first.Commands.AddComponent(firstEntity, new TestPosition(7));
        first.Commands.RemoveComponent<TestPosition>(firstEntity);
        second.Commands.RemoveComponent<TestPosition>(secondEntity);
        second.Commands.AddComponent(secondEntity, new TestPosition(7));

        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
        first.Commands.Playback();
        second.Commands.Playback();
        Assert.False(first.HasComponent<TestPosition>(firstEntity));
        Assert.True(second.HasComponent<TestPosition>(secondEntity));
    }

    [Fact]
    public void IdenticalPendingCommandsHaveIdenticalHash()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        first.Commands.AddComponent(first.CreateEntity(), new TestPosition(7));
        second.Commands.AddComponent(second.CreateEntity(), new TestPosition(7));
        first.Commands.CreateEntity();
        second.Commands.CreateEntity();

        Assert.Equal(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    private static EcsWorld BuildWorld(int capacity)
    {
        EcsWorld world = EcsTestWorld.Create(capacity);
        var ids = new EntityId[20];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = world.CreateEntity();
            world.AddComponent(ids[i], new TestPosition(i));
        }

        world.DestroyEntity(ids[3]);
        world.DestroyEntity(ids[12]);
        world.AddComponent(world.CreateEntity(), new TestPosition(88));
        world.Commands.RemoveComponent<TestPosition>(ids[8]);
        return world;
    }
}
