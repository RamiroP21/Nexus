# Campaign persistence contract

The Simulation Host is the sole owner of durable campaign semantics. Unity owns presentation and physical reconstruction only.

| State or operation | Authoritative owner | Persisted? |
| --- | --- | --- |
| CampaignId, seed, semantic tick and phase-entered tick | Host | Yes |
| Crisis outcomes, route, infrastructure, civilian zone state and deadlines | Host | Yes |
| Stable `WorldMemory` ledger and command receipts | Host | Yes |
| SessionId, server sequence, socket, process and save status/revision | Host transport metadata | No (except revision envelope metadata) |
| Camera, Rigidbody state, particles, VFX and Unity instance IDs | Unity | No |

The schema is `Nexus.CampaignSave.v1`; it is intentionally independent of the wire protocol `Nexus.DistrictAuthority.v1`. The development slot is `%LOCALAPPDATA%\Nexus\Saves\development\campaign.json` with `campaign.json.bak`. Writes serialize semantic state to a bounded temporary file, flush it durably, validate it, and atomically replace the primary while retaining one backup. Loading validates format, state invariants, SHA-256 payload checksum and canonical semantic hash. A valid backup repairs a corrupt primary; if both copies fail, startup returns a persistence error and refuses silent reset.

The semantic hash excludes campaign path, save revision/status, wall-clock time, process identity, session identity and transport sequence. It includes all future-relevant timers, stable zone state, memory sequence/records and durable command receipts. Therefore a resumed campaign has a new session identity but the same hash at the checkpoint and the same final hash as uninterrupted continuation for the same accepted input suffix.

Autosave occurs after accepted semantic transitions, meaningful Host evolution and explicit save/clean shutdown. The tick loop is active only for a connected client; disconnect freezes the saved tick, so there is no offline wall-clock progression. Duplicate command IDs are restored as receipts and cannot apply a transition or memory twice.

Unity receives campaign identity, save format, revision/status, memories and hash through authoritative snapshots. It never reads save files or advances semantic state locally. Socket/background threads enqueue bounded payloads; JSON decoding and all Unity API calls occur on the main thread.
