using System.Collections;
using System.IO;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.Interaction;
using Nexus.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class TraversalCombatPlaytests
    {
        private CharacterMotor motor;
        private PlayerVitality player;
        private TrainingSentinel sentinel;
        private float priorCapture;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.ScenePath);
            yield return new EnterPlayMode();
            priorCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            motor = Object.FindFirstObjectByType<CharacterMotor>(); player = motor.GetComponent<PlayerVitality>();
            sentinel = Object.FindFirstObjectByType<TrainingSentinel>();
            Assert.That(player && sentinel, Is.True, "Run AddTraversalCombat authoring before these fixture tests.");
            motor.GetComponent<LocalPlayerInput>().enabled = false; Time.timeScale = 1;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        { Time.captureDeltaTime = priorCapture; Time.timeScale = 1; yield return new ExitPlayMode(); }

        private void StepMotor(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) motor.Tick(Vector2.zero, false, false, 0, 1f / 60, Time.timeAsDouble + i / 60.0);
        }
        private void Place(Vector3 position)
        { motor.ResetMotion(position); Physics.SyncTransforms(); StepMotor(.2f); }

        [UnityTest]
        public IEnumerator ContextualVaultMantleAndBlockedClearance()
        {
            Place(new Vector3(7, .05f, -15));
            Assert.That(motor.Grounded, Is.True);
            Capture("vault-before");
            motor.Tick(Vector2.up, false, true, 0, 1f / 60, Time.timeAsDouble);
            Assert.That(motor.State, Is.EqualTo(MotorState.Vault));
            StepMotor(1.5f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(-13)); Assert.That(motor.Grounded, Is.True);
            Capture("vault-after");
            Place(new Vector3(15, .05f, -17.5f));
            Assert.That(motor.TryBeginTraversal(Vector3.forward), Is.True);
            Assert.That(motor.State, Is.EqualTo(MotorState.Mantle));
            StepMotor(1.5f);
            Assert.That(motor.transform.position.y, Is.InRange(1.85f, 2.1f));
            Assert.That(motor.Grounded, Is.True);
            Capture("mantle-after");
            Place(new Vector3(15, .05f, -17.5f));
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = new Vector3(15, 3.1f, -17); ceiling.transform.localScale = new Vector3(4, .3f, 3);
            Physics.SyncTransforms();
            Assert.That(motor.TryBeginTraversal(Vector3.forward), Is.False, "Head clearance must reject traversal.");
            Object.Destroy(ceiling);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FallLandingDamageKnockbackDepletionAndRecovery()
        {
            Place(new Vector3(4, 7, -8)); StepMotor(.6f);
            Assert.That(motor.LastLandingSpeed, Is.GreaterThan(motor.Settings.HardLandingSpeed));
            Assert.That(motor.State, Is.EqualTo(MotorState.Landing));
            StepMotor(.5f); Assert.That(motor.State, Is.EqualTo(MotorState.Grounded));
            Vector3 before = motor.transform.position;
            Assert.That(player.ReceiveHit(new Damage(12), Vector3.right * 7 + Vector3.up * 2), Is.True);
            Assert.That(motor.State, Is.EqualTo(MotorState.Knockback));
            StepMotor(.2f); Assert.That(motor.transform.position.x, Is.GreaterThan(before.x + .4f));
            StepMotor(1); Assert.That(motor.State, Is.EqualTo(MotorState.Grounded));
            player.ReceiveHit(new Damage(100), Vector3.zero);
            Assert.That(player.Depleted, Is.True); Assert.That(motor.State, Is.EqualTo(MotorState.Depleted));
            before = motor.transform.position;
            for (int i = 0; i < 30; i++) motor.Tick(Vector2.up, true, true, 0, 1f / 60, Time.timeAsDouble);
            Assert.That(Vector3.Distance(before, motor.transform.position), Is.LessThan(.1f));
            player.ResetTraining(); StepMotor(.2f);
            Assert.That(player.Receiver.Health.Current, Is.EqualTo(player.Receiver.Health.Maximum));
            Assert.That(motor.State, Is.EqualTo(MotorState.Grounded));
            Assert.That(motor.transform.position.z, Is.InRange(-10.1f, -9.9f));
            motor.ResetMotion(new Vector3(0, -12, 0)); yield return null;
            Assert.That(motor.transform.position.y, Is.GreaterThan(-1), "Out-of-bounds fall resets to spawn.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SentinelTelegraphsPhysicalAttackAndCanBeInterrupted()
        {
            Place(new Vector3(-4, .05f, 12));
            sentinel.Tick(.01f);
            Assert.That(sentinel.State, Is.EqualTo(SentinelState.Telegraph));
            Assert.That(sentinel.AttackCount, Is.Zero);
            Capture("telegraph");
            sentinel.Tick(sentinel.TelegraphSeconds * .5f);
            Assert.That(sentinel.AttackCount, Is.Zero);
            var physical = sentinel.GetComponent<PhysicalTarget>();
            physical.ReceiveForce(new ForceImpact(Vector3.right * 8, physical.AimPoint));
            Assert.That(sentinel.State, Is.EqualTo(SentinelState.Staggered));
            sentinel.Tick(1); sentinel.Tick(.01f);
            Assert.That(sentinel.State, Is.EqualTo(SentinelState.Telegraph));
            sentinel.Tick(sentinel.TelegraphSeconds + .01f);
            Assert.That(sentinel.AttackCount, Is.EqualTo(1)); Assert.That(sentinel.State, Is.EqualTo(SentinelState.Recovery));
            Assert.That(sentinel.LastHazard.GetComponent<Rigidbody>().linearVelocity.magnitude, Is.GreaterThan(5));
            Capture("hazard-flight");
            float health = player.Receiver.Health.Current;
            for (int i = 0; i < 35; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.Receiver.Health.Current, Is.LessThan(health), "Actual physical hazard reaches a stationary CharacterController.");
            Assert.That(sentinel.LastHazard.Spent, Is.True);
            Capture("player-hit");
            sentinel.GetComponent<DamageReceiver>().Receive(new Damage(100));
            Assert.That(sentinel.State, Is.EqualTo(SentinelState.Disabled));
            sentinel.Tick(10); Assert.That(sentinel.AttackCount, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator KineticDeflectsHazardAndDamagesThreatWithoutSpecialAbilityRules()
        {
            Place(new Vector3(-4, .05f, 12)); sentinel.Tick(.01f); sentinel.Tick(sentinel.TelegraphSeconds + .01f);
            var hazard = sentinel.LastHazard;
            var target = hazard.GetComponent<PhysicalTarget>();
            Vector3 incoming = target.Body.linearVelocity;
            float health = sentinel.GetComponent<DamageReceiver>().Health.Current;
            target.ReceiveForce(new ForceImpact(-incoming.normalized * 36, target.AimPoint));
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Dot(target.Body.linearVelocity, incoming), Is.LessThan(0), "Ordinary force reverses incoming hazard.");
            Capture("hazard-deflected");
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            Assert.That(sentinel.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(health), "Deflected hazard damages its source through ordinary damage receiver.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PhysicalCoverReleasesAndTargetingRetainsCentreUnderThreat()
        {
            var barrier = Object.FindFirstObjectByType<ImpactBarrier>();
            Assert.That(barrier, Is.Not.Null);
            var target = barrier.GetComponent<PhysicalTarget>(); Vector3 start = target.Body.position;
            target.ReceiveForce(new ForceImpact(Vector3.right * 36, target.AimPoint));
            Assert.That(barrier.Released, Is.True); Assert.That(target.Body.constraints, Is.EqualTo(RigidbodyConstraints.None));
            Capture("cover-released");
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(start, target.Body.position), Is.GreaterThan(.5f));
            Place(new Vector3(-4, .05f, 12));
            var selector = motor.GetComponent<ContextualTargeting>(); selector.Suspended = false;
            var camera = Object.FindFirstObjectByType<Nexus.Gameplay.CameraSystem.ThirdPersonCamera>(); camera.enabled = false;
            camera.transform.position = new Vector3(-4, 1.2f, 11); camera.transform.LookAt(sentinel.GetComponent<PhysicalTarget>().AimPoint);
            Physics.SyncTransforms(); sentinel.Tick(.01f);
            Assert.That(selector.TrySelect(out var selected), Is.True); Assert.That(selected.Target, Is.EqualTo(sentinel.GetComponent<PhysicalTarget>()));
            var ability = motor.GetComponent<KineticVectorAbility>();
            float health = sentinel.GetComponent<DamageReceiver>().Health.Current;
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(sentinel.State, Is.EqualTo(SentinelState.Staggered));
            Assert.That(sentinel.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(health));
            Assert.That(GameObject.Find("Vault rail") && GameObject.Find("Mantle ledge") && GameObject.Find("Landing drop platform"), Is.True);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (!camera || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var texture = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Production01B"); File.WriteAllBytes("Logs/Production01B/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            { RenderTexture.active = previous; texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels); }
        }
    }
}
