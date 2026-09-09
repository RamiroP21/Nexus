# Vertical Slice 01 — Urban Consequence

## Purpose

Vertical Slice 01 is a small, playable 10–15 minute urban greybox that turns the accepted Persistence01 experiment into one coherent experience. It is the next bounded experiment after Feel, Reactivity, Triage and Persistence: one district, one player power, one incident, two incompatible needs, and a return visit that makes the choice matter.

## Player and systemic promise

The player reuses the existing movement, camera, targeting and **Kinetic Vector** power. Kinetic Vector must be useful for traversal, rescue, containment and manipulating the environment—not merely a combat button. The slice is successful when the player understands that their power changes both the incident and the city's later state.

## Greybox contents

- A compact district with a street, depot, civilian route, service lane and a return landmark.
- Civilians, movable objects and one readable threat that can damage or block routes.
- One incident with a short setup, an active response window and a quiet aftermath.
- Diegetic cues only: radio/phone fragments, signage, spatial sound, damage, displaced props and civilian presence or absence. No debug labels are required for comprehension.

The incident presents two needs that cannot both be fully satisfied in the response window: **protect the exposed civilian** or **contain the threat before it overruns the depot**. Kinetic Vector can create a safer rescue, a stronger containment, or a costly compromise, but the slice must preserve a factual difference between the two priorities.

## Experience structure

1. **Read:** arrive, identify the civilian, depot and threat, and understand the immediate pressure.
2. **Act:** use Kinetic Vector and the environment to prioritize rescue or containment.
3. **React:** show immediate visible consequences—route access, civilian state, debris, barriers, threat absence and ambient response.
4. **Return:** leave and re-enter the district. Reconstruct the chosen aftermath and a small memory cue that points back to the decision.
5. **Reflect:** let the player compare the two possible outcomes on a later run without requiring a separate menu or exposition dump.

## Scope and stop criteria

In scope: one greybox district, one incident, one player power, two authored outcome variants, one persisted aftermath, one concise diegetic framing pass and human playtest evidence. Out of scope: procedural city simulation, factions, inventory, quests, combat progression, general save architecture, multiplayer, new ECS phases and Unity production polish.

Stop and report rather than expand if the two needs become simultaneously optimal, if the outcome cannot be recognized without debug text, if re-entry does not differ from the first visit, if persistence becomes frame/state serialization, or if the slice requires a new architectural contract.

## Human acceptance criteria

At least two people should be able to complete the slice without developer instruction and answer: What did you prioritize? What changed immediately? What remained different when you returned? Did the city appear to remember? Did the choice feel costly enough to replay? Evidence must include observed playthrough notes and paired before/after captures or equivalent recorded comparison.

## Layered implementation strategy

V1 layers the existing work rather than replacing it: reuse accepted Feel movement and power, reuse Reactivity's readable incident response, reuse Triage's competing priorities, reuse Persistence01's bounded outcome record and reconstruction, then add only the connective greybox layout, diegetic framing and the return loop needed to judge the whole experience. Any layer that fails its acceptance criteria is corrected or removed before adding presentation polish.
