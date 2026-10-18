# Story 006: Run end and permanent upgrades

> **Epic**: arcane-spire
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-17

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

---

## Completion Notes
**Completed**: 2026-10-18
**Criteria**: 7/7 checked, all OBSERVED in window-free runs (`production/qa/evidence/run-end-and-meta/`): RUN OVER summary with NEW BEST highlight, 5-node levelled skill tree (Arcane Power Lv 1/5), run 2 reset to wave 1 with Best 5 and shop Lv 0, permanent damage bonus applied in run 2 (12 = 10 x 1.15), three consecutive runs in one process. DEFERRED: mouse clicks and keys T/N/K/Q/W/E/R/F never exercised (headless has no input).
**Deviations**: None blocking. Story work was begun by an earlier session (7 files in `Assets/Scripts/Combat/Meta/` + edits) and audited, fixed (enemies leaked across runs, tree not reachable mid-run) and finished here. Orchestrator tuned `_runCurrencyPerKill` 1 -> 4 (run 1 earned 11 vs cheapest node 40). Skill nodes made levelled (max 5, cost x1.6). Mid-run tree (K) is view-only by design; purchases only between runs. Extra files: `HeadlessCapture.cs` now routes inactive canvases; `DevAutoBuy` gained `--autobuy-no-shop`, `--autoruns N`, `--autoruns-hold-summary`, `--autoruns-show-tree`. Advisory items logged as TD-020..TD-022.
**Test Evidence**: Integration tests waived at `qa.level: minimal`. Run result OBSERVED: see evidence folder; earlier invalid summary captures kept under `stale/`.
**Code Review**: Skipped — Solo mode (unity-specialist review run; fixes 2-7 and 9 applied)
