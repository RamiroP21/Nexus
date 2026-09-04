namespace Nexus.Simulation.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void ClockStartsAtTickZero()
    {
        var clock = new SimulationClock(60);

        Assert.Equal(0UL, clock.CurrentTick.Value);
        Assert.Equal(0d, clock.LogicalTimeSeconds);
        Assert.Equal(1d / 60d, clock.FixedDeltaTimeSeconds);
    }

    [Fact]
    public void AdvanceChangesOnlyTheIntegerTick()
    {
        var clock = new SimulationClock(60);

        for (int i = 0; i < 120; i++)
        {
            clock.Advance();
        }

        Assert.Equal(120UL, clock.CurrentTick.Value);
        Assert.Equal(2d, clock.LogicalTimeSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void ConstructorRejectsInvalidTickRate(int tickRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(tickRate));
    }
}
