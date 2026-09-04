namespace Nexus.Contracts;

/// <summary>
/// A deterministic mutation deferred to the command phase of a tick.
/// </summary>
public interface ISimulationCommand : IStateHashable
{
    void Execute(ISimulationContext context);
}
