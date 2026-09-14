using System.Text.Json;
using Nexus.Host;

namespace Nexus.Simulation.Tests;

public sealed class CampaignPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "NexusCampaignTests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static DistrictAuthoritySession NewSession()
    {
        var session = new DistrictAuthoritySession(777, "session-a");
        session.SetCampaignIdentity("campaign-test");
        return session;
    }
    private static DistrictCommandResult Command(DistrictAuthoritySession session, string id, DistrictCommandType type) =>
        session.Submit(new(DistrictAuthorityProtocol.Version, session.SessionId, "test", 1, id, type));

    [Fact]
    public void SaveLoadAndContinuationMatchUninterruptedActiveCrisis()
    {
        var session = NewSession();
        Command(session, "start", DistrictCommandType.StartCrisis1);
        session.Advance(17);
        var repository = new CampaignSaveRepository(_directory);
        CampaignSaveEnvelope saved = repository.Save(session, 1);
        CampaignLoadResult loaded = repository.Load()!;
        var resumed = DistrictAuthoritySession.RestoreCampaign(loaded.Envelope.State, "session-b");
        Assert.Equal(saved.StateHash, resumed.ComputeStateHash());
        Assert.Equal(session.CampaignId, resumed.CampaignId);
        Assert.NotEqual(session.SessionId, resumed.SessionId);
        session.Advance(24); resumed.Advance(24);
        Command(session, "fail", DistrictCommandType.FailCrisis1);
        Command(resumed, "fail", DistrictCommandType.FailCrisis1);
        session.Advance(60); resumed.Advance(60);
        Assert.Equal(session.Tick, resumed.Tick);
        Assert.Equal(session.ComputeStateHash(), resumed.ComputeStateHash());
        Assert.True(resumed.Crisis2InheritedDamage);
        Assert.Equal(session.Memories, resumed.Memories);
    }

    [Fact]
    public void DurableReceiptPreventsDuplicateMemoryAfterRestart()
    {
        var session = NewSession();
        Command(session, "start", DistrictCommandType.StartCrisis1);
        var resumed = DistrictAuthoritySession.RestoreCampaign(session.ExportCampaign(), "session-b");
        string before = resumed.ComputeStateHash();
        Assert.True(Command(resumed, "start", DistrictCommandType.StartCrisis1).Duplicate);
        Assert.Single(resumed.Memories);
        Assert.Equal(before, resumed.ComputeStateHash());
    }

    [Fact]
    public void HistorySurvivesCurrentStateChanging()
    {
        var session = NewSession();
        Command(session, "start", DistrictCommandType.StartCrisis1);
        Command(session, "fail", DistrictCommandType.FailCrisis1);
        session.Advance(60);
        Command(session, "resolve", DistrictCommandType.ResolveCrisis2);
        Assert.Equal(DistrictRouteState.Open, session.Route);
        Assert.Contains(session.Memories, m => m.Kind == "RouteOutcome" && m.Value == "Blocked");
        Assert.Contains(session.Memories, m => m.Kind == "RouteOutcome" && m.Value == "Open");
    }

    [Fact]
    public void CorruptPrimaryRecoversBackupAndRepairsPrimary()
    {
        var repository = new CampaignSaveRepository(_directory);
        var session = NewSession();
        string baseline = repository.Save(session, 1).StateHash;
        Command(session, "start", DistrictCommandType.StartCrisis1);
        repository.Save(session, 2);
        File.WriteAllText(repository.PrimaryPath, "corrupt");
        var recovered = repository.Load()!;
        Assert.True(recovered.Recovered);
        Assert.Equal(baseline, recovered.Envelope.StateHash);
        Assert.False(repository.Load()!.Recovered);
    }

    [Fact]
    public void BothInvalidFailClosedAndUnknownVersionRejected()
    {
        var repository = new CampaignSaveRepository(_directory);
        var saved = repository.Save(NewSession(), 1);
        File.WriteAllText(repository.PrimaryPath, JsonSerializer.Serialize(saved with { FormatVersion = "Nexus.CampaignSave.v99" }, CampaignSaveRepository.JsonOptions));
        Assert.Throws<InvalidDataException>(() => repository.Load());
        File.WriteAllText(repository.BackupPath, "broken");
        Assert.Throws<InvalidDataException>(() => repository.Load());
    }

    [Fact]
    public void CorruptionAndHashMismatchAreRejected()
    {
        var repository = new CampaignSaveRepository(_directory);
        var saved = repository.Save(NewSession(), 1);
        File.WriteAllText(repository.PrimaryPath, JsonSerializer.Serialize(saved with { StateHash = "bad" }, CampaignSaveRepository.JsonOptions));
        Assert.Throws<InvalidDataException>(() => repository.Load());
        File.WriteAllText(repository.PrimaryPath, JsonSerializer.Serialize(saved with { State = saved.State with { Tick = 10 } }, CampaignSaveRepository.JsonOptions));
        Assert.Throws<InvalidDataException>(() => repository.Load());
    }

    [Fact]
    public void SaveRevisionAndSessionDoNotAffectSemanticHash()
    {
        var repository = new CampaignSaveRepository(_directory);
        var session = NewSession();
        Assert.Equal(repository.Save(session, 1).StateHash, repository.Save(session, 2).StateHash);
        Assert.Equal(session.ComputeStateHash(), DistrictAuthoritySession.RestoreCampaign(session.ExportCampaign(), "different-session").ComputeStateHash());
    }

    [Fact]
    public void TemporaryFileIsNotPromotedOnLoad()
    {
        var repository = new CampaignSaveRepository(_directory);
        var saved = repository.Save(NewSession(), 1);
        File.WriteAllText(repository.PrimaryPath + ".tmp", "interrupted write");
        Assert.Equal(saved.StateHash, repository.Load()!.Envelope.StateHash);
    }

    [Fact]
    public void LoadedCampaignReplayUsesCheckpointAndAcceptedContinuationStream()
    {
        var initial = NewSession();
        Command(initial, "start", DistrictCommandType.StartCrisis1);
        initial.Advance(13);
        var resumed = DistrictAuthoritySession.RestoreCampaign(initial.ExportCampaign(), "session-b");
        resumed.Advance(29);
        Command(resumed, "fail", DistrictCommandType.FailCrisis1);
        resumed.Advance(61);
        Assert.Equal(resumed.ComputeStateHash(), DistrictReplay.Run(resumed.CreateReplayDocument()).ComputeStateHash());
    }
}
