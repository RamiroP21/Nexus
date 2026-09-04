using Nexus.Core;
using Nexus.Simulation;

namespace Nexus.Determinism.Tests;

public sealed class DeterminismIntegrationTests
{
    private const ulong DefaultSeed = 123_456_789UL;
    private const int DefaultTickRate = 60;
    private const ulong DefaultTickCount = 10_000UL;

    [Fact]
    public void ReconstructedSimulationProducesSameFinalHash()
    {
        ScenarioResult first = DeterministicScenario.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);
        ScenarioResult second = DeterministicScenario.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);

        Assert.Equal(first.RunResult.StateHashHex, second.RunResult.StateHashHex);
        Assert.Equal(first.RandomAccumulator, second.RandomAccumulator);
        Assert.Equal(DefaultTickCount, first.RunResult.CompletedTicks.Value);
    }

    [Fact]
    public void DifferentSeedChangesDemonstrationState()
    {
        ScenarioResult first = DeterministicScenario.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);
        ScenarioResult second = DeterministicScenario.RunFresh(
            DefaultSeed + 1UL,
            DefaultTickRate,
            DefaultTickCount);

        Assert.NotEqual(first.RandomAccumulator, second.RandomAccumulator);
        Assert.NotEqual(first.RunResult.StateHashHex, second.RunResult.StateHashHex);
    }

    [Fact]
    public void ExtraConsumptionInOneStreamDoesNotChangeAnotherStream()
    {
        var baseline = new RandomStreamProvider(new DeterministicSeed(DefaultSeed));
        var perturbed = new RandomStreamProvider(new DeterministicSeed(DefaultSeed));
        var baselineStreamB = baseline.GetStream("stream-b");
        var perturbedStreamA = perturbed.GetStream("stream-a");
        var perturbedStreamB = perturbed.GetStream("stream-b");

        for (int i = 0; i < 10_000; i++)
        {
            _ = perturbedStreamA.NextUInt64();
        }

        for (int i = 0; i < 128; i++)
        {
            Assert.Equal(baselineStreamB.NextUInt64(), perturbedStreamB.NextUInt64());
        }
    }

    [Fact]
    public void CompleteDefaultScenarioMatchesFrozenIntegrationHash()
    {
        ScenarioResult result = DeterministicScenario.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);

        Assert.Equal("988DF50CBA896652", result.RunResult.StateHashHex);
    }
}
