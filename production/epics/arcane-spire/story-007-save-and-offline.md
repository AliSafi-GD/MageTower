# Story 007: Save/load and offline progress

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 7`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] Meta progress (currency, permanent upgrades, equipped runes, best wave) is saved automatically and on quit.
- [ ] Loading restores the saved state exactly; a missing or corrupt save falls back to a fresh start without crashing.
- [ ] The save records a timestamp of the last session.
- [ ] On return, offline progress is computed from elapsed time, capped at a configured maximum (for example 8 hours).
- [ ] Offline gains use a simplified estimate from the best-wave power curve, not a full simulation.
- [ ] A welcome-back popup shows what was earned while away.
- [ ] Saving works in a WebGL build (PlayerPrefs or an equivalent browser-safe store).

---

## Implementation Notes

- WebGL cannot rely on normal file I/O: keep the storage behind a small interface.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Out of scope for the whole epic: cloud saves, leaderboards

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**: `Assets/Tests/PlayMode/save-and-offline_test.cs` OR playtest doc (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 003, 006
- Unlocks: None
