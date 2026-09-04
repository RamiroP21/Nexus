using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// Computes a versioned global hash from deterministic configuration, clock, and state partitions.
/// </summary>
public static class SimulationStateHasher
{
    private const string FormatVersion = "Nexus.Simulation.State.v2";
    private const string SystemScheduleFormatVersion = "Nexus.Simulation.SystemSchedule.v1";
    private const string ContributorSetFormatVersion = "Nexus.Simulation.ContributorSet.v1";
    private const string ContributorFormatVersion = "Nexus.Simulation.Contributor.v1";

    public static SimulationStateHash Compute(
        DeterministicSeed seed,
        int tickRate,
        SimulationTick currentTick,
        RandomStreamProvider randomStreams,
        CommandBuffer commands,
        EventBuffer events,
        IEnumerable<ISimulationSystem> systems,
        IEnumerable<IStateHashContributor> contributors)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickRate);
        ArgumentNullException.ThrowIfNull(randomStreams);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentNullException.ThrowIfNull(contributors);

        SystemRegistration[] orderedSystems = ValidateAndOrderSystems(systems);
        ContributorRegistration[] ordered = ValidateAndOrder(contributors);
        return ComputeValidated(
            seed,
            tickRate,
            currentTick,
            randomStreams,
            commands,
            events,
            orderedSystems,
            ordered);
    }

    internal static SimulationStateHash ComputeValidated(
        DeterministicSeed seed,
        int tickRate,
        SimulationTick currentTick,
        RandomStreamProvider randomStreams,
        CommandBuffer commands,
        EventBuffer events,
        IReadOnlyList<SystemRegistration> orderedSystems,
        IReadOnlyList<ContributorRegistration> orderedContributors)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickRate);
        ArgumentNullException.ThrowIfNull(randomStreams);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(orderedSystems);
        ArgumentNullException.ThrowIfNull(orderedContributors);

        var hasher = new StableHasher64();
        hasher.Add(FormatVersion);
        hasher.Add(seed);
        hasher.Add(tickRate);
        hasher.Add(currentTick);
        ContributeSystemScheduleToHash(hasher, orderedSystems);
        randomStreams.ContributeToHash(hasher);
        commands.ContributeToHash(hasher);
        events.ContributeToHash(hasher);

        ContributeContributorsToHash(hasher, orderedContributors);

        return new SimulationStateHash(hasher.Value);
    }

    internal static SystemRegistration[] ValidateAndOrderSystems(
        IEnumerable<ISimulationSystem> systems)
    {
        var registrations = new List<SystemRegistration>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();

        foreach (ISimulationSystem system in systems)
        {
            if (system is null)
            {
                throw new InvalidOperationException("A simulation system cannot be null.");
            }

            string id = system.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Every simulation system requires a stable ID.");
            }

            if (!ids.Add(id))
            {
                throw new InvalidOperationException($"Duplicate simulation-system ID '{id}'.");
            }

            int order = system.Order;
            if (!orders.Add(order))
            {
                throw new InvalidOperationException(
                    $"Duplicate simulation-system order '{order}'. Orders must be unique.");
            }

            SimulationSystemFrequency frequency = system.Frequency;
            if (frequency.Interval == 0)
            {
                throw new InvalidOperationException(
                    $"Simulation system '{id}' has an invalid zero-tick frequency.");
            }

            registrations.Add(new SystemRegistration(id, order, frequency, system));
        }

        return [.. registrations.OrderBy(static registration => registration.Order)];
    }

    internal static ContributorRegistration[] ValidateAndOrder(
        IEnumerable<IStateHashContributor> contributors)
    {
        var registrations = new List<ContributorRegistration>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();

        foreach (IStateHashContributor contributor in contributors)
        {
            if (contributor is null)
            {
                throw new InvalidOperationException("A state-hash contributor cannot be null.");
            }

            string id = contributor.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Every state-hash contributor requires a stable ID.");
            }

            if (!ids.Add(id))
            {
                throw new InvalidOperationException($"Duplicate state-hash contributor ID '{id}'.");
            }

            int order = contributor.Order;
            if (!orders.Add(order))
            {
                throw new InvalidOperationException(
                    $"Duplicate state-hash contributor order '{order}'. Orders must be unique.");
            }

            registrations.Add(new ContributorRegistration(id, order, contributor));
        }

        return [.. registrations.OrderBy(static registration => registration.Order)];
    }

    private static void ContributeSystemScheduleToHash(
        StableHasher64 hasher,
        IReadOnlyList<SystemRegistration> orderedSystems)
    {
        hasher.Add(SystemScheduleFormatVersion);
        hasher.Add(checked((ulong)orderedSystems.Count));

        for (int index = 0; index < orderedSystems.Count; index++)
        {
            SystemRegistration system = orderedSystems[index];
            hasher.Add(checked((ulong)index));
            hasher.Add(system.Order);
            hasher.Add(system.Id);
            hasher.Add(system.Frequency.Interval);
        }
    }

    private static void ContributeContributorsToHash(
        StableHasher64 hasher,
        IReadOnlyList<ContributorRegistration> orderedContributors)
    {
        hasher.Add(ContributorSetFormatVersion);
        hasher.Add(checked((ulong)orderedContributors.Count));

        for (int index = 0; index < orderedContributors.Count; index++)
        {
            ContributorRegistration contributor = orderedContributors[index];
            var contributorHasher = new StableHasher64();
            contributorHasher.Add(ContributorFormatVersion);
            contributorHasher.Add(contributor.Order);
            contributorHasher.Add(contributor.Id);
            contributor.Instance.ContributeToHash(contributorHasher);

            hasher.Add(checked((ulong)index));
            hasher.Add(contributorHasher.Value);
        }
    }

    internal sealed record SystemRegistration(
        string Id,
        int Order,
        SimulationSystemFrequency Frequency,
        ISimulationSystem Instance);

    internal sealed record ContributorRegistration(
        string Id,
        int Order,
        IStateHashContributor Instance);
}
