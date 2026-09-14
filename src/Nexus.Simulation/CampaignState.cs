namespace Nexus.Simulation;

public sealed record WorldMemory(string MemoryId, ulong SimulationTick, string DistrictId, string Kind, string SubjectId, string Value);
public sealed record CampaignCommandReceipt(string CommandId, bool Accepted, string Code);

/// <summary>Closed semantic checkpoint. Contains no filesystem, transport or presentation state.</summary>
public sealed record CampaignState(string CampaignId, ulong Seed, ulong Tick, ulong PhaseEnteredTick,
    DistrictPhase Phase, DistrictCrisisStatus Crisis1, DistrictCrisisStatus Crisis2,
    DistrictInfrastructureState Infrastructure, DistrictRouteState Route, bool Crisis2InheritedDamage,
    ulong Crisis2DeadlineTick, IReadOnlyList<DistrictZoneSnapshot> Zones, ulong MemorySequence,
    IReadOnlyList<WorldMemory> Memories, IReadOnlyList<CampaignCommandReceipt> CommandReceipts)
{
    public const string Format = "Nexus.CampaignSave.v1";
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CampaignId) || PhaseEnteredTick > Tick ||
            !Enum.IsDefined(Phase) || !Enum.IsDefined(Crisis1) || !Enum.IsDefined(Crisis2) ||
            !Enum.IsDefined(Infrastructure) || !Enum.IsDefined(Route) || Zones is null || Memories is null || CommandReceipts is null)
            throw new InvalidDataException("Invalid campaign semantic state.");
        string[] ids = ["district01.zone.a", "district01.zone.b", "district01.zone.c"];
        if (Zones.Any(z => z is null) || Memories.Any(m => m is null) || CommandReceipts.Any(c => c is null) ||
            Memories.Count > 128 || CommandReceipts.Count > 512 ||
            !Zones.Select(z => z.Id).Order(StringComparer.Ordinal).SequenceEqual(ids) ||
            Zones.Any(z => !Enum.IsDefined(z.CivilianState)) || Memories.Count != (long)MemorySequence ||
            Memories.Where((m, i) => m.MemoryId != $"district01.memory.{i + 1:D8}" || m.SimulationTick > Tick || m.DistrictId != "district01").Any() ||
            CommandReceipts.Any(c => string.IsNullOrWhiteSpace(c.CommandId) || c.CommandId.Length > 128 || !c.Accepted || c.Code != "accepted") ||
            CommandReceipts.Select(c => c.CommandId).Distinct(StringComparer.Ordinal).Count() != CommandReceipts.Count)
            throw new InvalidDataException("Invalid campaign stable IDs or memory sequence.");
    }
}
