# Production Milestone 06 — Durable World Memory & Campaign Persistence

Status: technical validation PASS; experiential validation **PENDING HUMAN PLAYTEST**.

Milestone 06 extends the District01 authority with one durable development campaign. A campaign is distinct from a Host session: restarting the Host creates a new `SessionId` while restoring the same `CampaignId`, semantic tick, deterministic timers, outcomes and world-memory ledger.

## Delivered

- Versioned save envelope `Nexus.CampaignSave.v1`, separate from protocol `Nexus.DistrictAuthority.v1`.
- `CampaignState` contains seed, tick, phase timer, crisis/route/infrastructure/civilian state, stable zones, command receipts and structured `WorldMemory` records.
- Default Windows location is `%LOCALAPPDATA%\Nexus\Saves\development\campaign.json`; tests use isolated temporary directories. No save data is written into the repository.
- Atomic bounded writes use a flushed temporary file and replace, with one `.bak` backup. A corrupt primary is recovered from and repairs the primary; corrupt primary and backup fail closed without creating a new campaign.
- Save revisions and persistence status are metadata. They are excluded from the canonical semantic hash, which includes future timer state, receipts and memories.
- Meaningful accepted transitions, explicit save and clean shutdown persist state. The Host does not advance while disconnected and never performs wall-clock offline progression.
- Memory IDs are deterministic, low-frequency structured facts (`district01.memory.########`), with duplicate command receipts preventing duplicate transitions and memories.
- Unity resumes only through Host snapshots; F8 resets authoritative and physical development state, while F9 exposes campaign/session/format/revision/persistence/tick/sequence/hash and memory diagnostics.

## Validation record

Validation evidence includes the full Release .NET build and tests, full Unity regression, real-process create/mutate/save/shutdown/restart E2E, deterministic continuation against uninterrupted execution, duplicate-command idempotence, protocol mismatch, backup recovery and fail-closed corruption checks. Payload size and save latency were measured during the E2E run; autosave is transition-bounded rather than per tick or packet.

Technical validation is required to remain green for publication. Experiential validation is intentionally **PENDING HUMAN PLAYTEST**; Milestone 07 is not started by this mission.
