using System.Collections;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.CameraSystem;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.Interaction;
using Nexus.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class FirstCombatEncounterPlaytests
    {
        private HostileCombatant hostile;
        private PlayerVitality player;
        private CharacterMotor motor;
        private CombatEncounter encounter;
        private float capture;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.ScenePath);
            yield return new EnterPlayMode();
            capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            hostile = Object.FindFirstObjectByType<HostileCombatant>(); player = Object.FindFirstObjectByType<PlayerVitality>();
            Assert.That(hostile && player, Is.True, "Run AddFirstCombatEncounter authoring first.");
            motor = player.GetComponent<CharacterMotor>(); encounter = player.GetComponent<CombatEncounter>();
            player.GetComponent<LocalPlayerInput>().enabled = false; Time.timeScale = 1;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        { Time.captureDeltaTime = capture; Time.timeScale = 1; yield return new ExitPlayMode(); }
        private void Place(Vector3 position) { motor.ResetMotion(position); Physics.SyncTransforms(); }
        private void StartWarning()
        {
            Place(new Vector3(10, .05f, 12)); hostile.Tick(.01f); hostile.Tick(.5f);
            Assert.That(hostile.State, Is.EqualTo(HostileState.Telegraph));
        }
        private TrainingHazard Fire()
        { StartWarning(); hostile.Tick(hostile.TelegraphSeconds + .01f); return hostile.LastHazard; }

        [UnityTest]
        public IEnumerator PerceptionRequiresRangeAndSightAndPositionsBeforeAttack()
        {
            hostile.Tick(.5f); Assert.That(hostile.State, Is.EqualTo(HostileState.Idle)); Assert.That(hostile.Target, Is.Null);
            Place(new Vector3(10, .05f, 12));
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.transform.position = new Vector3(10, 1.5f, 14);
            cover.transform.localScale = new Vector3(4, 3, .5f); Physics.SyncTransforms();
            hostile.Tick(.5f); Assert.That(hostile.Target, Is.Null, "Opaque cover prevents acquisition.");
            Object.DestroyImmediate(cover); Physics.SyncTransforms();
            hostile.Tick(.01f); Assert.That(hostile.State, Is.EqualTo(HostileState.Position)); Assert.That(hostile.Target, Is.EqualTo(player));
            Assert.That(hostile.GetComponent<Rigidbody>().linearVelocity.z, Is.GreaterThan(0), "Close hostile opens space before warning.");
            hostile.Tick(.5f); Assert.That(hostile.State, Is.EqualTo(HostileState.Telegraph)); Assert.That(hostile.AttackCount, Is.Zero);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WarningCommitsAimThenAttackAndRecoveryPreventSpam()
        {
            StartWarning(); hostile.Tick(hostile.TelegraphSeconds * .5f); Assert.That(hostile.AttackCount, Is.Zero);
            Place(new Vector3(13, .05f, 12)); hostile.Tick(hostile.TelegraphSeconds * .51f);
            Assert.That(hostile.State, Is.EqualTo(HostileState.Attack)); Assert.That(hostile.AttackCount, Is.EqualTo(1));
            Assert.That(Mathf.Abs(hostile.LastHazard.GetComponent<Rigidbody>().linearVelocity.x), Is.LessThan(.1f), "Warning aim does not track a sidestep.");
            hostile.Tick(.13f); Assert.That(hostile.State, Is.EqualTo(HostileState.Recover));
            hostile.Tick(hostile.RecoverySeconds * .5f); Assert.That(hostile.State, Is.EqualTo(HostileState.Recover));
            Assert.That(hostile.AttackCount, Is.EqualTo(1));
            encounter.ResetEncounter(); yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OrdinaryKineticTargetingInterruptsWithoutPermanentStaggerLock()
        {
            StartWarning(); var camera = Object.FindFirstObjectByType<ThirdPersonCamera>(); camera.enabled = false;
            camera.transform.position = new Vector3(10, 1.2f, 11); camera.transform.LookAt(hostile.GetComponent<PhysicalTarget>().AimPoint);
            var selector = player.GetComponent<ContextualTargeting>(); selector.Suspended = false; Physics.SyncTransforms();
            Assert.That(selector.TrySelect(out var selected), Is.True); Assert.That(selected.Target, Is.EqualTo(hostile.GetComponent<PhysicalTarget>()));
            Assert.That(player.GetComponent<KineticVectorAbility>().TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(hostile.State, Is.EqualTo(HostileState.Staggered)); Assert.That(hostile.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(36));
            var physical = hostile.GetComponent<PhysicalTarget>();
            for (int i = 0; i < 25; i++)
            { physical.ReceiveForce(new ForceImpact(Vector3.right * 8, physical.AimPoint)); hostile.Tick(.1f); }
            Assert.That(hostile.StaggerCount, Is.EqualTo(1), "Repeated impulses cannot refresh stagger while protected.");
            Assert.That(hostile.AttackCount, Is.EqualTo(1), "Hostile earns an attack window after interruption.");
            encounter.ResetEncounter(); yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PhysicalHazardTravelsHitsAndCanBeDeflectedBack()
        {
            var hazard = Fire(); float health = player.Receiver.Health.Current;
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.Receiver.Health.Current, Is.LessThan(health)); Assert.That(hazard.Spent, Is.True);
            encounter.ResetEncounter(); hazard = Fire(); var physical = hazard.GetComponent<PhysicalTarget>();
            Vector3 incoming = physical.Body.linearVelocity; float enemyHealth = hostile.GetComponent<DamageReceiver>().Health.Current;
            physical.ReceiveForce(new ForceImpact(-incoming.normalized * 36, physical.AimPoint));
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            Assert.That(hostile.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(enemyHealth), "Returned physical hazard damages its source.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SolidCoverStopsHazardAndResetRestoresReleasedCover()
        {
            StartWarning(); var shield = GameObject.CreatePrimitive(PrimitiveType.Cube); shield.transform.position = new Vector3(10, 1.2f, 13.6f);
            shield.transform.localScale = new Vector3(2, 2.4f, .4f); Physics.SyncTransforms();
            hostile.Tick(hostile.TelegraphSeconds + .01f); float health = player.Receiver.Health.Current;
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.Receiver.Health.Current, Is.EqualTo(health)); Assert.That(hostile.LastHazard.Spent, Is.True);
            Object.Destroy(shield);
            var cover = GameObject.Find("Encounter reusable cover").GetComponent<ImpactBarrier>(); var physical = cover.GetComponent<PhysicalTarget>();
            Vector3 start = physical.Body.position; physical.ReceiveForce(new ForceImpact(Vector3.right * 36, physical.AimPoint));
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(cover.Released, Is.True); Assert.That(Vector3.Distance(start, physical.Body.position), Is.GreaterThan(.1f));
            encounter.ResetEncounter(); Assert.That(cover.Released, Is.False); Assert.That(physical.Body.position, Is.EqualTo(start));
            Assert.That(physical.Body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeAll)); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MovingPropDamagesHostileAndWinLoseResetPreserveTraversal()
        {
            var prop = GameObject.Find("Encounter kinetic prop").GetComponent<Rigidbody>();
            prop.position = hostile.transform.position + new Vector3(0, .85f, -2.5f); prop.linearVelocity = Vector3.forward * 14; Physics.SyncTransforms();
            float health = hostile.GetComponent<DamageReceiver>().Health.Current;
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            Assert.That(hostile.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(health), "Environmental rigidbody collision has health consequence.");
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); encounter.Evaluate(); Assert.That(encounter.State, Is.EqualTo(EncounterState.Won));
            int attacks = hostile.AttackCount; hostile.Tick(10); Assert.That(hostile.AttackCount, Is.EqualTo(attacks));
            encounter.ResetEncounter(); Assert.That(hostile.State, Is.EqualTo(HostileState.Idle)); Assert.That(encounter.State, Is.EqualTo(EncounterState.Ready));
            player.ReceiveHit(new Damage(100), Vector3.zero); encounter.Evaluate(); Assert.That(encounter.State, Is.EqualTo(EncounterState.Lost));
            encounter.ResetEncounter(); Assert.That(player.Depleted, Is.False);
            Place(new Vector3(7, .05f, -15));
            for (int i = 0; i < 12; i++) motor.Tick(Vector2.zero, false, false, 0, 1f / 60, Time.timeAsDouble);
            Assert.That(motor.TryBeginTraversal(Vector3.forward), Is.True); Assert.That(motor.State, Is.EqualTo(MotorState.Vault));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
