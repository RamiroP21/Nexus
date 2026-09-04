using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class SimulationOptionsTests
{
    [Fact]
    public void ConstructorPreservesValidConfiguration()
    {
        var seed = new DeterministicSeed(42);

        var options = new SimulationOptions(seed, 60, 10_000);

        Assert.Equal(seed, options.Seed);
        Assert.Equal(60, options.TickRate);
        Assert.Equal(10_000UL, options.MaxTicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsInvalidTickRate(int tickRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationOptions(new DeterministicSeed(1), tickRate));
    }

    [Fact]
    public void ZeroMaximumTicksRepresentsAValidNoOpRun()
    {
        var pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(1), 60, 0));

        SimulationRunResult result = pipeline.Run();

        Assert.Equal(SimulationTick.Initial, result.CompletedTicks);
    }
}
