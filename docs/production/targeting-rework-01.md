# Production Targeting Rework 01 — Preparation

Status: preparation only. No runtime, scene, prefab or Input Actions were changed by this mission.

## Why the current experience was rejected

Production 01A is technically sound but the first human playtest rejected its targeting presentation. The player sees a target-bound cyan ring that is large relative to the target and reads as an imposed lock-on. It competes with the third-person view, makes the selected object look chosen by the system, and does not communicate the player's camera intent as directly as a small screen-centre cue would. This is an experiential failure, not evidence that force, damage or health contracts are wrong.

The replacement direction is:

`CAMERA INTENT → SMALL CENTRE RETICLE → SOFT AIM ASSISTANCE → DIRECT KINETIC VECTOR ACTIVATION`

It is an original third-person action-game interaction model, not a copy of another game's UI, assets or proprietary behavior. There must be no giant target ring, permanent lock-on, or mandatory hold-to-aim mode.

## Current implementation audit

### Runtime scripts and ownership

- `Runtime/Interaction/ContextualTargeting.cs` owns candidate discovery, validation and scoring. It queries up to 128 colliders around the force origin, requires a Rigidbody-backed `PhysicalTarget`, rejects the owner and non-dynamic bodies, then validates range, a camera-forward half-angle, and two line-of-sight raycasts (camera and force origin).
- `Runtime/Interaction/TargetingSettings.cs` currently configures `range = 24`, `halfAngle = 14°`, `distanceWeight = .12`, and a physics `queryMask`. Its score is `cameraAlignment - distanceWeight * distance/range`; alignment dominates distance, but the query is an origin-radius candidate set rather than a screen-space centre query.
- `Runtime/Interaction/TargetFeedback.cs` calls `TrySelect` every `LateUpdate` and draws a 24-segment `LineRenderer` ring around the selected object's bounds. It is target-bound, persistent while valid, and has no candidate-state reticle or stability/hysteresis state.
- `Runtime/Abilities/KineticVectorAbility.cs` calls `TrySelect` again at activation, applies the validated `ForceImpact`, optionally applies independent `Damage`, then applies recoil and consumes cooldown only after a receiver accepts the force.
- `Runtime/Character/LocalPlayerInput.cs` reads the existing Input System actions and directly calls `primaryAbility.TryActivate()` on `PrimaryPower.WasPressedThisFrame()`. The motor and camera do not own targeting decisions.
- `Runtime/Abilities/KineticFeedback.cs` draws a short transient world-space beam after successful execution. This is useful execution confirmation and should remain separate from selection feedback.

### Existing input

`Assets/Nexus/Input/NexusInput.inputactions` has one `Gameplay/PrimaryPower` action: mouse left button and gamepad right trigger. `Move`, `Look`, `Jump`, `Sprint` and `Pause` are separate actions. No RMB hold, aim action, target-cycle action or lock-on action exists. The minimum rework should reuse `PrimaryPower` unchanged and preserve direct mouse/gamepad activation; adding a separate action is not justified by the chosen direction.

### Existing tests and evidence

`GameplayRulesTests.cs` covers configuration, health/cooldown rules, prefab references and absence of Experimental dependencies. `SuperhumanPlaytests.cs` covers selection, invalid candidates, cooldown, force/damage, movement and visual captures. It currently asserts that a target-bound `TargetFeedback` is visible, which must be replaced when the implementation is authorized. There is no current test for centre-vs-lateral priority, candidate hysteresis, deliberate camera retargeting or reticle state transitions.

## Change map for the later implementation

| Area | Later action | Preparation decision |
| --- | --- | --- |
| `ContextualTargeting.cs` | Replace candidate query/scoring with camera-centre primary selection plus bounded soft-assist candidates and hysteresis. | Modify/reimplement; preserve `ValidatedTarget` and the activation-facing API where practical. |
| `TargetingSettings.cs` | Add small screen tolerance, assist angle/strength, hysteresis hold/switch margins and candidate query limits. | Modify; keep range/query mask and avoid a generic lock-on configuration. |
| `TargetFeedback.cs` | Replace target-bound ring with a small centre reticle state presenter. | Replace behavior in place or replace file; no world ring. |
| `KineticVectorAbility.cs` | Keep activation/effect/cooldown contract; consume the validated target from the new selector. | Intact except minimal integration if the selector API changes. |
| `KineticFeedback.cs` | Keep transient impact beam/feedback. | Intact. |
| `LocalPlayerInput.cs` | Keep direct `PrimaryPower` activation. | Intact except minimal reticle visibility wiring if needed. |
| `CharacterMotor.cs`, `ThirdPersonCamera.cs` | Preserve. | Intact; camera expresses intent through its existing forward direction. |
| Force, damage, health, `ReactiveEntity` | Preserve. | Intact; no reason for targeting UX to affect effect contracts. |
| Playground scene/prefab/Input Actions | Do not change in preparation. | Preserve; only later wiring may remove the ring component. |
| Tests | Add the exact cases below during implementation. | Do not add tests in this preparation mission. |

No file is proposed for deletion now. Obsolete ring-only code can be deleted only after the new reticle is implemented, references are migrated and tests prove no remaining consumer needs it.

## Proposed minimal targeting algorithm

The selector should return either a validated candidate or no candidate. It should not become a lock-on state machine.

1. Read the active camera centre ray from the production camera.
2. Perform a primary ray/shape query against `PhysicalTarget` candidates within range. A candidate intersecting the centre ray wins immediately after visibility and dynamic-target validation.
3. If the primary ray has no valid candidate, gather a bounded set of nearby candidates and project each aim point into viewport space. Reject candidates outside range, outside a small screen-space tolerance/assist cone, behind the camera, occluded, disabled, kinematic or owned by the player.
4. Score assistance primarily by screen-centre distance/angle. Use target distance only as a small tie-breaker. Add a modest projected-size allowance so a large object is not unfairly rejected, but do not let size or proximity override clear camera intent.
5. Keep the current candidate while it remains valid and inside a slightly larger release tolerance. Switch only when a new candidate exceeds it by a switch margin or the camera deliberately moves toward the new candidate. This is hysteresis, not target capture: no rotation, camera steering, input takeover or target persistence after leaving the tolerance.
6. Revalidate the selected candidate at activation. A stale highlight, obstruction or range change must return `InvalidTarget` and consume neither cooldown nor recoil.

Recommended initial tuning for a first implementation pass: centre tolerance about 2.5% viewport radius, assist tolerance about 8%, release tolerance about 10%, screen-centre weight dominant, distance tie-breaker below 15% of the score, and a switch margin large enough to prevent one-frame flicker but small enough to follow deliberate camera movement. These are starting values for playtest tuning, not final constants.

Two targets close together require a stable screen-space tie-breaker (centre distance, then projected size, then distance) and a retained current candidate. A farther centred target must beat a nearer lateral target. A partially hidden target must be rejected rather than selected by its visible edge unless its aim point remains visibly unobstructed. Large targets may receive a small tolerance benefit, never an automatic priority.

## Reticle UX

- Normal: a small, discreet centre reticle; no object ring and no permanent target label.
- Valid candidate: subtle colour/weight/brightness change at the same centre position. The player can read intent without a marker attached to the target.
- Activation: a brief reticle pulse plus the existing transient Kinetic feedback/impact response. Do not leave a persistent lock indicator.
- Invalid/no candidate: neutral reticle, no arbitrary selection, no cooldown, no recoil and no impact effect.
- Pause/disabled input: hide or neutralize the reticle consistently with the existing pause ownership.

The reticle should be a small screen-space/development-runtime component with no dependency on target geometry. It should expose a minimal state (`Neutral`, `Candidate`, `Activating` or equivalent) and be reusable by future abilities without becoming a HUD framework.

## Input and activation

Keep `Gameplay/PrimaryPower` direct: mouse left button and gamepad right trigger remain the activation equivalents. Do not add RMB hold, aim mode, target cycling or a second ability. `Look` continues to change camera intent; the ability samples the selector at activation. The flow remains:

`Input → Targeting snapshot/revalidation → Ability → Force/Damage effect`

The targeting component must never apply force, damage, recoil or cooldown. The ability must remain the owner of activation and effect execution.

## Exact implementation test plan

Add these tests only when the rework is authorized:

- centred valid target beats a nearer lateral target;
- lateral near target does not steal a centred target;
- invisible/occluded target is rejected;
- out-of-range target is rejected;
- small camera movement does not flicker between candidates;
- deliberate camera movement changes the candidate;
- two close candidates remain stable, with deterministic tie-breaking;
- large target receives only the documented tolerance benefit;
- no candidate leaves the reticle neutral and activation coherent;
- valid activation affects the intended target;
- invalid activation does not consume cooldown or recoil;
- reticle state is neutral/candidate/transient-activation as expected;
- direct mouse and gamepad activation both use the same validated snapshot;
- scene reload/disable clears transient reticle state without persistent target state.

The existing force, damage, health and movement tests should remain regression coverage. Do not modify those contracts to make targeting tests pass.

## Human acceptance criteria

The rework is not accepted until a human playtest can answer yes to all of these:

- I know approximately what I am pointing at from camera direction and the small reticle.
- The system does not choose a different nearby object in a distracting way.
- The reticle does not obstruct the scene.
- I can change target naturally by moving the camera.
- Kinetic Vector normally hits what I expect.
- The system does not feel like an artificial lock-on.
- Invalid aim produces no surprising force, cooldown or target jump.
- Impact feedback confirms execution without becoming a selection ring.

Record concrete observations and rejected cases; do not infer approval from automated tests or renders alone.

## Risks and boundaries

- Too-wide assist tolerance can recreate the rejected “system chooses for me” feeling; too-narrow tolerance loses the forgiving tool identity.
- Hysteresis can feel sticky if release/switch margins are not tuned against camera movement.
- Screen projection and occlusion vary with camera collision compression and large targets; test at near cover and ordinary distances.
- A small reticle can be lost against bright backgrounds; use subtle contrast without a giant marker.
- Direct activation must not accidentally reintroduce the old target-bound ring through stale prefab references.
- Do not change CharacterMotor, ThirdPersonCamera, Force, Damage, Health or `ReactiveEntity` unless implementation evidence proves a minimal integration need.

Out of scope: implementing or tuning runtime targeting, scene/prefab/input edits, combat, traversal, second ability, production VFX/audio, server/ECS, a general targeting framework, permanent lock-on, target cycling, RMB hold-to-aim and any new playtest content.

## Validation and delivery for this preparation

Because this mission changes documentation only, validation is static: inspect current scripts, input bindings, dependency direction and the documentation diff. No Unity tests, Play Mode, renders, build or .NET suite are required. The intended delivery is a docs-only commit:

`docs: define production targeting rework`

Working tree must contain only the two intentional documentation files, be clean after commit/push, and remain on `main`. No runtime or asset behavior is claimed to have changed.
