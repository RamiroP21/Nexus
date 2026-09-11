# Production Milestone 01B — Traversal + Combat Foundation

## Mission boundary

01B adds a bounded, reusable traversal/combat foundation to the accepted 01A playground. It does not advance the deterministic kernel/ECS, add a second ability, introduce enemies or AI, or alter the 01A targeting contract. The implementation is intentionally fixture-driven and remains local Unity presentation/physics code.

- Baseline: `main` at `49c12f3` (`refine production targeting experience`), with the working tree clean before the mission.
- Invariants: no new scene, package, Unity upgrade, experimental dependency, server/ECS integration, or targeting redesign; targeting remains centre-priority with bounded assist, hysteresis and direct-input activation.
- Scope: contextual vault/mantle, fall/landing recovery, knockback/depleted player state, a telegraphing training sentinel, physical incoming hazards, deflectable hazards, and an impact-releasable cover barrier.
- Stop condition: technical failures, unrelated changes, or a need to redesign the production architecture. 01C and later gameplay systems remain unauthorised.

## Runtime implementation

`CharacterMotor` now exposes explicit motion states (`Grounded`, `Rising`, `Falling`, `Landing`, `Vault`, `Mantle`, `Knockback`, `Depleted`). Traversal is contextual: a jump input first checks bounded physical probes for a low rail or higher ledge, validates top and destination clearance, then stages controller motion. Otherwise it falls back to the existing jump path. Falling records impact speed and applies a short landing recovery; `KnockBack`, `SetDepleted` and `ResetMotion` are explicit lifecycle operations.

`PlayerVitality` adapts the existing `DamageReceiver` to player-specific knockback and depletion behavior, with a safety floor and reset path. `LocalPlayerInput` keeps reset on the existing south/jump action when the player is depleted; no new input asset or global reset loop was introduced.

`TrainingSentinel` owns a small state machine (`Ready`, `Telegraph`, `Recovery`, `Staggered`, `Disabled`) and a visible line telegraph. `TrainingHazard` is a finite-life rigidbody projectile that reports swept/overlap/collision contacts, damages the player, and can be returned to its source by the existing Kinetic Vector force path. `ImpactBarrier` releases rigidbody constraints when hit by a physical target. Damage, force and collision response remain separate contracts.

The authoring tool extends the existing playground with a vault rail, mantle ledge, raised landing platform, releasable cover, hazard template and sentinel/telegraph fixtures. The player prefab receives only the required vitality/damage components. Existing 01A targeting assets and behavior were not rewritten.

## Evidence and corrections

The focused `TraversalCombatPlaytests` suite contains five journeys:

1. contextual vault/mantle and blocked-clearance fallback;
2. fall landing-speed recovery;
3. sentinel telegraph and recovery;
4. incoming hazard damage/knockback and depleted reset;
5. Kinetic Vector deflection plus impact-driven cover release.

The first focused run found one assertion-baseline error in the deflection test: sentinel health was sampled after the source collision had already damaged it. The test now captures the pre-impact baseline. Hazard contact handling also gained bounded swept/overlap receiver checks. The corrected focused run passed **5/5**.

The full Unity EditMode/PlayMode regression passed **51/51**, 0 failed, 0 skipped, using Unity 6000.3.2f1 with the graphical backend enabled so the repository's existing RenderTexture evidence tests can execute. Headless `-nographics` is not a valid full-suite mode because historical visual tests submit render requests; new 01B captures skip only when `GraphicsDeviceType.Null` is active. Existing 01A capture renders remain unchanged.

Root validation remained green: `dotnet build Nexus.sln -c Release --no-restore` completed with 0 warnings/errors and `dotnet test Nexus.sln -c Release --no-build` passed **211/211** (Core 48, ECS 100, Simulation 59, Determinism 4). No Host diagnostics were rerun because no deterministic simulation contract changed.

Visual evidence was rendered from the actual playground camera and retained locally under `client/Nexus.Unity/Logs/Production01B/`: `vault-before`, `vault-after`, `mantle-after`, `telegraph`, `hazard-flight`, `player-hit`, `hazard-deflected` and `cover-released`. These images verify fixture presence, telegraph/hazard state and cover release; they are not a substitute for human feel validation. The mantle camera framing is wide, so traversal readability should be checked in a fresh Game View session.

## Status and explicit debt

**TECHNICAL VALIDATION: PASS**

**EXPERIENTIAL VALIDATION: PASS — OWNER ACCEPTED**

The owner accepted vault/mantle timing, landing recovery, telegraph readability, hazard response, deflection and cover release as the 01B experiential baseline. 01A targeting was accepted by the owner as an experiential baseline, while its reticle art/tuning remains provisional and deferred; 01B does not reopen or redesign it.

Deferred: production animation/model, traversal polish and edge-case tuning, melee/ranged combat, enemies/NPC AI, VFX/audio, progression/save integration, server/ECS integration and any later production mission. Do not start 01C from this report.
