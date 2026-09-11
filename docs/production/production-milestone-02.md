# Production Milestone 02A — First Urban Block

## Scope and status

02A turns the accepted 01A/01B/01C foundations into one bounded production scene: a semantic greybox city block with a market, residential frontage, service alley, two civilians, one hostile, physical traversal, movable cover and one systemic situation with two outcomes. It is a first world slice, not a city simulation or a new gameplay phase.

- Baseline: `4c77b76` (`feat: add first production combat encounter`).
- Objective: provide a reusable UrbanBlock01 scene where traversal, targeting, hostile pressure, civilian reaction, physical props and persistent consequence compose without changing the accepted core contracts.
- Invariants: no Production → Experimental dependency; preserve 01A/01B/01C targeting, traversal, combat and reset behavior; no new package, Unity upgrade, navigation framework, server/ECS integration or 02B work.
- Worker: one fresh GPT-6 Astra Low worker implemented the scene, runtime world components, editor authoring and focused tests. Luna performed validation, review, documentation and Git closure.
- Status: **TECHNICAL VALIDATION: PASS**. **EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**.

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
