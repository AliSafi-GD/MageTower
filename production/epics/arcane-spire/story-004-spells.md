# Story 004: Auto-cast spells

> **Epic**: arcane-spire
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 4`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] Three spells exist: fireball (area damage), frost (slow), lightning (chain damage).
- [ ] Each spell starts locked and is unlocked with gold in the shop.
- [ ] Unlocked spells auto-cast when off cooldown and a valid target exists.
- [ ] Cooldown, damage and effect values come from config, one entry per spell.
- [ ] Frost slows enemies for a configured duration, then speed returns to normal.
- [ ] Lightning hits up to a configured number of enemies, never the same one twice per cast.
- [ ] Each spell has a distinct visual effect and a cooldown indicator in the HUD.

---

## Implementation Notes

- Use a shared Spell definition plus one effect class per spell, not three copies of the cast loop.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 005: rune modifiers
- Story 006: permanent upgrades

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/spells_test.cs` (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003
- Unlocks: None

---

## Completion Notes
**Completed**: 2026-10-17
**Criteria**: 7/7 checked. OBSERVED: all three spell effects in one frame (`all-three-spells.png`), locked/unlocked states, cooldown bars, slowed tint, chain between two enemies. Code-only: slow expiry restoring speed, lightning never hitting the same enemy twice (confirmed by unity-specialist review). DEFERRED: mouse clicks and keys 5-7 never exercised (headless has no input).
**Deviations**: None blocking. Orchestrator tuned `_fireballRadius` 2.5 -> 3.2 and `_lightningJumpRange` 3 -> 3.6 per review M1 (enemies ~3 units apart, spells mostly hit one target). Extra: `HeadlessCapture.cs` gained `--count/--interval` burst mode; `DevAutoBuy.cs` gained `--autobuy-spells-first`. Advisory items logged as TD-013..TD-016.
**Test Evidence**: Logic tests waived at `qa.level: minimal`. Run result OBSERVED: `production/qa/evidence/spells/` (note: the capture tool draws world sprites over the shop panel because it moves the overlay canvas to camera space; capture artifact only).
**Code Review**: Skipped — Solo mode (unity-specialist review run; first attempt hit an API rate limit, second completed; M1 applied, L1-L6 deferred)
