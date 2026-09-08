namespace Nexus.ECS;

/// <summary>Only adjacent promotions or demotions are valid; no-op requests are errors.</summary>
public static class FidelityTransitions
{
    public static bool IsValid(SimulationFidelity from, SimulationFidelity to)
    {
        ValidateTier(from);
        ValidateTier(to);
        return Math.Abs((int)to - (int)from) == 1;
    }

    internal static void ValidateTier(SimulationFidelity tier)
    {
        if (tier > SimulationFidelity.Full)
        {
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown simulation fidelity.");
        }
    }
}
