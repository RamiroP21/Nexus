# Production Milestone 01C — First Real Combat Encounter

## Scope and status

01C promotes the 01B physical hazard into one bounded hostile encounter. It is not a complete combat system: one greybox hostile, one ranged attack, one compact playground lane, and a minimal win/lose/reset lifecycle. No second player power, melee system, dodge roll, navigation package, behavior-tree framework, production art/audio/VFX, server/ECS integration or 01D work was introduced.

- Baseline: `7ea398a` (`feat: add traversal and combat foundation`).
- Worker: one fresh GPT-6 Astra Low worker implemented runtime, authoring and six focused journeys; its advanced quota was exhausted after implementation and the reset-pose correction.
- Orchestrator: Luna performed path correction, Unity/.NET validation, scope audit, documentation and closure. No second worker or substantial Luna reimplementation was used.
- 01B human experiential validation is recorded as **PASS**. 01C remains **EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**.

## Hostile architecture

`HostileCombatant` owns the minimal encounter-facing state machine: `Idle`, `Position`, `Telegraph`, `Attack`, `Recover`, `Staggered` and `Depleted`. It has explicit serialized references to the player, hazard template, muzzle and presentation. Perception is bounded by range and a raycast line-of-sight check; occluded players are not acquired and the hostile does not fire through cover.

Positioning uses bounded local steering on the existing Rigidbody. The hostile orients toward the player, approaches a preferred distance, opens space when too close, and stops when a local obstacle or unsupported step blocks movement. No pathfinding or general AI framework was added.

The attack loop is readable and timed: `Position (0.45 s) → Telegraph (1.15 s) → Attack (0.12 s) → Recover (1.6 s)`. The telegraph line and greybox label are visible before release. The attack instantiates the existing physical `TrainingHazard`; it travels through the world, can hit the player with force/damage, and can be redirected by Kinetic Vector. A valid physical impact above threshold interrupts the telegraph/attack into `Staggered` for 0.65 s. A three-second stagger protection window prevents repeated impulses from permanently locking the hostile.

`CombatEncounter` owns only lifecycle state (`Ready`, `Active`, `Won`, `Lost`), reset bodies and hazard cleanup. Player depletion ends pressure; hostile depletion ends pressure; reset restores the player, hostile, cover and prop. The existing `PhysicalTarget`, `DamageReceiver`, `CollisionDamage`, `PlayerVitality`, `TrainingHazard` and `ImpactBarrier` contracts remain the integration points. No enemy-specific branch was added to `KineticVectorAbility`.

## Playground and targeting

The existing `SuperhumanPlayground` is extended in place with an east combat lane, a greybox hostile, one reusable releasable cover and one kinetic prop. The earlier 01B sentinel/hazard fixtures remain. Cover can block the physical hazard and changes the encounter's positioning problem; a moved rigidbody prop can damage the hostile through the existing collision/damage path.

The accepted 01A camera-intent targeting remains unchanged. The focused pressure test verifies direct targeting of the hostile while the same playground contains hazards and props; there is no hidden `Enemy > Hazard > Prop` priority. Targeting and traversal source files were not rewritten.

## Validation

Focused 01C Play Mode journeys passed **6/6**:

1. range/line-of-sight perception and repositioning before attack;
2. committed telegraph aim, release and recovery/no-spam;
3. Kinetic Vector stagger/interruption with protection against permanent stagger lock;
4. physical hazard travel, player hit and deflection back to the hostile;
5. solid cover blocking a hazard and reset restoring the cover;
6. environmental prop damage, win/lose/reset and traversal regression.

The first test execution produced no result artifact because of an incorrect PowerShell path composition. The next execution exposed one simple reset-pose assertion (`cover position 8.56` instead of `8.00`). The worker corrected the Rigidbody/Transform pose restoration in `CombatEncounter.ResetEncounter` and `HostileCombatant.ResetCombatant`; the final focused run passed 6/6. Luna made no architectural or gameplay rewrite.

The 01B traversal regression passed **5/5** after the hazard contact changes. The complete Unity EditMode/PlayMode regression passed **57/57**, 0 failed and 0 skipped, using Unity 6000.3.2f1 with the graphical backend required by the repository's existing RenderTexture tests. Unity logs contain no C# errors, exceptions or crash markers; the only environment diagnostic is the known licensing access-token message.

Root validation passed:

- `dotnet build Nexus.sln -c Release --no-restore`: 0 warnings, 0 errors.
- `dotnet test Nexus.sln -c Release --no-build --no-restore`: **211/211** (Core 48, ECS 100, Simulation 59, Determinism 4).

The existing visual evidence under `client/Nexus.Unity/Logs/Production01B/` was reviewed: the courtyard composition, hazard/telegraph readability, greybox actors and staggered state render correctly. 01C's six Play Mode tests validate the new encounter states and physics; no dedicated 01C camera captures were added, keeping validation-only closure free of unrelated capture plumbing. The authored hostile, telegraph, cover and prop are serialized in the existing scene and are visible through the normal Game View flow.

## Required human review and debt

**TECHNICAL VALIDATION: PASS**

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

Human review should answer whether the hostile appears reactive, the telegraph gives enough response time, ordinary movement/traversal is sufficient, cover and deflection feel intentional, environmental damage feels systemic rather than scripted, and camera-intent targeting remains trustworthy under pressure.

Remaining debt is deliberately bounded: greybox visuals, provisional reticle art/tuning, no production animation/VFX/audio, no multiple hostile archetypes or squads, no navigation/pathfinding, no melee/weapon inventory, no progression/save/city systems, and no Nexus.Server/ECS integration. Do not start 01D from this report.
