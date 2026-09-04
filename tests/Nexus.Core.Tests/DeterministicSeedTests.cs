namespace Nexus.Core.Tests;

public sealed class DeterministicSeedTests
{
    [Fact]
    public void SeedsAreValueBasedAndZeroIsValid()
    {
        Assert.Equal(new DeterministicSeed(0), default);
        Assert.Equal(new DeterministicSeed(123), new DeterministicSeed(123));
        Assert.NotEqual(new DeterministicSeed(123), new DeterministicSeed(124));
        Assert.Equal("123", new DeterministicSeed(123).ToString());
    }

    [Fact]
    public void InstanceDeriveDelegatesToCanonicalDerivation()
    {
        var root = new DeterministicSeed(123_456_789);

        Assert.Equal(
            DeterministicSeedDerivation.Derive(root, "Weather"),
            root.Derive("Weather"));
    }
}
