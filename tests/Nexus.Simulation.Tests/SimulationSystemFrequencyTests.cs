using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class SimulationSystemFrequencyTests
{
    [Fact]
    public void EveryFactoryRejectsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationSystemFrequency.Every(0));
    }

    [Fact]
    public void DefaultValueIsRejectedWhenUsed()
    {
        Assert.Throws<InvalidOperationException>(
            () => default(SimulationSystemFrequency).IsDue(SimulationTick.Initial));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    public void FrequencyUsesTickZeroAsFirstDueTick(ulong tick, bool expected)
    {
        bool actual = SimulationSystemFrequency.EveryTenTicks.IsDue(new SimulationTick(tick));

        Assert.Equal(expected, actual);
    }
}
