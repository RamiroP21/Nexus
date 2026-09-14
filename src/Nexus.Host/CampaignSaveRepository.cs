using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexus.Simulation;

namespace Nexus.Host;

public sealed record CampaignSaveEnvelope(string FormatVersion, ulong SaveRevision, CampaignState State, string StateHash, string PayloadChecksum);
public sealed record CampaignLoadResult(CampaignSaveEnvelope Envelope, bool Recovered);

/// <summary>One bounded primary/backup pair; persistence metadata never enters semantic hashing.</summary>
public sealed class CampaignSaveRepository
{
    public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nexus", "Saves", "development");
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public string PrimaryPath { get; }
    public string BackupPath => PrimaryPath + ".bak";
    public CampaignSaveRepository(string directory) => PrimaryPath = Path.Combine(Path.GetFullPath(directory), "campaign.json");

    public CampaignLoadResult? Load()
    {
        if (!File.Exists(PrimaryPath) && !File.Exists(BackupPath)) return null;
        try { return new(Read(PrimaryPath), false); }
        catch (Exception ex) when (IsSaveFailure(ex))
        {
            try
            {
                CampaignSaveEnvelope backup = Read(BackupPath);
                // Repair the primary without replacing the known-good backup with corrupt bytes.
                WriteTemporaryAndPromote(backup, false);
                return new(backup, true);
            }
            catch (Exception backupError) when (IsSaveFailure(backupError))
            { throw new InvalidDataException("Campaign primary and backup are invalid; refusing to create a new campaign.", new AggregateException(ex, backupError)); }
        }
    }

    public CampaignSaveEnvelope Save(DistrictAuthoritySession session, ulong revision)
    {
        CampaignState state = session.ExportCampaign();
        state.Validate();
        var envelope = new CampaignSaveEnvelope(CampaignState.Format, revision, state, session.ComputeStateHash(), Checksum(state));
        Validate(envelope);
        WriteTemporaryAndPromote(envelope, true);
        return envelope;
    }

    private void WriteTemporaryAndPromote(CampaignSaveEnvelope envelope, bool keepPrimaryAsBackup)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrimaryPath)!);
        string temporary = PrimaryPath + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(true); }
        _ = Read(temporary);
        if (File.Exists(PrimaryPath)) File.Replace(temporary, PrimaryPath, keepPrimaryAsBackup ? BackupPath : null);
        else File.Move(temporary, PrimaryPath);
    }

    private static CampaignSaveEnvelope Read(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length > 1_048_576) throw new InvalidDataException("Campaign save missing or oversized.");
        CampaignSaveEnvelope envelope = JsonSerializer.Deserialize<CampaignSaveEnvelope>(File.ReadAllBytes(path), JsonOptions)
            ?? throw new InvalidDataException("Campaign save is empty.");
        Validate(envelope);
        return envelope;
    }

    private static void Validate(CampaignSaveEnvelope envelope)
    {
        if (envelope.FormatVersion != CampaignState.Format) throw new InvalidDataException("Unsupported campaign save format.");
        if (envelope.State is null || envelope.SaveRevision == 0) throw new InvalidDataException("Invalid campaign envelope.");
        envelope.State.Validate();
        if (Checksum(envelope.State) != envelope.PayloadChecksum) throw new InvalidDataException("Campaign payload checksum mismatch.");
        if (DistrictAuthoritySession.RestoreCampaign(envelope.State, "validation").ComputeStateHash() != envelope.StateHash)
            throw new InvalidDataException("Campaign semantic hash mismatch.");
    }

    private static string Checksum(CampaignState state) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions)));
    private static bool IsSaveFailure(Exception ex) => ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException;
}
