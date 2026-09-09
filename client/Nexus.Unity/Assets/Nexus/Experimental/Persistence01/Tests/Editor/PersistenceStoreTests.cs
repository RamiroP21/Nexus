using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Nexus.Persistence01.Tests
{
    public sealed class PersistenceStoreTests
    {
        private string directory;
        private Persistence01StateStore store;
        private readonly List<string> warnings = new();
        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "Nexus.Persistence01.Store." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); warnings.Clear();
            store = new Persistence01StateStore(Path.Combine(directory, "outcome.json"), warnings.Add);
        }
        [TearDown]
        public void TearDown()
        {
            File.Delete(store.FilePath); File.Delete(store.FilePath + ".tmp"); File.Delete(Path.Combine(directory, "unrelated.txt"));
            Directory.Delete(directory);
        }
        [Test]
        public void MissingRoundTripReplacementAndExplicitClear()
        {
            Assert.That(store.Load(), Is.Null); Assert.That(warnings, Is.Empty);
            var a = new Persistence01State { version = 1, rescue = RescueOutcome.Protected, depot = DepotOutcome.Overrun, lostGround = 3 };
            var b = new Persistence01State { version = 1, rescue = RescueOutcome.Hit, depot = DepotOutcome.Contained, lostGround = 1 };
            foreach (var state in new[] { a, b })
            {
                Assert.That(store.Save(state), Is.True);
                var loaded = new Persistence01StateStore(store.FilePath).Load();
                Assert.That(loaded.rescue, Is.EqualTo(state.rescue)); Assert.That(loaded.depot, Is.EqualTo(state.depot));
                Assert.That(loaded.lostGround, Is.EqualTo(state.lostGround)); Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False);
            }
            string neighbor = Path.Combine(directory, "unrelated.txt"); File.WriteAllText(neighbor, "preserve");
            Assert.That(store.Clear(), Is.True); Assert.That(store.Load(), Is.Null); Assert.That(store.Clear(), Is.True);
            Assert.That(File.ReadAllText(neighbor), Is.EqualTo("preserve")); Assert.That(warnings, Is.Empty);
        }
        [Test]
        public void CorruptEmptyUnknownAndIncompleteRecordsFallBackWithoutDeletingEvidence()
        {
            foreach (string json in new[] { "", "broken", "{}", "{\"version\":9,\"rescue\":1,\"depot\":1}",
                "{\"version\":1,\"rescue\":99,\"depot\":1}", "{\"version\":1,\"rescue\":1}",
                "{\"version\":1,\"rescue\":1,\"depot\":2,\"lostGround\":-1}", new string('x', 4097) })
            {
                File.WriteAllText(store.FilePath, json); int before = warnings.Count;
                Assert.That(store.Load(), Is.Null); Assert.That(warnings.Count, Is.EqualTo(before + 1));
                Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(json));
            }
            Assert.Throws<ArgumentException>(() => store.Save(new Persistence01State()));
        }
        [Test]
        public void FailedPublicationDoesNotDestroyPreviouslySavedOutcome()
        {
            var state = new Persistence01State { version = 1, rescue = RescueOutcome.Hit, depot = DepotOutcome.Contained, lostGround = 0 };
            Assert.That(store.Save(state), Is.True); string previous = File.ReadAllText(store.FilePath);
            // An unavailable temp destination simulates an ordinary local IO failure.
            Directory.CreateDirectory(store.FilePath + ".tmp");
            try { Assert.That(store.Save(state), Is.False); Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(previous)); Assert.That(warnings.Count, Is.EqualTo(1)); }
            finally { Directory.Delete(store.FilePath + ".tmp"); }
        }
    }
}
