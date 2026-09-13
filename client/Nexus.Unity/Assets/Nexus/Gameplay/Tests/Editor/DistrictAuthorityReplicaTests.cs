using Nexus.Gameplay.World;
using NUnit.Framework;

namespace Nexus.Gameplay.Tests
{
    public sealed class DistrictAuthorityReplicaTests
    {
        private static DistrictAuthorityWireSnapshot Snapshot(ulong sequence, string phase = "Calm") => new DistrictAuthorityWireSnapshot
        {
            sessionId = "district01-test", seed = 7, tick = sequence, serverSequence = sequence,
            phase = phase, crisis1 = "Pending", crisis2 = "Pending", infrastructure = "Stable", route = "Open",
            zones = new[] { new DistrictAuthorityWireZone { id = "district01.zone.a", visited = true, civilianState = "Normal" } },
            stateHash = "abc"
        };

        [Test]
        public void SnapshotReplacesSemanticReadModel()
        {
            var replica = new DistrictAuthorityReplica();
            Assert.That(replica.ApplySnapshot(Snapshot(3, "Incident")), Is.True);
            Assert.That(replica.HasSnapshot, Is.True);
            Assert.That(replica.Phase, Is.EqualTo("Incident"));
            Assert.That(replica.Tick, Is.EqualTo(3));
            Assert.That(replica.TryGetZone("district01.zone.a", out DistrictAuthorityWireZone zone), Is.True);
            Assert.That(zone.visited, Is.True);
        }

        [Test]
        public void StaleSnapshotAndDuplicateEventDoNotRegressState()
        {
            var replica = new DistrictAuthorityReplica();
            replica.ApplySnapshot(Snapshot(5, "Aftermath"));
            Assert.That(replica.ApplySnapshot(Snapshot(4, "Calm")), Is.False);
            var authorityEvent = new DistrictAuthorityWireEvent { sessionId = "district01-test", serverSequence = 6, simulationTick = 6, type = "Crisis2Started" };
            Assert.That(replica.ApplyEvent(authorityEvent), Is.True);
            Assert.That(replica.ApplyEvent(authorityEvent), Is.False);
            Assert.That(replica.LastEventType, Is.EqualTo("Crisis2Started"));
        }

        [Test]
        public void NewSessionSnapshotReconcilesEvenWhenSequenceResets()
        {
            var replica = new DistrictAuthorityReplica();
            replica.ApplySnapshot(Snapshot(9, "Incident"));
            DistrictAuthorityWireSnapshot newSnapshot = Snapshot(1, "Calm");
            newSnapshot.sessionId = "district01-new";
            Assert.That(replica.ApplySnapshot(newSnapshot), Is.True);
            Assert.That(replica.SessionId, Is.EqualTo("district01-new"));
            Assert.That(replica.LastServerSequence, Is.EqualTo(1));
        }
    }
}
