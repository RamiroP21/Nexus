namespace Nexus.Core.Tests;

public sealed class StableHasherReuseTests
{
    [Fact]
    public void ResetMakesReusedHasherEquivalentToAFreshInstance()
    {
        var reused = new StableHasher64();
        reused.Add("discarded");
        reused.Add(123UL);
        reused.Reset();

        var fresh = new StableHasher64();
        Assert.Equal(fresh.Value, reused.Value);
        reused.Add(42);
        fresh.Add(42);
        Assert.Equal(fresh.Value, reused.Value);
    }
}
