namespace Nexus.Contracts;

/// <summary>
/// Marks immutable information published by a completed simulation tick.
/// </summary>
public interface ISimulationEvent : IStateHashable;
