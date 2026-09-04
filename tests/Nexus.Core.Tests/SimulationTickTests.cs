using System.Globalization;

namespace Nexus.Core.Tests;

public sealed class SimulationTickTests
{
    [Fact]
    public void InitialIsZero()
    {
        Assert.Equal(0UL, SimulationTick.Initial.Value);
        Assert.Equal(default, SimulationTick.Initial);
    }

    [Fact]
    public void NextAndIncrementAdvanceExactlyOneTick()
    {
        var tick = new SimulationTick(41);

        Assert.Equal(new SimulationTick(42), tick.Next());

        tick++;
        Assert.Equal(new SimulationTick(42), tick);
    }

    [Fact]
    public void AdditionAdvancesByRequestedNumberOfTicks()
    {
        var tick = new SimulationTick(10);

        Assert.Equal(new SimulationTick(35), tick + 25UL);
    }

    [Fact]
    public void IncrementAndAdditionRejectOverflow()
    {
        var finalTick = new SimulationTick(ulong.MaxValue);

        Assert.Throws<OverflowException>(() => finalTick.Next());
        Assert.Throws<OverflowException>(() => finalTick + 1UL);
    }

    [Fact]
    public void ComparisonEqualityAndOrderingAreValueBased()
    {
        var earlier = new SimulationTick(9);
        var same = new SimulationTick(9);
        var later = new SimulationTick(10);

        Assert.Equal(earlier, same);
        Assert.NotEqual(earlier, later);
        Assert.True(earlier < later);
        Assert.True(earlier <= same);
        Assert.True(later > earlier);
        Assert.True(later >= same);
        Assert.Equal(0, earlier.CompareTo(same));
        Assert.True(earlier.CompareTo(later) < 0);
    }

    [Fact]
    public void ToStringIsInvariantAndReadable()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            Assert.Equal("1234567890", new SimulationTick(1_234_567_890).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
