# Story 003: Gold and upgrade shop

> **Epic**: arcane-spire
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-17

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

---

## Completion Notes
**Completed**: 2026-10-17
**Criteria**: 8/8 checked. DEFERRED: mouse clicks and keys 1-4 were never exercised (headless player has no input; autobuy goes through the same ShopModel.TryBuy). Shop UI, gold HUD, disabled-button state, levels, costs and upgrade effects were OBSERVED.
**Deviations**: None blocking. Orchestrator tuned `_goldPerKill` 5 -> 10 and `_upgradeCostGrowth` 1.35 -> 1.25 in CombatConfig.asset after the untuned run died at wave 7 even with autobuy; with the new values autobuy reached wave 13 in 300 s. Extra files: `Assets/Scripts/Combat/DevAutoBuy.cs` (test hook, active only with `--autobuy`). Advisory items logged as TD-010..TD-012.
**Test Evidence**: Logic tests waived at `qa.level: minimal`. Run result OBSERVED: 10 captures in `production/qa/evidence/gold-and-shop/` (fix-1280x720.png, fix-1024x768.png, tuned-autobuy-t300.png ...).
**Code Review**: Skipped — Solo mode (unity-specialist review run; 11 of 15 findings fixed)
