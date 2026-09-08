using Nexus.Core;

namespace Nexus.ECS.Tests;

internal struct TestPosition(int value) : IComponent<TestPosition>
{
    public int Value = value;

    public static string StableId => "tests.ecs.position.v1";

    public static void ContributeToHash(in TestPosition value, StableHasher64 hasher) =>
        hasher.Add(value.Value);
}

internal struct TestVelocity(int value) : IComponent<TestVelocity>
{
    public int Value = value;

    public static string StableId => "tests.ecs.velocity.v1";

    public static void ContributeToHash(in TestVelocity value, StableHasher64 hasher) =>
        hasher.Add(value.Value);
}

internal struct TestValue(int value) : IComponent<TestValue>
{
    public int Value = value;

    public static string StableId => "tests.ecs.value.v1";

    public static void ContributeToHash(in TestValue value, StableHasher64 hasher) =>
        hasher.Add(value.Value);
}

internal static class EcsTestWorld
{
    public static EcsWorld Create(int initialCapacity = 16)
    {
        var world = new EcsWorld("tests.ecs.world", 10, initialCapacity);
        world.RegisterComponent<TestPosition>();
        world.RegisterComponent<TestVelocity>();
        world.RegisterComponent<TestValue>();
        world.RegisterComponent<FidelityComponent>();
        return world;
    }

    public static ulong Hash(EcsWorld world)
    {
        var hasher = new StableHasher64();
        world.ContributeToHash(hasher);
        return hasher.Value;
    }
}
