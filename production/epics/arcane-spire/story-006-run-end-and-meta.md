# Story 006: Run end and permanent upgrades

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 6`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] When tower HP reaches 0 the run ends and a summary shows the wave reached and currency earned.
- [ ] Run currency is earned from waves and bosses and kept after the run ends.
- [ ] A simple permanent skill tree (at least 5 nodes) is bought with that currency.
- [ ] Permanent upgrades are applied at the start of every new run.
- [ ] A new run resets gold, shop levels and wave number to the start, keeping permanent upgrades and the best-wave record.
- [ ] A new best wave is highlighted on the summary screen.
- [ ] Run summary and skill tree are reachable from the main flow without a restart.

---

## Implementation Notes

- Run state and meta state are separate objects, so save/load can handle them independently.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 007: save/load of meta progress

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Integration
**Required evidence**: `Assets/Tests/PlayMode/run-end-and-meta_test.cs` OR playtest doc (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 002, 003
- Unlocks: 007
