using UnityEngine;

namespace Nexus.Feel01
{
    [RequireComponent(typeof(LineRenderer), typeof(AudioSource))]
    public sealed class KineticPulse : MonoBehaviour
    {
        [SerializeField] private FeelSettings settings;
        [SerializeField] private FeelPlayer player;
        [SerializeField] private FeelCamera orbit;
        [SerializeField] private Transform muzzle;
        private LineRenderer beam;
        private AudioSource sound;
        private AudioClip pulseSound;
        private float readyAt, flashUntil, landingUntil;
        private Vector3 endPoint;
        private bool hasTarget;
        private string targetName;
        private GUIStyle labelStyle;
        public int ShotCount { get; private set; }
        public Rigidbody LastBody { get; private set; }
        public Vector3 LastImpulse { get; private set; }
        public Vector3 LastPoint { get; private set; }
        // Optional local observers; Feel 01 has none and retains its existing behavior.
        public event System.Action<Vector3, float, Rigidbody> Impact;

        private void Awake()
        {
            beam = GetComponent<LineRenderer>();
            sound = GetComponent<AudioSource>();
            beam.enabled = false;
            // Generated placeholder transient: no imported audio and no asset writes.
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / 22050f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * (180 * t + 700 * t * t)) * Mathf.Exp(-45 * t) * .2f;
            }
            pulseSound = AudioClip.Create("Kinetic pulse placeholder", samples.Length, 1, 22050, false);
            pulseSound.SetData(samples, 0);
        }

        private void OnDestroy() { if (pulseSound) Destroy(pulseSound); }

        // Camera targeting first, then shoulder-to-hit occlusion prevents firing through nearby walls.
        private bool Target(Ray ray, out RaycastHit hit, out Vector3 point)
        {
            bool found = Physics.Raycast(ray, out hit, settings.range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            point = found ? hit.point : ray.GetPoint(settings.range);
            var fromMuzzle = point - muzzle.position;
            if (Physics.Raycast(muzzle.position, fromMuzzle.normalized, out var cover, fromMuzzle.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            { hit = cover; point = cover.point; found = true; }
            return found;
        }

        public bool Fire(Ray aim)
        {
            if (player.Paused || Time.time < readyAt) return false;
            readyAt = Time.time + settings.cooldown;
            bool hit = Target(aim, out var target, out endPoint);
            LastBody = hit ? target.rigidbody : null;
            LastImpulse = aim.direction.normalized * settings.impulse;
            LastPoint = endPoint;
            if (LastBody && !LastBody.isKinematic)
                LastBody.AddForceAtPosition(LastImpulse, endPoint, ForceMode.Impulse);
            player.AddImpulse(-aim.direction.normalized * settings.selfImpulse);
            ShotCount++;
            flashUntil = Time.time + .12f;
            beam.SetPosition(0, muzzle.position);
            beam.SetPosition(1, endPoint);
            beam.enabled = true;
            sound.PlayOneShot(pulseSound);
            if (hit) Impact?.Invoke(endPoint, settings.impulse, LastBody);
            return true;
        }

        public void LandingFeedback(float speed) { landingUntil = Time.time + Mathf.Min(.25f, speed * .012f); }

        // Explicit opt-in for experiments that reset while an impulse is still cooling down.
        public void ResetPulse()
        {
            readyAt = flashUntil = landingUntil = 0;
            ShotCount = 0; LastBody = null; LastImpulse = LastPoint = Vector3.zero;
            hasTarget = false; targetName = null; beam.enabled = false; sound.Stop();
        }

        private void Update()
        {
            beam.enabled = Time.time < flashUntil;
            if (player.Paused) return;
            hasTarget = Target(orbit.AimRay, out var hit, out _) && hit.rigidbody && !hit.rigidbody.isKinematic;
            targetName = hasTarget ? hit.rigidbody.name : null;
        }

        private void OnGUI()
        {
            if (player.Paused) return;
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            float x = Screen.width * .5f, y = Screen.height * .5f;
            GUI.color = Time.time < flashUntil ? Color.white : hasTarget ? new Color(.3f, 1, .8f) : new Color(1, 1, 1, .8f);
            float size = Time.time < flashUntil ? 12 : 7;
            GUI.DrawTexture(new Rect(x - size, y - 1, size * 2, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - size, 2, size * 2), Texture2D.whiteTexture);
            if (hasTarget) GUI.Label(new Rect(x - 140, y + 18, 280, 25), targetName, labelStyle);
            GUI.color = new Color(.3f, 1, .8f);
            float ready = Mathf.Clamp01(1 - (readyAt - Time.time) / settings.cooldown);
            GUI.DrawTexture(new Rect(x - 20, y + 12, 40 * ready, 2), Texture2D.whiteTexture);
            if (Time.time < landingUntil)
                GUI.DrawTexture(new Rect(x - 22, y + 42, 44, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
