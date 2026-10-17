# Story 005: Runes

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 5`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] Three runes exist, each with a passive modifier (for example +damage %, +gold %, -cooldown %).
- [ ] Runes are unlocked with gold or run currency and shown in a rune panel.
- [ ] The player has a limited number of rune slots and can equip or unequip freely.
- [ ] Equipped rune modifiers apply to tower and spell stats immediately.
- [ ] Modifier values stack additively within a rune type and multiplicatively across types, as set in config.
- [ ] Unequipping removes the effect exactly: stats return to their previous values.
- [ ] Equipped runes persist across waves within a run.

---

## Implementation Notes

- Route all stat changes through one stat-modifier layer shared with shop upgrades and spells.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: permanent progression
- Story 007: saving equipped runes

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/runes_test.cs` (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003
- Unlocks: None
