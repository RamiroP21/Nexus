using System.Net;
using System.Net.Sockets;
using Nexus.Host;
using Nexus.Simulation;

namespace Nexus.Simulation.Tests;

public sealed class DistrictAuthorityTests
{
    [Fact]
    public void NewSessionHasStableSemanticBaseline()
    {
        var session = new DistrictAuthoritySession(42, "test");
        DistrictSnapshot snapshot = session.Snapshot();
        Assert.Equal("test", snapshot.SessionId);
        Assert.Equal(DistrictPhase.Calm, snapshot.Phase);
        Assert.Equal(3, snapshot.Zones.Count);
        Assert.Equal(session.ComputeStateHash(), snapshot.StateHash);
    }

    [Fact]
    public void LegalFlowAndDownstreamDamageAreTickOwned()
    {
        var session = new DistrictAuthoritySession(9, "test");
        Assert.True(session.Submit(Command("start", DistrictCommandType.StartCrisis1)).Accepted);
        session.Advance(DistrictAuthoritySession.Crisis1WarningTicks);
        Assert.Equal(DistrictPhase.Incident, session.Phase);
        Assert.True(session.Submit(Command("fail", DistrictCommandType.FailCrisis1)).Accepted);
        Assert.Equal(DistrictInfrastructureState.Damaged, session.Infrastructure);
        Assert.Equal(DistrictRouteState.Blocked, session.Route);
        session.Advance(DistrictAuthoritySession.AftermathTicks);
        Assert.Equal(DistrictCrisisStatus.Active, session.Crisis2);
        Assert.True(session.Crisis2InheritedDamage);
        Assert.Equal(DistrictAuthoritySession.Crisis2DamagedDeadlineTicks, session.Crisis2DeadlineTick - session.Tick);
    }

    [Fact]
    public void DuplicateCommandIsIdempotent()
    {
        var session = new DistrictAuthoritySession(9, "test");
        DistrictCommand command = Command("same", DistrictCommandType.StartCrisis1);
        DistrictCommandResult first = session.Submit(command);
        DistrictCommandResult second = session.Submit(command);
        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.True(second.Duplicate);
        Assert.Single(session.AcceptedCommands);
        Assert.Single(session.Events);
    }

    [Fact]
    public void InvalidAndUnknownCommandsFailClosed()
    {
        var session = new DistrictAuthoritySession(9, "test");
        Assert.Equal("illegal_transition", session.Submit(Command("bad", DistrictCommandType.ResolveCrisis1)).Code);
        DistrictCommand unknown = Command("unknown", DistrictCommandType.ObserveZone) with { EntityId = "unknown" };
        Assert.Equal("unknown_entity", session.Submit(unknown).Code);
        Assert.Equal(0UL, session.Tick);
        Assert.Equal(DistrictPhase.Calm, session.Phase);
    }

    [Fact]
    public void ReplayReproducesFinalHash()
    {
        var session = new DistrictAuthoritySession(12, "test");
        DistrictCommand start = Command("start", DistrictCommandType.StartCrisis1) with { TargetTick = 0 };
        DistrictCommand fail = Command("fail", DistrictCommandType.FailCrisis1) with { TargetTick = DistrictAuthoritySession.Crisis1WarningTicks };
        Assert.True(session.Submit(start).Accepted);
        session.Advance(DistrictAuthoritySession.Crisis1WarningTicks);
        Assert.True(session.Submit(fail).Accepted);
        session.Advance(DistrictAuthoritySession.AftermathTicks);
        var replay = new DistrictReplayDocument(DistrictAuthorityProtocol.Version, 12, [start, fail], session.Tick);
        DistrictAuthoritySession replayed = DistrictReplay.Run(replay);
        Assert.Equal(session.Tick, replayed.Tick);
        Assert.Equal(session.ComputeStateHash(), replayed.ComputeStateHash());
        Assert.Equal(replayed.ComputeStateHash(), DistrictReplay.Run(replay).ComputeStateHash());
    }

    [Fact]
    public void FramedProtocolRoundTripsAndRejectsOversizedFrame()
    {
        DistrictWireMessage message = new() { MessageType = DistrictMessageTypes.ClientHello, ClientId = "client" };
        DistrictWireMessage decoded = DistrictProtocolCodec.Deserialize(DistrictProtocolCodec.Serialize(message));
        Assert.Equal(message.MessageType, decoded.MessageType);
        Assert.Throws<InvalidDataException>(() => DistrictProtocolCodec.Deserialize(new byte[] { 0x7B }));
    }

    [Fact]
    public void UnityJsonUtilityEmptyOptionalObjectsAreIgnored()
    {
        const string payload = "{\"protocolVersion\":\"Nexus.DistrictAuthority.v1\",\"messageType\":\"CreateSession\",\"sessionId\":\"\",\"clientId\":\"\",\"clientSequence\":0,\"commandId\":\"\",\"commandType\":\"\",\"entityId\":\"\",\"targetTick\":0,\"seed\":123,\"event\":{\"type\":\"\"},\"snapshot\":{\"sessionId\":\"\",\"zones\":[]},\"errorCode\":\"\",\"errorMessage\":\"\"}";
        DistrictWireMessage decoded = DistrictProtocolCodec.Deserialize(System.Text.Encoding.UTF8.GetBytes(payload));
        Assert.Equal(DistrictMessageTypes.CreateSession, decoded.MessageType);
        Assert.Null(decoded.Event);
        Assert.Null(decoded.Snapshot);
        Assert.Null(decoded.CommandType);
    }

    [Fact]
    public async Task LoopbackHostHandshakeAndSessionFlow()
    {
        await using var host = new DistrictAuthorityHost();
        using var stop = new CancellationTokenSource();
        Task running = host.RunAsync(stop.Token);
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, host.Port);
        NetworkStream stream = client.GetStream();
        await DistrictProtocolCodec.WriteAsync(stream, new() { MessageType = DistrictMessageTypes.ClientHello });
        DistrictWireMessage? hello = await DistrictProtocolCodec.ReadAsync(stream);
        Assert.NotNull(hello);
        Assert.Equal(DistrictMessageTypes.ServerHello, hello!.MessageType);
        await DistrictProtocolCodec.WriteAsync(stream, new() { MessageType = DistrictMessageTypes.CreateSession, Seed = 7 });
        DistrictWireMessage? started = await DistrictProtocolCodec.ReadAsync(stream);
        Assert.NotNull(started?.Snapshot);
        Assert.Equal(DistrictMessageTypes.SessionStarted, started!.MessageType);
        stop.Cancel();
        await host.DisposeAsync();
        await running.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static DistrictCommand Command(string id, DistrictCommandType type) =>
        new(DistrictAuthorityProtocol.Version, "test", "client", 1, id, type);
}
