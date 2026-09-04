using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class SimulationStateHasherTests
{
    [Fact]
    public void ContributorRegistrationOrderDoesNotChangeHash()
    {
        var first = new ValueContributor("first", 10, 100);
        var second = new ValueContributor("second", 20, 200);

        SimulationStateHash forward = Compute([first, second]);
        SimulationStateHash reverse = Compute([second, first]);

        Assert.Equal(forward, reverse);
    }

    [Fact]
    public void DuplicateContributorOrderIsRejected()
    {
        var contributors = new IStateHashContributor[]
        {
            new ValueContributor("first", 10, 1),
            new ValueContributor("second", 10, 2),
        };

        Assert.Throws<InvalidOperationException>(() => Compute(contributors));
    }

    [Fact]
    public void DuplicateContributorIdIsRejected()
    {
        var contributors = new IStateHashContributor[]
        {
            new ValueContributor("same", 10, 1),
            new ValueContributor("same", 20, 2),
        };

        Assert.Throws<InvalidOperationException>(() => Compute(contributors));
    }

    [Fact]
    public void StateValueSeedTickAndRateAllAffectHash()
    {
        SimulationStateHash baseline = Compute([new ValueContributor("state", 10, 1)]);
        SimulationStateHash changedState = Compute([new ValueContributor("state", 10, 2)]);
        SimulationStateHash changedSeed = Compute(
            [new ValueContributor("state", 10, 1)],
            seed: 6);
        SimulationStateHash changedTick = Compute(
            [new ValueContributor("state", 10, 1)],
            tick: 4);
        SimulationStateHash changedRate = Compute(
            [new ValueContributor("state", 10, 1)],
            tickRate: 30);

        Assert.NotEqual(baseline, changedState);
        Assert.NotEqual(baseline, changedSeed);
        Assert.NotEqual(baseline, changedTick);
        Assert.NotEqual(baseline, changedRate);
    }

    [Fact]
    public void HexIsCanonicalUppercaseAndFixedWidth()
    {
        SimulationStateHash hash = Compute([]);

        Assert.Equal(16, hash.Hex.Length);
        Assert.Matches("^[0-9A-F]{16}$", hash.Hex);
    }

    [Fact]
    public void ContributorPayloadCannotImitateAnotherContributorRegistration()
    {
        var embeddedRegistration = new DelegateContributor(
            "first",
            10,
            static hasher =>
            {
                hasher.Add(20);
                hasher.Add("second");
            });
        var first = new DelegateContributor("first", 10, static _ => { });
        var second = new DelegateContributor("second", 20, static _ => { });

        Assert.NotEqual(Compute([embeddedRegistration]), Compute([first, second]));
    }

    private static SimulationStateHash Compute(
        IEnumerable<IStateHashContributor> contributors,
        ulong seed = 5,
        int tickRate = 60,
        ulong tick = 3) =>
        SimulationStateHasher.Compute(
            new DeterministicSeed(seed),
            tickRate,
            new SimulationTick(tick),
            new RandomStreamProvider(new DeterministicSeed(seed)),
            new CommandBuffer(),
            new EventBuffer(),
            [],
            contributors);

    private sealed class ValueContributor(
        string id,
        int order,
        ulong value) : IStateHashContributor
    {
        public string Id => id;

        public int Order => order;

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(value);
    }

    private sealed class DelegateContributor(
        string id,
        int order,
        Action<StableHasher64> contribute) : IStateHashContributor
    {
        public string Id => id;

        public int Order => order;

        public void ContributeToHash(StableHasher64 hasher) => contribute(hasher);
    }
}
