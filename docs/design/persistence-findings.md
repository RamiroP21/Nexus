# Persistence Spike 01 — V1

TECHNICAL VALIDATION: PASS

EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST

## Mission

- Objective: play a factual outcome, persist it and return to a visibly changed place.
- Constraints: local Persistence01 only; accepted Feel/Reactivity/Triage and .NET remain unchanged; no packages, global save architecture, later systems or subagents.
- Evidence: clean main at `27201c7c63ae979f6c16b05f002a6358db69528c`, matching origin/main; baseline 17/17 Unity tests passed. New storage tests and actual Play Mode A/B, reload/re-entry and visual comparison required.
- Budget: maximum three reasonable implementation/observation rounds; no numerical quota budget supplied.
- Stop: unexpected dirty work, architectural expansion, prior-spike regression or a persistent blocker after 2–3 approaches.
- Done: verified factual persistence and visible aftermath, explicit clear and final clean memory, regression, findings and scoped publication. Human judgment remains pending.

## Design and storage

Persistence01 derives a new scene from Triage01, preserving its accepted movement, power and two crises. The experiment observes actual Rescue/Containment states and lost ground. It saves a complete pair after one second without outcome changes, then freezes that completed run so later actions cannot contradict the recorded result. It does not save each frame, choose a priority or store physics/transient state.

The small version-1 JSON stores `rescue` (Protected/Hit), `depot` (Contained/Overrun), and `lostGround` (0..3). Unknown/incomplete values cannot be saved. A contained threat can still leave lost ground; that cost remains factual. The path is `Application.persistentDataPath/Persistence01/outcome.json`. A closed temporary file is moved/replaced into the final path. This is EXPERIMENTAL / LOCAL TO PERSISTENCE01, not the final Nexus save architecture.

On re-entry, existing valid memory disables the crisis and reconstructs aftermath. Protected rescue: civil present, cargo safely off the lane. Hit rescue: civil absent, tipped cargo, debris and physical cordon. Each lost depot strip reconstructs displaced cargo and a broken barrier. The previous threat is absent on return. These are fixed factual reconstruction layouts, not a snapshot of every Rigidbody.

Scene reset preserves memory and reconstructs aftermath once an outcome exists. **Nexus → Persistence 01 → Clear experiment memory** deletes only the outcome and its temporary file. In Play Mode it reloads the experiment; outside Play it prepares the next run. Pause also exposes reload and explicit clear buttons. Read errors, empty/invalid/unknown records start clean with one controlled warning; original data remains until a new complete outcome is saved or explicitly cleared. Failed writes report failure and allow a paused retry.

## Validation

Storage tests use temporary paths. Play Mode tests inject a process-local temporary path via `NEXUS_PERSISTENCE01_TEST_PATH`, restored afterward and ignored by player builds. Tests do not touch the user's runtime record. The installed skills for Unity implementation/validation guided isolated storage, lifecycle checks and explicit evidence rather than inferring success from compilation.

Initial three bounded runs, 2026-09-09 (before the separately authorized closure round):

1. `artifacts/persistence01/round1.xml`: 3/5 passed. Storage round-trip/replacement/clear, invalid data and failed publication preservation passed. The Play Mode helper nested EnterPlayMode inside another coroutine; the runner did not perform that transition. Corrected by yielding lifecycle instructions directly from the Unity tests.
2. `artifacts/persistence01/round2.xml`: 3/5 passed. Actual entry occurred, but NUnit reran SetUp after domain reload and generated a different temporary path. Corrected test-path ownership using Editor SessionState. Moved the controlled invalid-file warning check to a scene reload within Play Mode.
3. `artifacts/persistence01/final.xml`: **21/22 passed**, exit 2. All 17 accepted-spike regression tests passed, all three storage tests passed, and invalid-memory scene startup passed. The remaining A/B matrix test failed because its local `run` loop value was lost across domain reload. It followed the B branch with an empty label and subsequently asserted the wrong expected rescue-damage flag. No fourth round was started, respecting the mission limit.

Partial factual evidence from the third run: actual movement/RT and physics generated `version=1, rescue=Hit, depot=Contained, lostGround=1` at approximately 6.64 s. The JSON remained unchanged while the completed scene continued. Scene reload loaded those same facts, entered aftermath and disabled the crisis; rescue-damage objects were active, consistent with Hit. This does not complete the A/B matrix or count as visual inspection.

At the initial stop, the correctly labelled A run, full matrix, Play Mode re-entry and visual comparison remained pending. No aftermath screenshots were produced before that assertion. These gaps were completed in the authorized closure round below.

The identified harness issue required retaining scenario identity and the pre-exit record across domain reload. The user explicitly authorized one additional closure round, with at most two harness strategies and publication conditional on complete evidence.

Initial final log: no C# compiler errors/warnings or gameplay exceptions; one controlled invalid-memory warning, the known non-blocking license-token startup diagnostic and the failing test assertion remain recorded. Validation was PENDING at that stop and nothing was published then.

Final cleanup: the real runtime file `C:/Users/Usuario/AppData/LocalLow/Nexus/Nexus/Persistence01/outcome.json` and its `.tmp` were confirmed absent. The one orphan temporary invalid-data record from the earlier test setup was deleted with its now-empty dedicated folder. Test evidence remains in ignored `artifacts/persistence01/` and `client/Nexus.Unity/Logs/Persistence01/`. No URP diff appeared and no .NET files were changed; .NET tests were not run.

## Initial working-tree handoff

Initial main/origin SHA remains `27201c7c63ae979f6c16b05f002a6358db69528c`. Intentional uncommitted work: new Persistence01 scene, runtime state/store/coordinator, editor authoring/menu, tests/assembly definitions/metadata, this findings document, and an appended EditorBuildSettings entry. Prior spike code/scenes and packages are unchanged. Runtime memory is clean; the working tree intentionally retains this incomplete implementation for follow-up. Approximately 17 minutes including baseline and bounded validation.

## Authorized closure — technical PASS

The first closure strategy succeeded. The test loop cursor and saved JSON checkpoint now live in the existing Editor-only SessionState scope, alongside its temporary path. The coroutine no longer relies on its local A/B string or JSON surviving domain reload. SessionState entries are removed during cleanup. Runtime code was not changed for this correction; domain reload remains enabled.

`artifacts/persistence01/closure.xml`: **22/22 passed, 0 failed, 0 skipped**, exit 0. The single full suite includes all 17 prior-spike tests, all three storage tests, invalid-record startup and the complete A/B matrix. No additional regression run is necessary for this test-only correction.

| Run | Played and saved facts | Stop / Start Play Mode | Reconstructed aftermath |
| --- | --- | --- | --- |
| A | Protected / Overrun / 3 lost sectors, at about 8.32 s | Full JSON unchanged after exit and after re-entry | Civil present, rescue clear, three depot damage groups |
| B, after explicit clear | Hit / Contained / 1 lost sector, at about 6.64 s | Full JSON unchanged after exit and after re-entry | Civil absent, accident cordon/debris, one depot damage group |

Both runs additionally passed scene reload and normal arena reset without altering the record; the explicit clear operation removed the record and reloaded a clean active experiment. No per-frame rewrite occurred after saving.

Four 1280×720 camera captures in `client/Nexus.Unity/Logs/Persistence01/` were generated and reviewed: `A-rescue-no-labels.png`, `B-rescue-no-labels.png`, `A-depot-no-labels.png`, `B-depot-no-labels.png`. Text labels were disabled for capture. Rescue A visibly retains the civil and an open lane; rescue B has a cordon and scattered debris with no civil. Depot A has three displaced-cargo/broken-barrier groups; B retains two clear sectors and one damaged group. The paired viewpoints distinguish both outcomes without labels. No floating trace geometry or broken materials was observed; tests also check ground bounds.

Closure Console/log: no C# compiler errors/warnings or gameplay exceptions; the expected invalid-record warning and known non-blocking licensing diagnostic remain visible. The full suite exited 0. Final checks found no real runtime `outcome.json`, no `.tmp`, and no Persistence01 temporary test directories. Evidence logs/captures remain locally ignored. Prior spike code/assets, .NET and packages are unchanged; no URP restoration was needed.

Publication uses `feat: add persistent consequence prototype` after scoped diff/staging review. The final response records the resulting SHA and verified remote state. Experiential validation remains PENDING HUMAN PLAYTEST.

## Human flow and limits

Clear memory; open **Nexus → Persistence 01 → Open gameplay scene**; Play; prioritize rescue; wait for MEMORY SAVED; stop/start Play or pause and Return later; explore both areas. Clear memory explicitly and repeat prioritizing containment.

Controls remain WASD/mouse, Space jump, Shift sprint, left click power, Esc pause. Ask: do you recall your priority; recognize the trace without labels; feel that the place retains history; value the earlier decision more; want to compare the other result; imagine larger consequences? Does the world appear to remember what you did?

Inherited debts: basic targeting/camera, provisional art/sound/animation/VFX, physical controller and Windows player untested. No claim of meaningful memory before Ramiro's playtest; no advance to Vertical Slice.

## Closure decision — human validation and handoff

Ramiro's bounded human playtest confirmed the prototype outcome: **PASS FOR PROTOTYPE**. The technical evidence above is also a **TECHNICAL PASS**. Persistence01 is therefore **ACCEPTED TO CONTINUE**, with the explicit decision to advance to the first vertical slice while keeping this implementation local and experimental.

The validated experiential chain is: **Power → Reaction → Choice → Consequence → Memory**. The player uses power in a readable urban incident; the city reacts immediately; the player chooses which need to prioritize; the incompatible outcome changes the scene; and returning later makes that choice legible through persistent aftermath.

Open debts are intentionally carried forward: improve the physical controller and camera, replace provisional art/sound/animation/VFX, test a Windows player build, and validate the broader slice with more than one human. Persistence01 does not become a general save system, and no later simulation or gameplay system is implied by this acceptance.
