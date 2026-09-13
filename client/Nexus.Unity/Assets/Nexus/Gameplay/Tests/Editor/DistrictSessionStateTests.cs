using Nexus.Gameplay.World;
using NUnit.Framework;

namespace Nexus.Gameplay.Tests
{
    public sealed class DistrictSessionStateTests
    {
        [Test]
        public void EarlierLossSurvivesTravelAndChangesLaterReadinessWithoutVisualObjects()
        {
            var state = new DistrictSessionState();
            state.Visit(DistrictZone.Market);
            state.ObserveMarket(UrbanBlockPhase.Incident, PressureOutcome.Resolved, PressureOutcome.Failed, 0, 2);
            state.Tick(90);
            Assert.That(state.CanStartCrisis2(15), Is.False, "Active crisis must not skip the aftermath interval.");
            state.ObserveMarket(UrbanBlockPhase.Aftermath, PressureOutcome.Resolved, PressureOutcome.Failed, 0, 2);
            state.Visit(DistrictZone.Residential); state.Tick(14);
            Assert.That(state.CanStartCrisis2(15), Is.False);
            state.Visit(DistrictZone.Service); state.Tick(1);
            Assert.That(state.CanStartCrisis2(15), Is.True);
            Assert.That(state.ServiceDegraded, Is.True);
            Assert.That(state.Crisis1, Is.EqualTo(DistrictCrisisState.Failed));
            Assert.That(state.Zone(DistrictZone.Market).Sheltered, Is.EqualTo(2));
            Assert.That(state.Zone(DistrictZone.Service).Sheltered, Is.Zero);
            state.ObserveCrisis2(DistrictCrisisState.Active, state.ServiceDegraded);
            Assert.That(state.CanStartCrisis2(15), Is.False, "An occurred crisis must not restart on reentry.");
            Assert.That(state.Zone(DistrictZone.Service).RouteBlocked, Is.True);
        }
        [Test]
        public void ResetAllowsAlternateHistoryAndClearsEveryZone()
        {
            var state = new DistrictSessionState();
            state.ObserveMarket(UrbanBlockPhase.Aftermath, PressureOutcome.Failed, PressureOutcome.Failed, 1, 0);
            state.ObservePopulation(DistrictZone.Residential, 2, 1);
            state.Visit(DistrictZone.Service); state.Tick(30);
            state.ObserveCrisis2(DistrictCrisisState.Failed, true);
            state.Reset();
            Assert.That(state.CanStartCrisis2(0), Is.False);
            foreach (DistrictZone zone in System.Enum.GetValues(typeof(DistrictZone)))
            {
                Assert.That(state.Zone(zone).Visited, Is.False);
                Assert.That(state.Zone(zone).Injured, Is.Zero);
                Assert.That(state.Zone(zone).Sheltered, Is.Zero);
                Assert.That(state.Zone(zone).RouteBlocked, Is.False);
            }
            state.ObserveMarket(UrbanBlockPhase.Aftermath, PressureOutcome.Resolved, PressureOutcome.Resolved, 0, 2);
            state.Tick(20);
            Assert.That(state.ServiceDegraded, Is.False);
            Assert.That(state.CanStartCrisis2(15), Is.True);
            Assert.That(state.Crisis1, Is.EqualTo(DistrictCrisisState.Resolved));
        }
    }
}
