using UnityEngine;

namespace Nexus.Reactivity01
{
    public enum ReactionRole { Civil, Enemy }
    public enum ReactionState { Idle, React, Flee, Approach, Recover }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class ReactiveActor : MonoBehaviour
    {
        [SerializeField] private ReactionRole role;
        [SerializeField] private ReactionStage stage;
        [SerializeField] private Transform player;
        [SerializeField] private Renderer visual;
        [SerializeField] private TextMesh stateLabel;
        [SerializeField] private Camera view;
        private Rigidbody body;
        private MaterialPropertyBlock tint;
        private Vector3 away;
        private float stateUntil, nextThreat;
        public ReactionState State { get; private set; }
        public ReactionRole Role => role;
        public Vector3 Position => body ? body.position : transform.position;
        public StimulusKind LastStimulus { get; private set; }
        public int ReactionCount { get; private set; }
        private ReactionSettings Settings => stage.Settings;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (!stage || !stage.Settings || !player || !visual || !stateLabel || !view)
            { Debug.LogError("Reactive actor references missing.", this); enabled = false; return; }
            tint = new MaterialPropertyBlock();
            ResetReaction();
        }

        public void ResetReaction()
        {
            if (tint == null) return;
            // Arena teleports then sleeps bodies. Wake actors so interpolation refreshes even while Idle.
            body.WakeUp();
            away = Vector3.zero;
            nextThreat = Time.time + Settings.threatInterval;
            ReactionCount = 0;
            ChangeState(role == ReactionRole.Civil ? ReactionState.Idle : ReactionState.Approach, 0);
        }

        public void React(StimulusKind kind, Vector3 position, Rigidbody source)
        {
            if (!isActiveAndEnabled || tint == null) return;
            if (role == ReactionRole.Enemy)
            {
                // Threats are emitted by the enemy, never self-trigger recovery. Nearby-only pulses do not stun it.
                if (kind == StimulusKind.Threat || (kind == StimulusKind.KineticImpact && source != body)) return;
                if (kind == StimulusKind.PhysicalImpact && (position - body.position).sqrMagnitude > 2.25f) return;
                ChangeState(ReactionState.Recover, Settings.recoveryDuration);
            }
            else
            {
                away = Vector3.ProjectOnPlane(body.position - position, Vector3.up).normalized;
                if (away.sqrMagnitude < .01f) away = -transform.forward;
                // Repeated impacts redirect a fleeing civil without trapping it in an endless startle.
                if (State == ReactionState.Flee) stateUntil = Time.time + Settings.fleeDuration;
                else if (State != ReactionState.React) ChangeState(ReactionState.React, Settings.startleDuration);
            }
            LastStimulus = kind;
            ReactionCount++;
        }

        private void FixedUpdate()
        {
            if (State == ReactionState.React && Time.time >= stateUntil) ChangeState(ReactionState.Flee, Settings.fleeDuration);
            if (State == ReactionState.Flee && Time.time >= stateUntil) ChangeState(ReactionState.Idle, 0);
            if (State == ReactionState.Recover && Time.time >= stateUntil) ChangeState(ReactionState.Approach, 0);
            // During startle/recovery the body is free to carry the actual kinetic/collision impulse.
            if (State == ReactionState.React || State == ReactionState.Recover) return;
            Vector3 desired = Vector3.zero;
            if (State == ReactionState.Flee) desired = away * Settings.fleeSpeed;
            if (State == ReactionState.Approach)
            {
                Vector3 offset = Vector3.ProjectOnPlane(player.position - body.position, Vector3.up);
                if (offset.magnitude > Settings.stopDistance) desired = offset.normalized * Settings.approachSpeed;
                if (Time.time >= nextThreat)
                {
                    nextThreat = Time.time + Settings.threatInterval;
                    stage.Report(StimulusKind.Threat, body.position, 1, body);
                }
            }
            if (desired.sqrMagnitude > .01f)
            {
                Vector3 direction = desired.normalized;
                // Tiny local steering for an open courtyard, not navigation/pathfinding.
                if (body.SweepTest(direction, out _, .8f, QueryTriggerInteraction.Ignore))
                {
                    Vector3 side = Vector3.Cross(Vector3.up, direction);
                    if (!body.SweepTest(side, out _, .8f, QueryTriggerInteraction.Ignore)) desired = side * desired.magnitude;
                    else if (!body.SweepTest(-side, out _, .8f, QueryTriggerInteraction.Ignore)) desired = -side * desired.magnitude;
                    else desired = Vector3.zero;
                }
                if (desired.sqrMagnitude > .01f) body.MoveRotation(Quaternion.LookRotation(desired));
            }
            Vector3 horizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 change = Vector3.ClampMagnitude(desired - horizontal, Settings.acceleration * Time.fixedDeltaTime);
            body.AddForce(change, ForceMode.VelocityChange);
        }

        private void ChangeState(ReactionState state, float duration)
        {
            State = state;
            stateUntil = Time.time + duration;
            Color color = state == ReactionState.React ? Color.yellow : state == ReactionState.Flee ? new Color(1, .6f, .12f)
                : state == ReactionState.Recover ? new Color(.8f, .25f, 1)
                : role == ReactionRole.Enemy ? new Color(.85f, .15f, .2f) : new Color(.2f, .75f, 1);
            tint.SetColor("_BaseColor", color);
            visual.SetPropertyBlock(tint);
            stateLabel.text = (role == ReactionRole.Civil ? "CIVIL / " : "THREAT / ") + state.ToString().ToUpperInvariant();
        }

        private void LateUpdate()
        {
            stateLabel.transform.rotation = view.transform.rotation;
            // A visible flinch separate from the collider: does not distort physics.
            visual.transform.localRotation = State == ReactionState.React ? Quaternion.Euler(-18, 0, 10) : Quaternion.identity;
        }
    }
}
