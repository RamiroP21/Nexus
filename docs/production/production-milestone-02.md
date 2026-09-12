# Production Milestone 02A — First Urban Block

## Scope and status

02A turns the accepted 01A/01B/01C foundations into one bounded production scene: a semantic greybox city block with a market, residential frontage, service alley, two civilians, one hostile, physical traversal, movable cover and one systemic situation with two outcomes. It is a first world slice, not a city simulation or a new gameplay phase.

- Baseline: `4c77b76` (`feat: add first production combat encounter`).
- Objective: provide a reusable UrbanBlock01 scene where traversal, targeting, hostile pressure, civilian reaction, physical props and persistent consequence compose without changing the accepted core contracts.
- Invariants: no Production → Experimental dependency; preserve 01A/01B/01C targeting, traversal, combat and reset behavior; no new package, Unity upgrade, navigation framework, server/ECS integration or 02B work.
- Worker: one fresh GPT-6 Astra Low worker implemented the scene, runtime world components, editor authoring and focused tests. Luna performed validation, review, documentation and Git closure.
- Status: **TECHNICAL VALIDATION: PASS**. **EXPERIENTIAL VALIDATION: PASS (accepted human review)**.

## World composition

`UrbanBlock01.unity` is authored as a separate production scene and does not replace `SuperhumanPlayground`. Its four semantic roots are:

- `Main street - Calle Mercado`: street surface, crosswalk, road markings and block boundaries.
- `Crossing and corner market`: market frontage, awning, window and sign.
- `Residential access - Patio Sur`: two building fronts, courtyard wall, entrance beam and two refuge points.
- `Service alley and roof shortcut`: service building, vault rail, loading-dock mantle and raised walkway.

The scene reuses the accepted `SuperhumanPlayerRig` prefab and 01C hostile/hazard authoring. The delivery cart and steel waste container are real Rigidbody/`PhysicalTarget` props. The container starts as a solid obstacle and is released by a sufficient physical impact. No navigation or pathfinding package is introduced.

## Situation and consequences

`UrbanBlockSituation` owns the scene-level outcome state: `Quiet`, `Danger`, `Secured` and `SecuredWithCasualties`. The hostile's existing bounded perception and telegraph pressure activate the block; civilians perceive local danger and physically move toward explicit refuges. The delivery cart can block one route, so clearing a real obstacle changes the civilian outcome. A launched `TrainingHazard` can injure a civilian through the existing damage contract. Defeating the hostile produces either:

1. `Secured`: all residents sheltered and unharmed.
2. `SecuredWithCasualties`: the hostile is defeated but an injured resident remains injured.

The notice and resident labels expose the state, and the consequence survives the player leaving and returning to the scene. `F8` is a development-only reset that restores the player, hostile, hazards, physical props and civilians; it is not a save system or a release flow.

## Validation evidence

Focused UrbanBlock Play Mode coverage passed **8/8**. The same journeys produce the reviewed visual evidence under `client/Nexus.Unity/Logs/UrbanBlock02A/`: `block-overview.png`, `commercial-area.png`, `residential-service.png`, `traversal-route.png`, `incident-state.png`, `civilian-reaction.png`, `hazard-flight.png`, `outcome-a.png`, `outcome-b.png` and `returned-consequence.png` (the original `overview.png` is retained). The additional scene-load/render check passed **1/1**. Together these cover the requested block overview, commercial/residential/service areas, traversal route, calm/incident states, civilian reaction, hostile/hazard, both outcomes and the returned consequence. The focused journeys cover:

1. accepted player rig, semantic zones, two civilians and no Experimental dependencies;
2. hostile pressure, civilian flee/blocked/sheltered states and cart-blocked route;
3. cart removal, safe outcome and continuity after leaving/returning;
4. physical hazard injury and `SecuredWithCasualties` continuity;
5. camera-intent targeting and Kinetic Vector stagger under urban pressure;
6. waste-container cover, hazard stopping, prop damage and barrier release;
7. vault, mantle and return route through the service alley;
8. development reset of world, actors, props, health and player state.

The first focused attempt found a lifecycle ordering defect: `UrbanBlockSituation` evaluated before civilian receivers had initialized, producing a `NullReferenceException` and false quiet outcomes. The worker moved the initial evaluation to `Start`, then corrected the vault landing clearance and reset pose tolerance. The final focused and visual runs passed with no C# errors or unexpected exceptions.

The complete Unity EditMode/PlayMode regression passed **65/65**, 0 failed and 0 skipped, with Unity 6000.3.2f1 and the graphical backend required by the repository's RenderTexture tests. The only environment diagnostic in the log is the known licensing access-token message; URP shader dependency messages are existing editor import diagnostics, not errors from this feature.

Root validation passed:

- `dotnet build Nexus.sln -c Release --no-restore`: 0 warnings, 0 errors.
- `dotnet test Nexus.sln -c Release --no-build`: **211/211** (Core 48, ECS 100, Simulation 59, Determinism 4).

The renders show the street, market, residential massing, service alley, traversal fixtures, civilian/hostile actors and physical props as a coherent greybox composition. Human play is still required to judge pacing, civilian readability, route choice, cover/prop affordances and whether the consequence feels systemic rather than test-scripted.

## Changed surface and debt

Runtime additions are limited to `CivilianPresence` and `UrbanBlockSituation` under `Assets/Nexus/Gameplay/Runtime/World`. `UrbanBlockAuthoring` is editor-only and extends the existing authoring entry point with one explicit menu action. `UrbanBlockPlaytests` exercises observable scene behavior. Required Unity metadata is included. No existing combat, targeting or traversal source was changed; only the authoring class became `partial` to host the new menu action.

The slice remains deliberately greybox: no production art, animation, audio, VFX, persistence/save integration, civilian dialogue, multiple hostile archetypes, squad AI, navigation, server/ECS bridge or final reticle tuning. Current local Unity physics remains outside the deterministic headless contract. Do not start 02B until this scene receives human playtest review and an explicit mission.

## 02A.1 diegetic legibility pass

The first human review found that 02A communicated too much through large state labels and QA overlays. This pass keeps the situation rules intact and moves normal comprehension toward space, silhouette, movement, pose, materials and physical aftermath. It does not add a mechanic, archetype or simulation layer.

- Debug/presentation separation: UrbanBlock QA labels and diagnostics are hidden by default. `F9` toggles them for inspection; `F8` remains the development reset. Diegetic signs such as `MERCADO 24` and `PATIO SUR / HOMES` remain visible. The toggle is presentation-only and does not alter situation state.
- Civilians: added readable greybox details (hair, apron/bag or satchel) and state poses for normal, fleeing, blocked, sheltered, injured and depleted. Injury/depletion remain physical and persist after leaving and returning.
- Hostile/hazard: the urban hostile uses a distinct shoulder silhouette and state poses for telegraph/stagger/depleted; the existing telegraph line remains visible before damage. The physical hazard gains only a provisional trail, preserving deflection and collision contracts.
- Urban semantics: added non-colliding produce stalls/crates, residential windows/sills and bench, and stacked service pallets. These reinforce market, residential and service roles without rebuilding the block or introducing external assets.
- Outcomes: `Secured` and `SecuredWithCasualties` remain distinct through the disabled hostile, sheltered residents, injured/fallen civilian and displaced props; the presentation persists through return and resets with the authored baseline.

Focused legibility coverage passed **10/10**: the original eight UrbanBlock journeys plus debug-toggle and injury-pose/reset journeys. The final complete Unity regression passed **67/67**, 0 failed and 0 skipped. Visual evidence is generated under `client/Nexus.Unity/Logs/UrbanBlock02A/`: the calm street, market, residential/service area, traversal route, normal/fleeing residents, hostile telegraph/stagger, hazard flight, both aftermaths, returned aftermath and `debug-off-street.png`/`debug-on-street.png` pair. These are actual GPU RenderTexture captures; no pixel-diff assertions were added.

The pass used one fresh Astra Low worker for runtime presentation, authoring and focused tests. Luna applied the authoring method, corrected a simple capture helper compile issue, reviewed that diegetic signage stayed visible, ran focused/full Unity and .NET validation, and updated this report. The only known log diagnostics are the environment licensing access-token message and existing URP shader-import messages; no new C# errors or unexpected exceptions were observed.

**02A.1 TECHNICAL VALIDATION: PASS**

**02A.1 EXPERIENTIAL VALIDATION: PASS (accepted human review)**

The 02A/02A.1 human review is accepted for this milestone. Final art, animation, VFX/audio pipelines and reticle tuning remain deferred. 02B is a separate continuity slice documented below.

## 02B living block continuity

02B adds only a bounded continuity layer to the accepted UrbanBlock01 scene. The block now has three local phases: `Calm`, `Incident` and `Aftermath`. The existing `UrbanSituationState` outcomes (`Quiet`, `Danger`, `Secured` and `SecuredWithCasualties`) remain unchanged and continue to own consequence semantics.

### Calm life and authored activity

At scene start the hostile is held in incident readiness and the block is `Calm`; no combat pressure or instant damage occurs. A spatial incident boundary at the market crossing gives the player time to walk the market, residential frontage and service route, including the existing vault/mantle shortcut, before activation. Each resident has two explicit local activity anchors and a short wait/walk loop. Movement is bounded, physical and authored in the scene; there is no scheduler, crowd system, pathfinding package or global routine model.

### Interruption and hostile integration

Entering the crossing boundary, a civilian alert, physical impact or hostile harm transitions the block to `Incident`. The hostile is released from its separate hold and its existing perception, telegraph, hazard, stagger and depletion rules run unchanged. Residents keep their current positions when normal activity is interrupted, then perceive local danger and flee toward their existing authored refuges. There is no countdown, popup, objective screen or cutscene, and activation itself does not damage the player.

### Aftermath, return and reset

Defeating the hostile transitions the phase to `Aftermath`. `Secured` residents remain sheltered initially and can make a short cautious move toward an authored aftermath anchor after a pause; `SecuredWithCasualties` retains injured/depleted residents, displaced props and the same cautious survivor behavior. Routine never revives residents, repositions props, reactivates the hostile or clears damage. The player remains in control, can leave and return, and observes the same outcome and post-crisis state. F8 restores the authored Calm baseline; F9 continues to expose phase/activity/outcome QA data without changing simulation or normal diegetic presentation.

### Validation and evidence

The focused UrbanBlock suite passed **12/12**: the original 10 02A/02A.1 journeys plus Calm activity/interruption and Aftermath cautious-return/persistence coverage. The complete Unity EditMode/Play Mode regression passed **69/69**, 0 failed and 0 skipped, on Unity 6000.3.2f1 with the repository graphical backend. Root validation passed `dotnet build Nexus.sln -c Release --no-restore` with 0 warnings/errors and `dotnet test Nexus.sln -c Release --no-build --no-restore` with **211/211** (Core 48, ECS 100, Simulation 59, Determinism 4). Logs contain only the known licensing access-token diagnostic and existing regression warnings; no new C# errors or unexpected exceptions were observed.

GPU camera evidence is retained under `client/Nexus.Unity/Logs/UrbanBlock02A/`. The 02A/02A.1 captures cover the calm market/residential/service composition, traversal, incident, flee, hostile pressure, both outcomes, away/return and debug OFF/ON. 02B adds `02b-calm-local-activity.png`, `02b-activity-interrupted.png` and `02b-aftermath-survivor.png`, showing authored activity, interruption and continued post-crisis movement without QA overlays. These are camera renders, not a substitute for human play feel.

The implementation used one fresh GPT-6 Astra Low worker for runtime, authoring and focused tests. Luna reviewed the diff, ran authoring, corrected no gameplay contract, restored the known incidental URP settings change, ran focused/full Unity and .NET validation, and completed this report. No second worker was required.

### Debt, deviations and status

The slice remains deliberately greybox. Final art, animation, audio/VFX, dialogue, navigation, schedules, economy, crowd behavior, save integration, server/ECS integration and new mechanics remain out of scope. Activity movement is intentionally local and Rigidbody-based, and visual evidence remains validation-only. No targeting, traversal, combat, damage/force, hazard deflection, outcome or debug-off contracts were intentionally changed.

**02B TECHNICAL VALIDATION: PASS**

**02B EXPERIENTIAL VALIDATION: PASS (accepted human review)**

The 02B human review is accepted for this milestone. The next authorized step is the integrated 02C slice documented below.

## 02C compound urban crisis vertical slice

02C composes the accepted block systems into one bounded crisis rather than adding another microphase. The intended loop is `NORMALITY → WARNING → COMPOUND CRISIS → PRIORITY/ROUTING → PLAYER ACTION → WORLD REACTION → TRADE-OFF → CONSEQUENCE → AFTERMATH → CONTINUED EXPLORATION`.

### Phase model and warning

`UrbanBlockSituation` now owns a local `Calm`, `Warning`, `Incident` and `Aftermath` phase. Calm preserves the 02B authored activities and traversal. Entering the existing incident boundary produces a four-second diegetic Warning: the hostile remains held, the two pressure beacons become visible, and player control is unchanged. After Warning, all three pressures begin together; there is no countdown HUD, objective menu, popup or cutscene.

### Compound pressure architecture

`CompoundCrisisPressure` is an opt-in scene component with independent component outcomes and progress. Pressure A remains the existing 01C `HostileCombatant` (perception, positioning, telegraph, hazards, stagger and depletion unchanged). Pressure B is a localized resident emergency using the existing shelter route and delivery obstruction: the resident can be protected by clearing the physical route, otherwise the deadline produces a casualty. Pressure C is a separate service-unit load: the authored load can be moved clear with the existing physical force contract, otherwise the unit reaches a visible degraded/ruptured state with persistent debris. B and C run concurrently, with separate clocks and outcomes; resolving one does not resolve another.

The scene keeps the three pressures at distinct points of the existing block. The same Kinetic Vector contract therefore serves combat (A), rescue/route clearance (B) and infrastructure/load manipulation (C). Existing vault, mantle and elevated shortcut remain the traversal route between pressures; no traversal mechanic or navigation package was added. Physical composition permits natural cross-interactions such as hazards meeting cover/props and displaced loads changing access.

### Priorities, civilians and outcomes

Focused journeys cover A-first, C-first with switching, and B+C-first before A, plus a continuous mixed run. Pressure progress persists while the player changes priority; no first-choice hard-lock is introduced. Residents retain local 02B states and respond from their actual positions. The aggregate does not use a permutation enum: hostile, civilian and infrastructure results remain separate (`Pending`, `Active`, `Resolved`, `Failed`) and combine into the existing secured/casualty semantics. Aftermath accepts excellent, imperfect and casualty/damaged results; hostile state, resident injury, displaced props and ruptured infrastructure remain visible, and the player can leave and return without reset.

### Reset, QA and validation

F8 resets the player, hostile, civilians, pressure actors, load, props, progress and damaged infrastructure to Calm. F9 exposes phase plus A/B/C status and progress while leaving simulation untouched and preserving the debug-off diegetic baseline. Targeting continues to use camera intent with no hidden type priority.

The compound focused suite passed **4/4**, including warning-before-incident, pressure independence, priority switching, favorable/imperfect outcomes, persistence/return and a continuous integrated journey. The 02B focused suite remained **12/12** after opt-in isolation. The complete Unity regression passed **73/73**, 0 failed and 0 skipped, on Unity 6000.3.2f1 with the repository graphical backend. Root validation passed `dotnet build Nexus.sln -c Release --no-restore` with 0 warnings/errors and `dotnet test Nexus.sln -c Release --no-build --no-restore` with **211/211** (Core 48, ECS 100, Simulation 59, Determinism 4). Logs contain only the known licensing access-token diagnostic and existing intentional regression warnings; no new C# errors or unexpected exceptions were observed.

GPU evidence remains under `client/Nexus.Unity/Logs/UrbanBlock02A/`: 02C adds `02c-warning.png`, `02c-three-pressures.png`, `02c-infrastructure-resolved.png`, `02c-civilian-fails-infrastructure-stable.png`, `02c-imperfect-aftermath.png`, `02c-favorable-aftermath.png` and `02c-continuous-aftermath.png`. Combined with the accepted 02A/02A.1/02B captures, the set covers Calm life, Warning, three-pressure overview, hostile pressure, civilian emergency, infrastructure emergency, traversal, degrading pressure while handling another, systemic physical aftermath, favorable/imperfect/casualty outcomes, away/return and one F9 reference. Captures are GPU camera renders, not proof of fun.

The implementation used one fresh GPT-6 Astra Low worker for the compound runtime, authoring and focused tests. Luna integrated the authoring method, added one bounded continuous/capture test pass, restored the known incidental URP settings change after Unity runs, reviewed serialized references/diff, ran focused/full Unity and .NET validation, and completed documentation. Worker 2 was not needed.

### Debt, continuous play and status

The slice remains greybox and intentionally local: no second power, hostile archetype, mission/quest HUD, global scheduler, crowd/traffic, save-to-disk, dialogue, production art/animation/audio/VFX, Blender/external assets, package upgrades or Server/ECS bridge. Automated continuous coverage validates one uninterrupted integrated journey with real Play Mode physics and return/reset, but it is a short simulated validation flow rather than the requested 8–12 minute normal-player session. A human should play the complete slice for several minutes to judge pacing, readability, priority pressure, routing value and whether consequence feels earned.

**02C TECHNICAL VALIDATION: PASS**

**02C EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

After this mission the next action is human playtest, not another production implementation mission. Do not start 02D.
