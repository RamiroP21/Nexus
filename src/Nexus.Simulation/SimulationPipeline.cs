using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// Runs an immutable, explicitly ordered set of systems on one sequential thread.
/// </summary>
public sealed class SimulationPipeline
{
    private readonly List<ISimulationSystem> _systemRegistrations = [];
    private readonly List<IStateHashContributor> _contributorRegistrations = [];
    private readonly SimulationClock _clock;
    private readonly RandomStreamProvider _randomStreams;
    private readonly CommandBuffer _commands;
    private readonly EventBuffer _events;
    private readonly SimulationContext _externalContext;
    private SimulationStateHasher.SystemRegistration[] _systems = [];
    private SimulationContext[] _systemContexts = [];
    private SimulationStateHasher.ContributorRegistration[] _contributors = [];
    private ulong[] _executionCounts = [];
    private bool _isRunning;

    public SimulationPipeline(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
        _clock = new SimulationClock(options.TickRate);
        _randomStreams = new RandomStreamProvider(options.Seed);
        _commands = new CommandBuffer();
        _events = new EventBuffer();
        _externalContext = new SimulationContext(_clock, _randomStreams, _commands, _events);
    }

    public SimulationOptions Options { get; }

    public SimulationTick CurrentTick => _clock.CurrentTick;

    public IReadOnlyList<ISimulationEvent> PublishedEvents => _events.PublishedEvents;

    public bool IsTickOpen => _events.IsTickOpen;

    public bool IsFrozen { get; private set; }

    public bool IsFaulted { get; private set; }

    public SimulationPipeline RegisterSystem(ISimulationSystem system)
    {
        EnsureRegistrationOpen();
        ArgumentNullException.ThrowIfNull(system);
        _systemRegistrations.Add(system);
        return this;
    }

    public SimulationPipeline RegisterStateContributor(IStateHashContributor contributor)
    {
        EnsureRegistrationOpen();
        ArgumentNullException.ThrowIfNull(contributor);
        _contributorRegistrations.Add(contributor);
        return this;
    }

    public void EnqueueCommand(ISimulationCommand command)
    {
        EnsureUsable();
        _commands.Enqueue(command);
    }

    public void Freeze()
    {
        if (IsFrozen)
        {
            return;
        }

        if (_isRunning)
        {
            throw new InvalidOperationException("The pipeline cannot be frozen while it is running.");
        }

        _systems = SimulationStateHasher.ValidateAndOrderSystems(_systemRegistrations);
        _systemContexts =
        [
            .. _systems.Select(system => new SimulationContext(
                _clock,
                _randomStreams,
                _commands,
                _events,
                system.Id)),
        ];
        _contributors = SimulationStateHasher.ValidateAndOrder(_contributorRegistrations);
        _executionCounts = new ulong[_systems.Length];
        IsFrozen = true;
    }

    public SimulationRunResult Run()
    {
        if (Options.MaxTicks is not ulong maximumTicks)
        {
            throw new InvalidOperationException(
                "Run() requires SimulationOptions.MaxTicks. Use Run(tickCount) otherwise.");
        }

        if (_clock.CurrentTick.Value > maximumTicks)
        {
            throw new InvalidOperationException("The clock has advanced beyond the configured maximum.");
        }

        return Run(maximumTicks - _clock.CurrentTick.Value);
    }

    public SimulationRunResult Run(ulong tickCount)
    {
        EnsureUsable();
        Freeze();

        if (_isRunning)
        {
            throw new InvalidOperationException("The pipeline cannot run recursively.");
        }

        ulong finalTick;
        try
        {
            finalTick = checked(_clock.CurrentTick.Value + tickCount);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "The requested tick count would overflow the simulation clock.");
        }

        if (Options.MaxTicks is ulong maximumTicks && finalTick > maximumTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "The requested run exceeds SimulationOptions.MaxTicks.");
        }

        _isRunning = true;
        try
        {
            for (ulong i = 0; i < tickCount; i++)
            {
                ExecuteTick();
            }
        }
        finally
        {
            _isRunning = false;
        }

        return CreateResult();
    }

    public SimulationStateHash ComputeStateHash()
    {
        EnsureUsable();
        if (_isRunning || _events.IsTickOpen)
        {
            throw new InvalidOperationException(
                "State hashes can only be computed at a completed tick boundary.");
        }

        Freeze();
        return SimulationStateHasher.ComputeValidated(
            Options.Seed,
            Options.TickRate,
            _clock.CurrentTick,
            _randomStreams,
            _commands,
            _events,
            _systems,
            _contributors);
    }

    public IReadOnlyList<SimulationSystemMetrics> GetSystemMetrics()
    {
        Freeze();
        var metrics = new SimulationSystemMetrics[_systems.Length];
        for (int i = 0; i < _systems.Length; i++)
        {
            SimulationStateHasher.SystemRegistration system = _systems[i];
            metrics[i] = new SimulationSystemMetrics(
                system.Id,
                system.Order,
                system.Frequency,
                _executionCounts[i]);
        }

        return Array.AsReadOnly(metrics);
    }

    private void ExecuteTick()
    {
        _events.BeginTick();
        try
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                SimulationStateHasher.SystemRegistration system = _systems[i];
                if (!system.Frequency.IsDue(_clock.CurrentTick))
                {
                    continue;
                }

                system.Instance.Execute(_systemContexts[i]);
                _executionCounts[i] = checked(_executionCounts[i] + 1);
            }

            _commands.ProcessPending(_externalContext);
            _events.CompleteTick();
            _clock.Advance();
        }
        catch
        {
            if (_events.IsTickOpen)
            {
                _events.AbortTick();
            }

            IsFaulted = true;
            throw;
        }
    }

    private SimulationRunResult CreateResult()
    {
        SimulationStateHash hash = ComputeStateHash();
        return new SimulationRunResult(
            Options.Seed,
            Options.TickRate,
            _clock.CurrentTick,
            hash,
            GetSystemMetrics());
    }

    private void EnsureRegistrationOpen()
    {
        EnsureUsable();
        if (IsFrozen)
        {
            throw new InvalidOperationException(
                "The pipeline is frozen; systems and contributors can no longer be registered.");
        }
    }

    private void EnsureUsable()
    {
        if (IsFaulted)
        {
            throw new InvalidOperationException(
                "The pipeline faulted during a tick and cannot be resumed safely.");
        }
    }

}
