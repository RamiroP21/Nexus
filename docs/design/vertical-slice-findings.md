# Vertical Slice 01 — Integrated V1

TECHNICAL VALIDATION: PASS

EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST

## Mission

- Objective: test Power → Reaction → Choice → Consequence → Memory as one playable urban incident.
- Constraints: local Unity experiment, existing controller/power/reaction components, existing safe materials and primitives; no .NET, packages, downloads, later systems or subagents.
- Baseline: clean `main` at `96662386948fea3f2fd68a93b2308b4a09bb8962`, matching the live remote. Unity 6000.3.2f1; all 22 existing Unity tests passed before edits (`artifacts/verticalslice01/baseline.xml`).
- Budget: at most three implementation/play/observe/correct rounds. Stop on architectural expansion, unknown dirty work, structural regression or persistent failure after the bounded attempts.
- Done: integrated scene, factual outcomes, distinct persistent aftermaths, real Play Mode matrix, inspected camera captures, regression, final clean experiment memory, documentation and scoped publication if technical checks pass. Human acceptance remains separate.

## Place and incident

Scene: `Assets/Nexus/Experimental/VerticalSlice01.unity`. A small market passage, crossing, bench/planter frontage, loading depot and open service channel make a single greybox block. Buildings frame the north and west; the open centre keeps the market and loading lane visible together. The market canopy and teal depot facade are return landmarks. Four civilians use the accepted small reaction state machine; one kinetic intruder advances toward the depot.

There are six seconds to orient, move or prepare. The intruder then discharges into a nearby freight load, sending it diagonally across the crossing toward the exposed market visitor while advancing toward the loading bays. A short beam, impact flash, warning light and nearby civilian startle communicate the common origin. The initial discharge is authored; subsequent freight travel, impulses, collisions, reaction/recovery and threat motion use Unity physics. This is one incident with two spatial consequences, not two unrelated starts.

A / protect: deflect the moving freight or move its potential victim. Actual cargo/civil contact records Hit. A projected clear trajectory records Protected while it remains clear; intervention can reopen risk before the stable result closes.

B / contain: displace the intruder into the adjacent service channel, whose geometry holds its body below the street. Until then it recovers from kinetic hits and continues advancing. Crossing each of three loading bays leaves irreversible damage. Later containment does not erase lost ground. Damaged side fixtures are outside the approach corridor so their presentation does not automatically contain the threat.

The newer Integrated V1 mission explicitly permits exceptional or creative execution to improve both outcomes. This refines the absolute wording in `vertical-slice-01.md`: priorities are pressured by distance and concurrent physical progress; there is no mutual-exclusion rule that forces one need to fail. No guarantee of a perfect route is made.

## Power, response and improvisation

The accepted CharacterController, movement, sprint, jump, air control, camera, recoil and Kinetic Vector parameters are reused. The same input displaces freight, threat, civilians and portable props; recoil remains available for positioning. A light portable barrier and an empty delivery crate provide opportunities to interrupt, obstruct or redirect through ordinary physical interaction. No Rescue/Contain action or special solution trigger exists.

ReactionStage and ReactiveActor retain their local perception, flinch/flee/recover behaviour. Nearby pulses and relayed prop collisions cause visible reactions; the intruder emits threat stimuli. The exposed visitor reacts to actual freight contact. The hazard does not emit a broad proximity stimulus that automatically evacuates its target before impact.

## Consequence, memory and continuity

Contact leaves a visible accident cordon and fragments. Each taken loading bay shows damaged fixtures, spilled freight and changed paving. A stable factual pair closes after 1.5 seconds without changes; only then is it saved. The phase-ending message is small and offers a later return through pause, with no score or stars.

Storage reuses the accepted experimental record codec with independent ownership and path: `Application.persistentDataPath/VerticalSlice01/outcome.json`. Version 1 records Protected/Hit, Contained/Overrun and 0–3 lost loading bays. It does not use Persistence01's runtime file or store Rigidbody snapshots. Atomic publication and invalid/write-failure handling come from the existing codec; retry remains explicit on pause. This is local experimental reuse, not a production save architecture.

Returning reloads the same scene and reconstructs the recorded facts. Protected keeps the visitor and places cargo safely beside the market. Hit removes that visitor and leaves tipped freight, a cordon and fragments. Lost bays retain their damaged fixtures and spilled freight; containment adds a channel safety rail. The intruder is absent on return. Ordinary reset retains memory and reconstructs the same aftermath; explicit Clear removes only this experiment's JSON and temporary publication file.

## Presentation and inherited debt

No RESCUE, CONTAIN, AFTERMATH or actor-state labels are shown. Small market/depot signs identify places. Colour, motion, pulse flash, depot damage and civilian presence carry the basic state. Optional actor TextMesh references remain wired but their renderers are disabled. Debug numbers are recorded by tests, not required by gameplay.

Two opt-in fields reduce the old harness UI only in this scene: FeelArena shows controls during pause; KineticPulse shows its existing aim indicator contextually when a physical body is under the ray, without target-name text. Target selection and camera behaviour are unchanged. This is not soft targeting. The pause window and explicit clear/reload buttons remain experimental UI.

Primitive actors, simple authored damage, local steering, basic targeting, camera occlusion and provisional feedback remain debts. There is no physical gamepad or Windows player-build evidence. Duration is to be measured by Ramiro: the incident is intentionally short, and exploration, experimenting with strategies and returning should supply the session rather than artificial waits. A 10–15 minute session is a ceiling/target, not a measured claim.

The perimeter is a greybox boundary, not a sealed city: thrown freight can leave it. The inherited FeelArena fall recovery can return that body to its spawn; in the ignore run this happened after the Hit consequence was already recorded, and the creative run ended with freight below street level outside the block. Neither erased the factual outcome or the reconstructed accident, but this limits immediate spatial continuity for those routes and should be examined in the human playtest. A successful improvised containment was not demonstrated.

## Validation rounds

1. Scene authoring and compilation succeeded. The first integration run reached rescue-first save/reload/reset and captured BEFORE, ACTIVE, result A and aftermath A. B-first exposed an unintended 5 cm pavement lip that stopped the freight's lateral motion and spared the visitor without rescue intervention. The projected result was therefore Protected instead of the expected Hit. Corrected the pavement to a visual surface over the existing physical floor. The overview also prompted an explicit synchronization of reconstructed Rigidbody and Transform poses, with new assertions for both.
2. Full regression: 22/23 passed; all prior tests remained green. The reconstructed cargo physics/visual pose checks passed. Rescue-first still saved and returned correctly, but B-first again produced Protected. Static swept-volume inspection identified the portable barrier corner intersecting the departing freight path. Moved that barrier two metres north, retaining it as a movable affordance while clearing the initial hazard trajectory.
3. Final full suite: **23/23 passed, 0 failed, 0 skipped**, exit 0 (`artifacts/verticalslice01/round3.xml` and `round3.log`). All 22 baseline tests remained green; the added integration test completed all six strategies, save/reload/aftermath/reset and final clear. No fourth round was run.

### Final Play Mode matrix

Times below are seconds after the initial discharge at the recorded result sample, not total session duration. Both needs progress without countdown outcome logic.

| Strategy | Rescue | Depot | Lost bays | Observed sample |
| --- | --- | --- | --- | --- |
| Rescue first | Protected | Overrun | 3 | 11.08 s; freight hit by player pulse at 2.08 s |
| Containment first | Hit | Contained | 0 | 5.54 s; threat hit at 1.34 and 2.24 s |
| Alternate | Protected | Contained | 1 | 8.01 s; cargo, threat, then cargo interventions |
| Ignore | Hit | Overrun | 3 | 11.07 s; no shots |
| Try both | Protected | Contained | 1 | 8.12 s; sprint between priorities, actual threat recovery |
| Creative barrier | Hit | Overrun | 3 | 11.06 s; barrier moved, subsequent cargo shot missed |

The tested routes produced four distinct factual combinations. The faster combined route retained one lost bay; code does not impose that cost on all possible routes. The creative attempt establishes physical prop manipulation, not a successful emergent solution. Recoil/positioning uses are inherited from Feel and its passing regression, not a newly proven traversal route in this matrix.

Technical assertions cover a clean scene and six-second orientation, references, initial civilian reaction, concurrent hazard/territory progress, actual aimed hits, distinct outcomes, stable valid JSON, no per-frame rewrites, reload reconstruction, matching physics/rendered cargo poses, normal reset preserving memory, explicit clear/restart, finite bodies and grounded aftermath fragments. Reload was the chosen return mechanism; this round did not separately repeat Stop/Start Play for VerticalSlice01.

### Visual inspection and cleanup

Reviewed final 1280×720 BEFORE and ACTIVE views, RESULT-A-first / RESULT-B-first, AFTERMATH-A-first / AFTERMATH-B-first and market/depot close views. The market and depot are visible from spawn and distinguishable by frontage, paving and local signs. The entry beam and nearby yellow reaction expose the incident's origin. Result A preserves the visitor while damaging the depot; result B visibly leaves an accident while preserving the depot. Return retains the buildings and crossing, relocates freight, removes the struck visitor and retains distinct debris/rail arrangements. No missing materials or broken aftermath geometry were seen in these views; human readability of the fast event is still unproven. The space remains deliberately sparse greybox, with no claim of production composition.

Final log: no C# compiler errors/warnings or gameplay exceptions. The known non-blocking licensing token diagnostic remains; the existing invalid-memory regression deliberately emits its controlled warning. Tests exited 0. No Unity Editor process was left running.

Final read-only checks confirmed no real `VerticalSlice01/outcome.json`, no `.tmp`, and no `Nexus.VerticalSlice01.*` temporary test directory. Test clear was exercised in every strategy transition and after the final run. Logs, captures, Library and other generated artifacts remain ignored. No URP diff remained or required restoration. No .NET or package files changed; .NET tests were intentionally not run.

### Changed surface and publication

New local runtime (`SliceIncident`, `SliceCargoContact`), scene authoring/menu, integration test, scene, material/settings assets, assembly definitions and generated metadata. Shared code changes are the two opt-in HUD settings in FeelArena/KineticPulse; existing scenes default to their previous presentation. EditorBuildSettings only appends the new scene and preserves startup order. Findings record the current mission's explicit compatibility refinement above.

Publication is scoped to `feat: integrate first nexus vertical slice` after diff/staging review. The final response records the resulting SHA, live remote and clean state. Approximately 20 minutes of implementation/validation; no experiential verdict or next-phase work.

Evidence is local and ignored: `artifacts/verticalslice01/` for XML/editor logs; `client/Nexus.Unity/Logs/VerticalSlice01/` for URP camera captures and strategy traces. Captures are rendered camera views, not Editor screenshots and do not include IMGUI. The matrix uses synthetic gamepad movement/RT and camera aiming with actual physics; it never teleports the player to perform an intervention. A fixed overview camera is used only for comparing the place.

## Human playtest

Open **Nexus → Vertical Slice 01 → Open gameplay scene** and enter Play. Controls: WASD / left stick, mouse / right stick, Space / South jump, Shift / L3 sprint, click / RT impulse, Esc / Start pause. Orient in the market passage, respond to the incident, explore the result, then choose **Volver más tarde** in pause or stop/start Play. Explore both ends of the changed place. Clear explicitly to try another strategy; ordinary Reset preserves the outcome.

Questions for Ramiro (and a second human before accepting the broader slice):

1. Did this feel like a place rather than a test?
2. Did you understand what was happening?
3. Did Kinetic Vector work as a systemic tool?
4. Did you have to prioritize?
5. Did you understand what happened where you were not present?
6. Did you try improvising?
7. Did the aftermath remind you of your decision?
8. Did you want to try another strategy?
9. Did this feel like one coherent experience or several prototypes joined together?
10. Does it begin to feel like Nexus?
11. Do you want to keep playing after the incident?

Record observed playthroughs, route/priority, unprompted understanding, desire to replay, time spent and paired world comparisons. Technical success cannot answer these questions. No work beyond Vertical Slice 01 is authorized by this implementation.
