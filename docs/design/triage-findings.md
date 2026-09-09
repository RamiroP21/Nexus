# Triage Spike 01 — implementation findings

TECHNICAL VALIDATION: PASS

HUMAN EXPERIENTIAL VALIDATION: PASS FOR PROTOTYPE
STATUS: ACCEPTED TO CONTINUE

## Mission and boundaries

- Objective: two simultaneous, physically progressing crises; priorities emerge through movement and Kinetic Vector.
- Constraints: independent Triage01 scene; reuse accepted Feel/Reactivity runtime; no .NET, persistence, city systems, packages, general quest framework or subagents.
- Initial evidence: clean `main` at `60ecb97d4ce3399cb63df2f06098c18f421ceb19`, same origin/main; 13/13 existing Unity tests passed.
- Budget: initial implementation/play/correction, second play/correction, final regression only.
- Stop conditions: unexpected dirty work, architectural expansion, broken accepted experiments or persistent blocker after bounded attempts.
- Done: useful technical tests, six observed Play Mode strategies, regression and console review, findings, scoped commit/push with verified remote. Human significance remains unclaimed.
- The user explicitly authorized synthetic input in actual Play Mode, physics and rendered captures for the agent matrix. It is not manual human playtesting.

## Local design

Scene: `Assets/Nexus/Experimental/Triage01.unity`; menu **Nexus → Triage 01 → Open gameplay scene**.

A / rescue: a 12 kg runaway cargo starts at (-32, 0.8, -10), sliding at 4 m/s toward a civil at (-32, 1.01, 6). Actual contact marks the civil struck and triggers the existing flinch/flee reaction. The lane turns red and the sign retains the consequence. Diverting the cargo laterally or moving the civil can clear the trajectory. No timer chooses failure.

B / containment: the existing 8 kg threat advances at 1.5 m/s from (32, 1.01, -4) toward the depot at z=12, not toward the player. Crossing z=0, 3 and 6 within the visible strips' width takes three ground strips, which remain red even after late containment. Displacing the threat into the adjacent 3 m deep service trench (x=34..42, z=-9..9) physically contains it; its existing AI remains enabled. A kinetic hit still causes the accepted 1.1 s recovery before movement resumes.

Both start together. Bays are 64 m apart in one open 84 × 30 m courtyard. The player starts at (0, 0.05, -6). There are no gated routes, choice UI or changed power parameters. The trench is a physical containment affordance, not an invisible trigger that disables the enemy.

Kinetic Vector retains impulse 36, range 24 m and cooldown 0.65 s. It diverts the cargo, displaces/startles the civil, displaces the threat, or moves a 2 kg barrier. Moving the barrier to obstruct the threat is a possible physical experiment, not a scripted secret solution or promised success.

States are scene-local: Active, Escalating, Resolved, Consequence. Rescue clearance is re-evaluated while no collision has occurred; pushing danger back into the lane can reopen it. Ground already lost is not erased by containment. Reset uses the existing arena body snapshots and reaction reset, plus local cargo velocity, territory colors and crisis state/time reset. An explicit `KineticPulse.ResetPulse()` clears cooldown, beam/audio and shot feedback for Triage; the existing scenes do not opt in and are otherwise unchanged.

## Evidence and tuning

Three bounded rounds:

1. Initial Play Mode: 3/4 Triage tests passed. Autonomous consequences, diverted rescue and six-strategy execution worked. Containment failed: floor friction stopped the enemy before the trench at x=36. A walking rescue attempt also arrived too late. Corrected the trench edge to x=34, without modifying power/AI parameters; explicitly reset power feedback/cooldown; changed the A-first route to sprint to the urgent rescue, then walk to B.
2. Second Play Mode: 4/4 passed. Both physical interventions worked. Synthetic strategies produced four distinct outcome combinations. Rendered captures inspected for the start, rescue lane and depot intervention. Aligned territory detection width with the rendered strips before final regression.
3. Final regression: **17/17 Unity tests passed**, exit 0. This includes the 13 baseline tests (foundation, GameplayFeel01 and Reactivity01) and all four Triage tests. Final strategy outcomes and physical windows matched round 2. No further implementation round.

Measured unopposed windows in round 2: cargo contact at 3.78 s; depot ground crossings at 3.08 / 5.22 / 7.34 s. These are observations with frame-tolerant sampling, not countdown logic or deterministic Unity contracts. Walk speed 6 m/s, sprint 10 m/s; the A-first route reached its first impulse at 2.58 s and, after attending A and walking across, reached B at 11.02 s. B had already crossed every strip. This is the measured cost of attending one area, not a scripted failure of the other.

### Six-strategy matrix

Actual Unity Play Mode with synthetic gamepad movement and RT, camera API aiming, Rigidbody simulation and collision callbacks. No player teleportation in this matrix. The separate technical intervention fixtures position the player beside the tested crisis, so they do not claim travel feasibility. Shots record the body actually hit, including cover and misses. No automated verdict on meaningful choice.

| Strategy | Executed approach | Observed rescue | Observed containment / cost |
| --- | --- | --- | --- |
| A first | Sprint A, divert cargo, attend 1 s, walk B, two pulses | Civil unstruck; path clear | Depot overrun, 3/3 strips; late pulses displace threat beyond trench's north end |
| B first | Sprint B, two pulses, sprint A | Civil struck before return | Threat physically contained; 1/3 strip lost |
| Alternate | Sprint A, walk B, return A | Civil unstruck | 3/3 strips lost; final cargo attempt misses after it leaves range |
| Ignore both | Remain at spawn | Civil struck and displaced/flees | Threat reaches depot; 3/3 strips lost |
| Try both | Sprint A then sprint B, two threat pulses | Civil unstruck | Threat contained, but 3/3 strips remain lost |
| Creative barrier | Sprint B, push movable barrier, sprint A | Civil struck before return | This barrier placement did not contain/delay sufficiently; 3/3 strips lost |

The B-first route's first aimed threat shot actually hit the movable barrier; the second hit the threat. The recorded outcome includes that physical chain, rather than claiming two direct hits. No tested route was a perfect zero-cost rescue plus containment. The matrix does not prove that an extraordinarily efficient/recoil-assisted route cannot exist, nor does code force one crisis to fail when the other resolves. That boundary remains an important human test.

The creative attempt is a negative finding, not an advertised successful solution. Other angles, shifting the civil, repeated displacement or recoil-assisted travel remain possibilities to test, not validated exploits.

### Technical evidence

- Four Triage tests: scene/references/two actors/autonomous motion/contact/territory/reset; physical rescue while B continues; physical containment while A continues and trench reset; six-strategy matrix with finite-body checks.
- Reset additionally tests an immediate fresh impulse while the previous cooldown would still be active.
- XML/log evidence: ignored local `artifacts/triage01/{baseline,round1,round2,final}.*`; rendered captures and per-strategy observations: ignored local `client/Nexus.Unity/Logs/Triage01/`.
- Images are camera render requests, not a capture of the Editor Console or IMGUI overlay. Scene visibility was inspected; interactive desktop operation was unavailable and was replaced only with the user's approved method.
- .NET remains outside scope; its 211 tests are intentionally not run. No packages, ECS, server, saved state or production art added.
- Console/log review: no C# compiler errors/warnings, runtime exceptions or unexpected test logs. Unity emitted a non-blocking licensing-service `Access token is unavailable; failed to update` startup diagnostic; execution continued and the test runner exited 0. This is not reported as a gameplay/runtime failure or hidden as a completely empty log.
- Final visual inspection confirmed the rescue sign/lane/civil and the distinction between clean containment and containment with red lost-ground strips. At spawn the crises are off to the sides, indicated by a central spatial sign; whether they are noticed fast enough remains a human legibility question. The contained actor can be hidden below the trench lip while its sign communicates the result.
- No URPProjectSettings diff appeared in the final worktree; no restoration was needed. EditorBuildSettings only appends Triage and preserves existing scene entries/startup order. Library, Logs, artifacts, bin and obj are not staged.

## Changed surface and publication

Main files: Triage scene and metadata; `Triage01/Runtime/TriageDirector.cs` and `CargoContact.cs`; `Triage01/Editor/TriageSceneAuthoring.cs`; local reaction/contact assets; `Triage01/Tests/Editor/TriagePlaytests.cs`; isolated assembly definitions. Existing changes are the opt-in eight-line power reset method and the appended build-scene entry, plus this findings document.

Publication is scoped to this mission using `feat: add triage gameplay prototype`. The final response records the resulting SHA and verified remote state. Implementation and validation took approximately 20 minutes before publication; no Persistence work.

## Human playtest

Playtest humano confirmado por Ramiro después de la validación técnica:

- Ambas crisis se perciben suficientemente para probar el concepto.
- Atender una consume tiempo mientras la otra progresa.
- Las consecuencias de distintas prioridades son comprensibles.
- El escenario hace evidente que ser poderoso no implica resolver todo perfectamente.
- El jugador puede comparar prioridades y considerar otras estrategias.
- Para este nivel de greybox, alcanza para continuar.

Decisión formal: Triage 01 queda aceptado como prototipo experimental. No es production-ready y no se inicia Triage V2.

En producción, la presión debe volverse mucho más diegética: personas, sonido, movimiento, VFX, entorno y comportamiento deben comunicar las crisis sin depender de labels grandes.

Deudas no bloqueantes: legibilidad dependiente de señalización explícita/textos grandes; cámara/oclusiones; targeting básico; arte, sonido, animaciones y VFX provisionales; mando físico no probado; Windows player no probado.

WASD / left stick move; mouse / right stick look; Space / South jump; Shift / L3 sprint; left mouse / RT Kinetic Vector; Esc / Start pause; jump while paused resets.

Questions: Did both problems register quickly? Did movement time create pressure? Was the neglected consequence understandable without debug numbers? Did priority change the experience? Did an improvised intervention buy useful time? Is there a trivial perfect sequence? Would you replay with the other priority?

Known inherited debts: basic targeting and camera occlusion. No advanced camera or combat changes. No conclusion about meaningful choice, fun or emotional responsibility; no advance to Persistence.
