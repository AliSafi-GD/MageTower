# Story 003: Gold and upgrade shop

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 3`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] Killing an enemy awards gold; bosses award a configured multiple.
- [ ] The shop offers upgrades for damage, attack speed, tower HP and crit chance.
- [ ] Each upgrade has a level; its cost rises per level by a configured curve.
- [ ] Buying deducts gold, raises the level and applies the effect immediately.
- [ ] A button is disabled when gold is insufficient; keyboard shortcuts buy the first four upgrades.
- [ ] Gold and levels are held in a plain C# model, independent of UI.
- [ ] Large numbers are shown in a short format (1.2K, 3.4M).
- [ ] A retained screenshot of the shop UI is saved in production/qa/evidence/.

---

## Implementation Notes

- Economy curve is the riskiest part: keep every constant in config and expose cost/effect formulas as pure functions.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: spells
- Story 005: runes

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/gold-and-shop_test.cs` (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002
- Unlocks: 004, 005, 006, 007
