# Production Milestone 01 — Superhuman Core

## Authorized transition and mission 01A

The owner accepted Gameplay Feel 01, Reactivity 01, Triage 01, Persistence 01, Vertical Slice 01 and Session Loop 01A as preproduction references. Session Loop 01B is cancelled as a separate prototype, superseding its pending status in the historical design/findings documents. This is the first permanent gameplay foundation, not another spike. This decision does not retrospectively invent human-playtest measurements or declare final game feel.

- Objective: reusable player/camera, contextual target selection, one physical ability, separate force and damage, reactive targets and a permanent development playground.
- Invariants: no Production → Experimental dependencies; preserve prototypes, shared Input asset, .NET kernel/ECS and package versions. No server/ECS integration, second power, full combat, city, production art or subsequent mission.
- Baseline: main `49c12f3`, equal to the accepted 01A production baseline before 01B work. The sole pre-existing URP 9→10/folder diff exactly matched the authorized restoration; only that file was restored.
- Evidence: inspect dependencies; Unity compilation and tests; seven technical Play Mode flows; camera renders; final scoped Git review.
- Budget: no subagents, at most two major functional corrections and two visual correction rounds.
- Stop: unsafe unrelated changes, major architectural redesign, non-convergent failures or exhausted correction budget. Publish only with technical PASS.
- Done: required runtime, scene, targeting rework, tests, Play Mode evidence and documented decisions. 01A is technically and experientially accepted by the owner; reticle art/tuning remains provisional and deferred.

## Promotion review

Feel01 supplies the accepted movement constants, camera-relative acceleration/braking, buffered/coyote jump, air momentum, impulse decay, orbit sensitivities, exponential following and collision compression. These algorithms are adapted into `CharacterMotor`, `JumpWindow`, `ThirdPersonCamera` and dedicated configuration assets, not referenced from the experiment. Ground support uses CharacterController contact rather than the prototype's potentially self-intersecting ground query. Input/pause ownership is moved to `LocalPlayerInput`; motor has no knowledge of abilities, scenes, UI or reset.

Reimplemented: camera-centre contextual selection with bounded screen-space soft assistance and hysteresis; small transient centre-reticle feedback; reusable cooldown; Kinetic Vector execution; force contract; health/damage; reactive/depleted posture; collision-to-damage opt-in. The contextual target is reselected/revalidated on activation, so stale candidate state cannot fire through cover. Both camera and force-origin visibility matter. Invalid/no target consumes neither cooldown nor recoil; successful activation applies one impulse, optional independent damage and opposite-direction player recoil. Unlike the experimental air/wall shot, an invalid production target cannot be used for self-launch; unrestricted traversal activation is deliberately deferred. Default direct damage is 12 on opted-in receivers, independently configurable down to zero. Plain props have no health component.

Not promoted: FeelArena snapshots, experimental respawn/reset/HUD and generated audio; ReactionStage global-in-scene stimuli, actor steering and debug labels; TriageDirector coordinate rules; persistence codec/save paths; SliceIncident outcomes/freeze rules; SessionLoopDirector agenda, timers and hardcoded endpoints. Their dependency chain runs through Feel01/Reactivity01 and, for the slice, Persistence01. They remain unchanged laboratory/regression code.

## Runtime and assets

`Assets/Nexus/Gameplay/Runtime` builds `Nexus.Gameplay`, referencing only Unity and the existing Input System. Conceptual folders: Character, Camera, Interaction, Abilities, Combat and World. A single runtime assembly avoids a premature multi-assembly dependency graph; the folder boundaries and small APIs reflect the actual consumers. `Nexus.Gameplay.Editor` owns one-time scene authoring/open tools; `Nexus.Gameplay.Tests` owns unit/integration validation. There are no global singletons or production dependencies on experimental assets.

The reusable `SuperhumanPlayerRig.prefab` contains motor, input adapter, camera, targeting, Kinetic Vector and feedback with explicit references. The shared `Nexus/Input/NexusInput.inputactions` is reused unchanged, cloned per input owner and disposed with it. Movement, camera, targeting and Kinetic Vector have separate ScriptableObjects; health and collision thresholds are per-target serialized settings. Disable/enable retains health and cooldown; input and observer subscriptions are cleaned up. This single-player input owner restores prior timescale/cursor on disable; future multi-owner pause coordination is not implemented.

`IForceReceiver` accepts an impulse and world-space application point. `PhysicalTarget` is the current Rigidbody-backed adapter; Kinetic Vector does not know concrete prop or entity types. It also reports real collisions to reaction observers without applying their physics impulse a second time. `DamageReceiver` owns a plain `Health` object with finite/nonnegative damage validation, clamping and terminal depletion. Force does not imply damage. The ability's damage setting can be zero; bodies without a receiver remain force-only. `CollisionDamage` independently converts sufficiently strong normal impacts to damage on opted-in bodies. `ReactiveEntity` shows flinch/recovery/depleted posture; `DamageStateVisual` exchanges intact casing for bent/exposed geometry. No NPC cognition, enemy behavior or combat formulas are implied.

The camera has a configurable shoulder offset. Both that offset and the backward camera distance are sphere-swept against cover, excluding the player. This addresses the observed centred-player obstruction of near targets. Close compression temporarily hides the player's renderers and restores them on camera disable. Bounded non-allocating queries fail conservatively if saturated. Target choice is local Unity presentation logic, not a deterministic ECS ordering guarantee.

`Scenes/SuperhumanPlayground.unity` is a bounded courtyard with steps/landing, camera alcove, targeting cover, lightweight pallet (2 kg), heavy equipment (18 kg), another movable pallet, articulated training mannequin and damageable cabinet. Scene materials/configs are new production assets, not experimental references. The scene is appended to development Build Settings without replacing previous scenes or claiming a release startup flow. No mandatory laboratory HUD is introduced. The centre reticle is neutral/candidate/activation screen feedback; the effect trace remains transient world feedback.

## 01A validation and acceptance

TECHNICAL VALIDATION: PASS

EXPERIENTIAL VALIDATION: PASS — OWNER ACCEPTED

The first human playtest rejected the target-bound ring and its implied selection behavior. The scoped rework implemented camera-centre priority, bounded assistance, hysteresis, activation revalidation and a small screen-centre reticle. The owner subsequently accepted the 01A targeting behavior as the experiential baseline. The reticle art and final tuning remain provisional/deferred. See [targeting-rework-01.md](targeting-rework-01.md) for the implementation record.

The first complete Unity run passed 33/37 tests: all 25 existing tests and eight new rules tests passed, while four new Play Mode tests correctly rejected missing prefab config references. Asset creation before a scene switch had left the prefab without its four ScriptableObject references. The correction established the scene first, added a missing-reference guard to authoring, repaired only those four prefab references and added explicit prefab-reference assertions. After this correction, the targeted production suite passed 13/13 (eight rules tests plus five actual Play Mode journeys, including indirect collision coverage). Logs/XML retain the failed first run rather than hiding it.

Initial visual inspection then found the player obscuring the selected target. One bounded visual correction added shoulder framing and an assertion that player/target projections are separated. The prior full suite passed **38/38**. After the targeting rework, the targeted suite passed **13/13**, and the fresh complete Unity regression passed **46/46**, 0 failed, 0 skipped, runner exit 0. One targeting lifecycle correction was required after the first rework run; no final game-feel claim is made.

Final observations: walk 5.41 m over 1 s; sprint 6.28 m over 0.8 s including acceleration; one jump with repeat rejected; camera compressed to 2.70 m near cover. Light/heavy displacement over comparable observation intervals was 5.932 m / 0.226 m with the same 36 kg m/s impulse. Four successful activations covered the four interaction targets; rapid attempts did not add activations. Cabinet health changed from 36 to 24 and its casing visibly changed. These are fixture observations, not general performance/feel guarantees.

Visual review covered the existing spawn/overview, light selection and impulse, heavy impulse, reactive flinch/depletion, bent cabinet panels, compressed camera and absent-target feedback renders. The corrected framing exposes both player and target; materials render normally. The owner accepted the 01A targeting behavior in human play; the reticle is OnGUI screen feedback and its final art/tuning remains provisional and deferred. Figures remain simple articulated silhouettes and feedback is deliberately provisional.

Final compilation reported no C# warnings/errors or new required-feature runtime errors. The logs retain the environment licensing access-token diagnostic and the intentional Persistence01 invalid-save warning from regression coverage. The initial authoring run also warned about the then-empty test assembly; the completed test assembly removes that warning in subsequent runs. No warnings were suppressed.

The seven requested technical flows are covered as follows:

| Flow | Execution and observable assertion |
| --- | --- |
| Spawn/move/sprint/jump/orbit | Gamepad movement, walk versus sprint displacement, grounded initialization, one jump with midair repeat rejected, stick orbit and camera-cover compression. |
| Light object | Contextual selection, right-trigger activation, force-only pallet moves over two metres. |
| Heavy object | Same ability and impulse; 18 kg body moves less than half the 2 kg body's displacement over comparable intervals. |
| Reactive entity | Trigger activation displaces the mannequin, emits impact and shows a flinch; force-only configuration leaves health unchanged. |
| Damage | Cabinet casing changes on damage; mannequin health clamps at zero with depleted posture; a real incoming rigidbody collision separately produces reaction and damage. |
| Invalid target | Disabled, kinematic, out-of-range and occluded candidates rejected; aiming into empty sky produces no activation/cooldown and hides feedback. |
| Cooldown | Rapid trigger attempts activate once; direct attempts return CoolingDown; disable/enable does not reset it. |

The fixture uses a fresh scene for each test, synthetic gamepad states and real Unity physics. Camera aim is adjusted through the camera API for repeatable interaction framing; orbit itself is also tested with a gamepad stick. The player is never teleported to satisfy a route. The isolated candidate-range test moves its target, not the player. Tests restore Input System settings, devices, capture timestep and Play Mode. Camera renders are actual URP renders, not Editor UI screenshots; no image is synthesized.

Root regression: `dotnet build Nexus.sln -c Release` passed with 0 warnings/errors; `dotnet test Nexus.sln -c Release --no-build` passed 211/211 (Core 48, ECS 100, Simulation 59, Determinism 4). No Host diagnostics were rerun because no simulation contract/code changed. Windows standalone build, physical controller and human playtest were not run; they are not implied by Editor evidence. No package, Unity version, render-pipeline or .NET change was made.

Reproduce Unity from the repository root using the installed Unity 6000.3.2f1 executable with `-batchmode -projectPath client/Nexus.Unity -buildTarget Win64 -runTests -testPlatform EditMode -testResults <absolute-output.xml> -logFile <absolute-output.log>`. Tests enter actual Play Mode through the existing Editor-test convention. Production-only filter: `-testFilter Nexus.Gameplay.Tests.SuperhumanPlaytests`; the full run covers all Unity assemblies. Evidence is ignored local output: `artifacts/targeting01a-{targeted-3,full}.{log,xml}` and `client/Nexus.Unity/Logs/Production01/*.png`/observations. Runtime/editor source, prefab, scene, configs, materials, metadata and this report are versioned; generated logs/build artifacts are not.

## 01B traversal and combat foundation

The bounded 01B slice is documented in [superhuman-core-01b.md](superhuman-core-01b.md). 01B has **TECHNICAL VALIDATION: PASS** (five focused journeys, 51/51 full Unity regression, .NET 211/211) and **EXPERIENTIAL VALIDATION: PASS — OWNER ACCEPTED**. It adds contextual vault/mantle, fall recovery, player knockback/depletion, a telegraphing sentinel, physical hazards/deflection and impact-releasable cover. It does not change the accepted 01A targeting behavior.

## 01C first real combat encounter

The first hostile encounter is documented in [superhuman-core-01c.md](superhuman-core-01c.md). 01C has **TECHNICAL VALIDATION: PASS** (six focused journeys, five traversal-regression journeys, 57/57 full Unity regression, .NET 211/211) and **EXPERIENTIAL VALIDATION: PASS — OWNER ACCEPTED**. It adds one dynamic hostile with bounded perception, local positioning, a telegraphed physical attack, stagger/interruption, cover, environmental prop damage and a minimal win/lose/reset lifecycle. One Astra worker implemented the slice; Luna performed validation and closure. 01D is not authorized.

## Use and explicit debt

Open **Nexus → Production → Open Superhuman Playground**, then Play. WASD/mouse or gamepad sticks; Shift/left-stick press sprint; Space/south button jump; click/right trigger Kinetic Vector; Esc/Start pause/resume. The small screen-centre reticle is neutral without a candidate, changes subtly for a candidate and pulses on activation. There is no target-bound ring, experimental completion/reset loop or lock-on mode; stop/re-enter Play for a fresh playground.

Deferred: production animation and character model, advanced traversal polish, melee/weapon systems, multiple enemies and NPC AI, VFX, sound, final targeting tuning/reticle art, progression, save integration, Nexus.Server and ECS integration. Current local Unity physics is not part of the deterministic headless contract. No Session Loop 01B, 01D or subsequent production mission is authorized by this implementation.
