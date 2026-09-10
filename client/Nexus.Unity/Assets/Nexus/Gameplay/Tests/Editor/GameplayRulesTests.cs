using System;
using System.Linq;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.CameraSystem;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nexus.Gameplay.Tests
{
    public sealed class GameplayRulesTests
    {
        [Test]
        public void MovementDefaultsPreserveAcceptedBaseline()
        {
            var settings = ScriptableObject.CreateInstance<MovementSettings>();
            try
            {
                Assert.That(settings.IsValid, Is.True);
                Assert.That(settings.MoveSpeed, Is.EqualTo(6)); Assert.That(settings.SprintSpeed, Is.EqualTo(10));
                Assert.That(settings.JumpSpeed, Is.EqualTo(Mathf.Sqrt(2 * 24 * 1.8f)).Within(.001f));
                Assert.That(settings.AirControl, Is.EqualTo(.35f));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
        [Test]
        public void JumpRequiresSupportAndConsumesItOnce()
        {
            var jump = new JumpWindow(); jump.Request(1);
            Assert.That(jump.TryConsume(1, .1f, .12f), Is.False);
            jump.Support(1); Assert.That(jump.TryConsume(1.05, .1f, .12f), Is.True);
            jump.Request(1.06); Assert.That(jump.TryConsume(1.06, .1f, .12f), Is.False);
        }
        [Test]
        public void BufferedJumpAndExpiredCoyoteAreDistinct()
        {
            var jump = new JumpWindow(); jump.Request(1); jump.Support(1.08);
            Assert.That(jump.TryConsume(1.08, .1f, .12f), Is.True);
            jump.Support(2); jump.Request(2.2);
            Assert.That(jump.TryConsume(2.2, .1f, .12f), Is.False);
            jump.Support(3); Assert.That(jump.TryConsume(3, .1f, .12f), Is.False);
        }
        [Test]
        public void CooldownBoundaryAndInvalidTime()
        {
            var cooldown = new AbilityCooldown();
            Assert.That(cooldown.TryConsume(10, .5f), Is.True);
            Assert.That(cooldown.TryConsume(10.49, .5f), Is.False);
            Assert.That(cooldown.TryConsume(10.5, .5f), Is.True);
            Assert.That(cooldown.IsReady(double.NaN), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => cooldown.TryConsume(11, 0));
        }
        [Test]
        public void HealthClampsAndCannotDamageDepletedStateAgain()
        {
            var health = new Health(30);
            Assert.That(health.Apply(new Damage(12)), Is.True); Assert.That(health.Current, Is.EqualTo(18));
            Assert.That(health.Apply(new Damage(100)), Is.True); Assert.That(health.Current, Is.Zero);
            Assert.That(health.IsDepleted, Is.True); Assert.That(health.Apply(new Damage(3)), Is.False);
        }
        [Test]
        public void HealthRejectsInvalidAmountsAndZeroIsNoOp()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Damage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Damage(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Damage(float.PositiveInfinity));
            var health = new Health(8); Assert.That(health.Apply(new Damage(0)), Is.False); Assert.That(health.Current, Is.EqualTo(8));
        }
        [Test]
        public void ProductionSceneAndPrefabHaveNoExperimentalDependencies()
        {
            foreach (string path in new[] { SuperhumanPlaygroundAuthoring.ScenePath, SuperhumanPlaygroundAuthoring.PrefabPath })
            {
                Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Not.Null);
                Assert.That(AssetDatabase.GetDependencies(path, true).Where(p => p.Contains("/Experimental/")), Is.Empty, path);
            }
            var refs = typeof(CharacterMotor).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(refs.Any(n => n.StartsWith("Nexus.")), Is.False, "Runtime has no other Nexus assembly dependency.");
        }
        [Test]
        public void AuthoredProductionConfigurationsAreValid()
        {
            const string data = SuperhumanPlaygroundAuthoring.Root + "/Data/";
            Assert.That(AssetDatabase.LoadAssetAtPath<MovementSettings>(data + "Movement.asset").IsValid, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<CameraSettings>(data + "Camera.asset").IsValid, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<TargetingSettings>(data + "Targeting.asset").IsValid, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<KineticVectorSettings>(data + "KineticVector.asset").IsValid, Is.True);
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(SuperhumanPlaygroundAuthoring.PrefabPath);
            Assert.That(rig.GetComponentInChildren<CharacterMotor>().Settings, Is.Not.Null);
            Assert.That(rig.GetComponentInChildren<ThirdPersonCamera>().Settings, Is.Not.Null);
            Assert.That(rig.GetComponentInChildren<ContextualTargeting>().Settings, Is.Not.Null);
            Assert.That(rig.GetComponentInChildren<KineticVectorAbility>().Settings, Is.Not.Null);
        }
    }
}
