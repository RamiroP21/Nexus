# Production Milestone 03 — Superhuman Power Expression Vertical Slice

## Scope and goal

Milestone 03 adds a second systemic player power, **Kinetic Grasp**, to the accepted `UrbanBlock01` slice at HEAD `2fa5849`. Kinetic Vector remains the instantaneous impulse power; Grasp adds sustained physical manipulation so the player can acquire, hold, reposition, release and compose a physical object with Vector.

The milestone addresses the player-expression risk identified after 02C: a reactive city is not enough if controlling the superhuman remains a one-button experience. The implementation stays local to the Unity production surface. It does not add a third power, a resource system, a telekinetic grapple, direct civilian manipulation, or a Server/ECS bridge.

## Player powers and input

- `PrimaryPower`: existing mouse LMB / gamepad Right Trigger route; Kinetic Vector semantics are unchanged.
- `SecondaryPower`: new mouse RMB / gamepad Left Trigger route. Holding the action attempts acquisition and maintains Grasp; releasing it releases the object.
- No mandatory aim mode or hold-RMB camera mode was introduced.

Kinetic Grasp is a sustained ability component with explicit acquire, hold and break ranges, mass eligibility, line-of-sight validation and release reasons. A future power can reuse the input split without introducing a generic MMORPG ability framework.

## Eligibility and physical hold

`Graspable` is explicit opt-in on a `PhysicalTarget`. Props, cover, cargo, the authored service load and valid hazards opt in; civilians and hostiles are rejected even when they have Rigidbodies. Eligibility also checks active state, non-trigger collider, dynamic Rigidbody, range and configured maximum mass.

The held body remains a normal Rigidbody. `KineticGraspAbility` applies bounded spring/damper acceleration toward a camera-relative hold point in `FixedUpdate`; it does not assign `Transform.position`, kinematic state, constraints, gravity or velocity. Collisions therefore remain active, heavy objects respond more slowly, and release resumes ordinary Rigidbody behavior with current momentum. Persistent occlusion, invalid eligibility, depletion, disable/reset or break range releases fail closed without stale references.

## Composition and UrbanBlock integration

Vector receives the held object through the existing `IForceReceiver`/`PhysicalTarget` contract. An accepted Vector impulse releases the held target with a `Vector` reason; there is no hidden throw-combo branch or separate weapon class. The same contracts support environmental combat, hazard redirection and physical cover interception.

`UrbanBlock01` now opts its delivery cart, movable steel container, service load and hazard template into Grasp. The crisis state machine remains authoritative: Pressure B still depends on the resident's physical shelter route and Pressure C still observes the service load position. Grasp changes how precisely the player can manipulate those world objects; starting a hold does not auto-resolve a pressure. Civilians and hostiles remain non-Graspable. `F8` resets held state through the player reset event and restores authored Rigidbody baselines. `F9` adds Grasp target/state and acquire/hold/break ranges beside the existing crisis diagnostics. The normal reticle gains a Holding state and a subtle line feedback component exposes the held object's physical relationship with the player.

## Tests and evidence

Focused coverage includes:

- acquisition, opt-in rejection, mass/range/occlusion validation;
- spring convergence without teleportation or Rigidbody overrides;
- walking/rotation-compatible hold behavior and momentum-preserving release;
- line-of-sight, break range, disable, depletion/reset and Vector handoff;
- SecondaryPower gamepad input and release;
- UrbanBlock prop eligibility, civilian/hostile exclusion and crisis pressure integration.

The focused Unity set passes **5/5** (three Grasp playtests, SecondaryPower input, and UrbanBlock integration). The complete Unity regression passes **78/78**, with 0 failures and 0 skips. `dotnet build Nexus.sln -c Release --no-restore` completes with 0 warnings and 0 errors, and `dotnet test Nexus.sln -c Release --no-build --no-restore` passes **211/211**. GPU captures are written under `client/Nexus.Unity/Logs/UrbanBlock02A/`, including the Grasp holding state and the existing compound-crisis evidence. Captures are validation evidence, not a substitute for human feel evaluation.

## Stability, performance and debt

The hot path is one bounded `FixedUpdate` force calculation for the single held body; it allocates no collections and leaves physics ownership with Unity. Validation watches for oscillation, runaway forces, duplicate hazard contact, stale references and reset leaks. The greybox presentation remains provisional: no final animation, VFX, audio, external assets, tutorial framework or UI redesign is included. Character Grasp is deliberately deferred to avoid combat/animation/AI debt. Perfect AAA collision handling and strength progression remain future work.

## Agent workflow

One fresh GPT-6 Astra Low worker implemented the runtime Grasp contract and focused tests. Luna integrated input naming, prefab/scene authoring, feedback, UrbanBlock coverage, diagnostics, documentation, validation and Git closure. No second worker was needed: focused and full validation found no remaining runtime or physics defect.

**TECHNICAL VALIDATION: PASS**

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

The next action after technical PASS is a normal-player human playtest of the complete continuous slice. Do not start another production milestone from this document.
