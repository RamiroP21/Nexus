# Unity / Simulation Authority Boundary

Milestone 05 establishes a local, single-player semantic authority for District01. The Unity scene remains responsible for input, camera, CharacterMotor, Rigidbody physics, targeting, Vector/Grasp presentation, hostile movement, collisions and VFX. The .NET Simulation Host owns the district session identity, seed, semantic tick, district phase, crisis outcomes, infrastructure, route state, civilian persistent state, downstream consequence and deterministic hash.

The contract is deliberately narrow:

| Concern | Owner |
| --- | --- |
| Physical observation and player input | Unity |
| Semantic validation and transitions | Simulation Host |
| High-frequency transforms/velocities/collisions | Unity only |
| Persistent district outcome and timing | Simulation Host |
| Semantic presentation reconciliation | Unity replica |

The flow is `Unity observes → ClientCommand intent → Host validates → ServerEvent/Snapshot → Unity presents`. Unity never commits a semantic outcome directly and does not run a second local semantic authority. If transport is unavailable, commands fail closed and the development diagnostic reports the failure.

## IDs, ticks and state

District01 uses authored stable IDs (`district01`, `district01.zone.a`, `.b`, `.c`, `district01.crisis.1`, `.2`). They are independent of GameObject instance IDs, addresses and scene paths. The Host advances `SimulationTick`; warning, aftermath, crisis deadlines and bounded off-screen evolution are tick-derived. Wall-clock, process and Unity transform data are excluded from the state hash.

## Protocol and transport

Protocol version is `Nexus.DistrictAuthority.v1`. Messages are compact JSON envelopes framed by a four-byte big-endian payload length and capped at 128 KiB. The TCP listener binds to `IPAddress.Loopback`; this is local IPC, not multiplayer. Flows include `ClientHello/ServerHello`, `CreateSession/SessionStarted`, `SnapshotRequest/Snapshot`, `ClientCommand/ServerEvent`, `CommandRejected/ProtocolError` and `Ping/Pong`.

Commands carry session, client, client sequence, command ID, command type, stable entity and optional target tick. The Host rejects stale sessions, unknown IDs, malformed payloads and illegal phase transitions. Command IDs are remembered for the session: retries return the original result and do not repeat a transition or event. Snapshots are authoritative read models for initial connection and resync; normal progression uses events.

The Unity `DistrictAuthorityReplica` replaces semantic state from snapshots and accepts events only in increasing server-sequence order. Socket work is off-thread; JSON decoding and Unity presentation are drained on the main thread through bounded queues. No per-frame snapshots or physics replication are used. Disconnects stop semantic progression rather than falling back locally; reconnect requests a snapshot.

## Replay and development flow

`DistrictReplayDocument` records protocol version, seed and the ordered accepted command stream with tick targets. `Nexus.Host --replay <file>` reinitializes the same seed, feeds those inputs, and reports final tick and canonical district hash. The District01 development authoring adds a bootstrap component that starts only the exact Release Host executable it owns on loopback and closes that child on destruction. A future standalone can place the Host beside the player executable or use the serialized override; installer/distribution is intentionally out of scope.

F8 sends an authoritative reset and then restores Unity physical fixtures. F9 remains opt-in and shows connection/session/seed/tick/sequence, phase, outcomes, route, infrastructure, hash and rejection diagnostics. With F9 disabled, no protocol HUD is shown.

## Future layers (documented, not implemented)

Layer 1 (this milestone) is semantic consequences and district state. Future layers may add semantic combat resolution, NPC/faction/world simulation, and campaign/save authority. Rigidbody simulation and local presentation do not need to move wholesale to the Host.
