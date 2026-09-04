using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class RandomStreamProviderTests
{
    [Fact]
    public void RepeatedKeyReturnsTheSameStatefulSource()
    {
        var provider = new RandomStreamProvider(new DeterministicSeed(1));

        ISimulationRandomSource first = provider.GetStream("weather");
        ISimulationRandomSource second = provider.GetStream("weather");

        Assert.Same(first, second);
    }

    [Fact]
    public void ExtraDrawsFromOneKeyDoNotAffectAnotherKey()
    {
        var baseline = new RandomStreamProvider(new DeterministicSeed(99));
        uint expected = baseline.GetStream("weather").NextUInt32();
        var subject = new RandomStreamProvider(new DeterministicSeed(99));

        for (int i = 0; i < 10_000; i++)
        {
            _ = subject.GetStream("economy").NextUInt32();
        }

        Assert.Equal(expected, subject.GetStream("weather").NextUInt32());
    }

    [Fact]
    public void SystemsWithTheSameLocalKeyReceiveIndependentNamespacedStreams()
    {
        IReadOnlyList<uint> baseline = RunCapturingSystem(includeNoisySystem: false);
        IReadOnlyList<uint> withNoise = RunCapturingSystem(includeNoisySystem: true);

        Assert.Equal(baseline, withNoise);
    }

    [Fact]
    public void RandomStateChangesTheGlobalHashEvenWhenDrawsAreDiscarded()
    {
        var untouched = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(12), 60, 1));
        var consumed = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(12), 60, 1));
        consumed.RegisterSystem(new DiscardingSystem());

        SimulationRunResult untouchedResult = untouched.Run();
        SimulationRunResult consumedResult = consumed.Run();

        Assert.NotEqual(untouchedResult.StateHash, consumedResult.StateHash);
    }

    [Fact]
    public void StreamCreationOrderDoesNotChangeTheGlobalHash()
    {
        var seed = new DeterministicSeed(321);
        var forward = new RandomStreamProvider(seed);
        var reverse = new RandomStreamProvider(seed);

        _ = forward.GetStream("alpha").NextUInt32();
        _ = forward.GetStream("beta").NextUInt64();
        _ = reverse.GetStream("beta").NextUInt64();
        _ = reverse.GetStream("alpha").NextUInt32();

        SimulationStateHash forwardHash = SimulationStateHasher.Compute(
            seed,
            60,
            SimulationTick.Initial,
            forward,
            new CommandBuffer(),
            new EventBuffer(),
            [],
            []);
        SimulationStateHash reverseHash = SimulationStateHasher.Compute(
            seed,
            60,
            SimulationTick.Initial,
            reverse,
            new CommandBuffer(),
            new EventBuffer(),
            [],
            []);

        Assert.Equal(forwardHash, reverseHash);
    }

    [Fact]
    public void GlobalAndSystemScopesWithTheSameNamesCannotShareAStream()
    {
        var seed = new DeterministicSeed(654);
        var globalProvider = new RandomStreamProvider(seed);
        uint[] globalValues = Enumerable.Range(0, 8)
            .Select(_ => globalProvider.GetStream("main").NextUInt32())
            .ToArray();
        var systemValues = new List<uint>();
        var pipeline = new SimulationPipeline(new SimulationOptions(seed, 60, 8));
        pipeline.RegisterSystem(new CapturingSystem(systemValues));

        pipeline.Run();

        Assert.False(globalValues.SequenceEqual(systemValues));
    }

    private static List<uint> RunCapturingSystem(bool includeNoisySystem)
    {
        var values = new List<uint>();
        var pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(777), 60, 8));
        if (includeNoisySystem)
        {
            pipeline.RegisterSystem(new NoisySystem());
        }

        pipeline.RegisterSystem(new CapturingSystem(values));
        pipeline.Run();
        return values;
    }

    private sealed class NoisySystem : ISimulationSystem
    {
        public string Id => "noise";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            ISimulationRandomSource random = context.GetRandomStream("main");
            for (int i = 0; i < 100; i++)
            {
                _ = random.NextUInt32();
            }
        }
    }

    private sealed class CapturingSystem(ICollection<uint> values) : ISimulationSystem
    {
        public string Id => "capture";

        public int Order => 20;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) =>
            values.Add(context.GetRandomStream("main").NextUInt32());
    }

    private sealed class DiscardingSystem : ISimulationSystem
    {
        public string Id => "discard";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) =>
            _ = context.GetRandomStream("main").NextUInt32();
    }
}
