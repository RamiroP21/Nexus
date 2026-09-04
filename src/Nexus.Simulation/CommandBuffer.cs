using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// Stores commands in FIFO order until the deterministic command phase.
/// </summary>
public sealed class CommandBuffer
{
    private const string HashFormatVersion = "Nexus.Simulation.CommandBuffer.v1";
    private const string CommandHashFormatVersion = "Nexus.Simulation.Command.v1";

    private readonly Queue<BufferedCommand> _commands = new();
    private bool _isProcessing;

    public int Count => _commands.Count;

    public void Enqueue(ISimulationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Enqueue(new BufferedCommand(
            command,
            StateHashablePayload.CaptureStableTypeId(command),
            null,
            true,
            string.Empty));
    }

    internal void Enqueue(ISimulationCommand command, SimulationContext ownerContext)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(ownerContext);
        _commands.Enqueue(new BufferedCommand(
            command,
            StateHashablePayload.CaptureStableTypeId(command),
            ownerContext,
            ownerContext.UsesGlobalRandomScope,
            ownerContext.RandomStreamOwnerId));
    }

    /// <summary>
    /// Executes the commands that existed when this phase began. Commands enqueued by a
    /// command are deliberately deferred until the following tick.
    /// </summary>
    public int ProcessPending(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_isProcessing)
        {
            throw new InvalidOperationException("The command buffer is already being processed.");
        }

        _isProcessing = true;
        try
        {
            int commandsAtPhaseStart = _commands.Count;
            for (int i = 0; i < commandsAtPhaseStart; i++)
            {
                BufferedCommand buffered = _commands.Dequeue();
                buffered.Command.Execute(buffered.OwnerContext ?? context);
            }

            return commandsAtPhaseStart;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    internal void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        if (_isProcessing)
        {
            throw new InvalidOperationException(
                "A command buffer can only be hashed at a completed phase boundary.");
        }

        hasher.Add(HashFormatVersion);
        hasher.Add(checked((ulong)_commands.Count));

        ulong index = 0;
        foreach (BufferedCommand buffered in _commands)
        {
            hasher.Add(index);
            hasher.Add(buffered.UsesGlobalRandomScope);
            hasher.Add(buffered.RandomStreamOwnerId);
            hasher.Add(StateHashablePayload.ComputeHash(
                CommandHashFormatVersion,
                buffered.StableTypeId,
                buffered.Command));
            index++;
        }
    }

    private sealed record BufferedCommand(
        ISimulationCommand Command,
        string StableTypeId,
        ISimulationContext? OwnerContext,
        bool UsesGlobalRandomScope,
        string RandomStreamOwnerId);
}
