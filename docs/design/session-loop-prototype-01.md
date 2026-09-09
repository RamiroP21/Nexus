# Session Loop Prototype 01

## Status and central question

This is a design brief only. It does not authorize implementation in this document.

Central question: can the systemic identity validated by Vertical Slice 01 sustain a continuous play session in which exploring, noticing situations, intervening, accepting consequences and continuing remains interesting?

The target is an initial human session of approximately 30–45 minutes maximum. A shorter version is preferable if it answers the question. This is not a campaign, a 60–90 minute content target or a production game loop.

## Loop

**EXPLORE → NOTICE → PRIORITIZE → ACT → WORLD REACTS → CONSEQUENCE → CONTINUE → RETURN / DISCOVER CHANGE**

The player should move through the world and discover situations through proximity, spatial signals, movement, sound and changing conditions. A mission menu, chore list or linear event checklist must not be the primary structure. After an immediate outcome, the player needs an intrinsic reason to keep moving: another visible change, a new pressure, an unresolved situation, a useful route or a place worth revisiting.

## World and continuity

The prototype is a small connected area larger than Vertical Slice 01, with two or three recognizable urban microzones. A suitable shape is a commercial plaza/street, a residential block and a small industrial/service edge, or an equivalent composition. This is a compact place, not a city and not a procedural generator.

The player moves freely between zones without resetting after each incident. Earlier outcomes remain part of the same session. A zone changed by situation A must still be changed when the player leaves, handles situation B and returns. Cross-session persistence may reuse the validated minimal outcome pattern if it helps observation, but the main question is within-session continuity; no production SaveSystem is designed here.

## Situations

Start with only two or three systemic situations. They should vary their combination of civilians, threats, objects, space, pressure, Kinetic Vector and consequence rather than repeat Triage three times. Examples of useful variation include:

- a moving physical hazard through a public route, where rescue and rerouting compete;
- a service or residential disruption where objects and access matter more than direct threat containment;
- a situation discovered while another is already progressing, so returning late reveals a changed outcome.

At least one portion of the prototype must allow two situations to overlap in time. The player may discover one while the other progresses, abandon one, change priority or return too late. The experiment should begin to test the feeling that a living city cannot have everything solved by one actor. Not every moment should be a crisis: exploration, observation, calm, small movements and returns are part of the loop.

## Minimal director and spontaneity

An explicit local `SessionLoopDirector`-style mechanism may activate a small authored agenda, but it must remain **EXPERIMENTAL**. It is not a production event framework, quest director or city scheduler. Proximity, elapsed time, world state and player movement should vary when situations become discoverable. Avoid presenting the experience as Event 1 → Event 2 → Event 3 even if a small authored sequence exists underneath.

## Kinetic Vector and civilians

Kinetic Vector remains the only power. Across the session it must support traversal and repositioning, physical manipulation, rescue, containment and improvisation. No second power, special rescue button or special containment action is introduced.

Civilians are perceptual instruments as well as actors: they show danger, react, flee, occupy space and make consequences visible. They do not need relationships or deep individual memory. Their behavior should help the player notice that something is happening without depending on giant labels.

## Legibility and diegesis

The Vertical Slice debt is explicit: important objects must not be semantically empty cubes whose meaning comes only from orange or another color. Greybox forms should improve gradually through shape, scale, placement, movement, sound, reaction and simple material/VFX cues. A load should resemble a load, a barrier a barrier and a damaged zone a damaged zone. Debug views may exist for validation, but the player should infer the basic situation from the world.

Continue reducing laboratory labels, mandatory debug text and giant floating instructions. Minimal framing may communicate place, people and an observable cause; this experiment is about game loop, not a complete narrative, dialogue system or branching story.

## Rhythm and memory

Aim for an alternation of **calm → signal → pressure → consequence → breathing**. Do not maintain constant adrenaline or fill time with waits. Consequences should persist during the session and remain visible when the player returns. The prototype does not simulate days, a global clock or a whole city.

## Human evaluation

The future playtest should record both what the player says and what they do. The highest-value observation is whether Ramiro keeps moving and exploring after the immediate objective appears finished.

Questions:

1. Did you continue after the first incident?
2. Did exploration naturally lead you to new situations?
3. Did things appear to happen while you were elsewhere?
4. Did you have to decide where to spend your time?
5. Did you remember an earlier consequence while handling something new?
6. Did you voluntarily return to a zone?
7. Did Kinetic Vector offer different uses across the session?
8. Did you try to improvise?
9. Was there enough breathing room between crises?
10. Did the world begin to feel connected?
11. Did you want to know what would happen next?
12. Could you imagine playing for an hour if real content existed?

Decisive question: after resolving a situation, does the player have an intrinsic reason to keep moving through Nexus?

## Limits and stop conditions

Out of scope: a complete city, procedural generation, traffic, crowds, final quest or event frameworks, campaign, long dialogue, factions, reputation, economy, police, Nemesis, relationships, a second power, progression, inventory, loot, production combat, production save, Nexus.Server, ECS integration, Cognitive Society, LLM NPCs, production art and Blender pipeline.

Stop if the experiment requires building a city, a general quest framework, multiple powers, extensive narrative, production persistence or a content factory. Do not optimize scale before the loop is demonstrated. If the prototype passes, this brief does not select the next production-oriented system; that decision follows the evidence.

## Definition of done for a future implementation mission

One small connected area, two or three authored systemic situations, at least one temporal overlap, visible in-session consequences, meaningful calm between incidents, Kinetic Vector used in several contexts, an experimental local director, a clear/repeatable reset path and a documented human playtest. No claim of production readiness or campaign viability follows from that result.
