# Production Milestone 04 — Persistent District Session

## Purpose and boundary

Milestone 04 expands the accepted `UrbanBlock01` compound crisis into one authored, persistent district session. `District01` is a single scene containing three connected zones and two distinct authored situations. The local Unity gameplay surface remains authoritative for this milestone; there is no save-to-disk, scene streaming, Addressables migration, campaign state, or Server/ECS bridge.

## District and scene strategy

`District01.unity` is the smallest robust boundary: a copy of the accepted UrbanBlock01 production scene extended with authored connectors and two additional zones. No additive loading or world-partition framework was introduced.

- Zone A — Market / Mixed Use: the accepted UrbanBlock01 market, residential entrance, hostile pressure and compound crisis.
- Zone B — Residential / Community: courtyard, homes, community props and two residents.
- Zone C — Service / Infrastructure: utility yard, loading geometry, service workers and the second crisis.

The player travels through physical street connectors. The district includes a standard route, a residential vault shortcut, a service mantle shortcut and a service route that can become blocked by persistent emergency debris. Diegetic signs and authored geometry communicate function; F9 exposes exact technical identifiers only when QA is enabled.

## Session memory

`DistrictSessionState` is a plain C# session-domain object with no scene or visual references. It owns independent zone visitation/population/route snapshots, Crisis 1 and Crisis 2 outcomes, resident/infrastructure outcomes and a bounded aftermath clock. `DistrictSessionDirector` is the local Unity adapter: it observes authored actors, advances the bounded session clock, starts Crisis 2 after a readable aftermath interval and reconciles presentation. This boundary leaves future authoritative Server/ECS integration possible without hiding state in labels or random GameObjects.

## Crisis 1 and consequence chain

The accepted compound crisis remains intact: hostile pressure, resident rescue and service infrastructure pressure run through the existing Vector/Grasp/PhysicalTarget contracts. Its physical outcomes remain visible in the district.

After Crisis 1 reaches aftermath, Crisis 2 becomes eligible only after a six-second bounded interval and proximity to Zone C. If infrastructure was lost, Crisis 2 starts with a shorter deadline and the service route is degraded. The later situation therefore reads actual prior world state instead of branching to a cutscene or text-only flag.

## Crisis 2 — moving service emergency

`DistrictCargoEmergency` authors two runaway service pallets travelling along a marked utility lane. It is intentionally different from 02C: the pressure is a moving/spreading physical emergency rather than hostile-plus-blocked-exit repetition.

Two systemic response paths are available through existing powers and physics:

1. Vector-deflect a load laterally out of the lane.
2. Grasp and lift/carry a load above the safe clearance height.

No combo branch or bespoke minigame exists. While the player is nearby, local Rigidbody propulsion runs in `FixedUpdate`; when the player leaves, the bounded cognitive deadline continues without full-fidelity distant physics. Returning sees the evolved outcome and presentation. A failed or inherited emergency activates damaged utility presentation and a physical debris obstruction that changes navigation.

## Civilian continuity, reset and diagnostics

Zone residents and service workers are authored `CivilianPresence` actors. Injury/shelter state is observed into session memory and the same objects remain in the scene across travel; no NPC schedules, crowd simulation or respawn-on-zone-change logic was added. F8 resets the UrbanBlock actors, district cargo, civilians, routes and `DistrictSessionState` to the authored Calm baseline. F9 adds district phase, zone visits/routes, both crisis states, memory outcomes, deadline and inherited-damage diagnostics without changing simulation.

## Powers and targeting

Kinetic Vector remains the immediate impulse and is useful for hostile combat, hazard deflection and cargo redirection. Kinetic Grasp remains sustained physical manipulation for obstruction removal, defense, infrastructure interaction and hazard handling. Existing camera-intent targeting and explicit Graspable eligibility remain unchanged across the district.

## Validation and evidence

- Focused Unity district/session set: **6/6**.
- Full Unity regression: **84/84**, 0 failed, 0 skipped.
- `dotnet build Nexus.sln -c Release --no-restore`: 0 warnings, 0 errors.
- `dotnet test Nexus.sln -c Release --no-build --no-restore`: **211/211**.
- Authoring: `Create Persistent District 01` and route repair completed successfully in batch mode.
- GPU captures (debug off) are written under `client/Nexus.Unity/Logs/District01/`: district overview, Zone B, Zone C and Crisis 2 active. Existing UrbanBlock/02C captures remain available under `Logs/UrbanBlock02A/`.

The automated continuous flow covers scene loading, zone travel markers, Crisis 1 consequence observation, Crisis 2 response/evolution and F8 reset. It is not a substitute for the requested 15–20 minute human-equivalent run.

## Performance, corrections and debt

The district adds one bounded director update and one Crisis 2 physics loop for two bodies. Population observation uses cached arrays; no per-frame LINQ, scene searches, allocations or generic scheduler were introduced. Off-screen evolution advances scalar state only. The main technical corrections were a compiler cache refresh after a stale Unity import, a route-boundary repair, and test-harness fixes for first-frame visitation and physically valid cargo diversion.

The graybox remains provisional: no final art, animation, audio, VFX pipeline, dialogue, minimap, waypoint system, save format, network authority or third power. Crisis 2 does not add a hostile archetype. The final decision remains human: whether the district feels connected, memorable and worth exploring when no crisis is active.

## Agent workflow and status

One fresh GPT-6 Astra Low worker implemented `DistrictSessionState`, `DistrictCargoEmergency` and domain tests. Luna implemented the district authoring, director adapter, scene integration, diagnostics, evidence capture and validation. A second Astra worker was not needed.

**TECHNICAL VALIDATION: PASS**

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

The next action is the normal-player 15–20 minute district session and leave/return playtest. Do not start Milestone 05 from this document.
