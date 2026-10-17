# Story 001: Tower and mage auto-attack

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Visual/Feel
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 1`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] A tower with an HP value and a mage on top exist in the main scene (Assets/Scenes), side-view 2D.
- [ ] Enemies spawn off-screen and walk toward the tower; a placeholder enemy type is enough.
- [ ] The mage automatically targets the nearest living enemy within range, no player input needed.
- [ ] Attack speed, damage and range are data-driven (ScriptableObject or config), not hardcoded.
- [ ] An enemy reaching the tower deals damage to tower HP; HP is shown on screen.
- [ ] Each hit shows a floating damage number and a hit effect; a killed enemy is removed.
- [ ] With no targets the mage idles and resumes attacking when a new enemy enters range.
- [ ] A retained screenshot of the running scene (tower, mage, enemies, damage numbers) is saved in production/qa/evidence/.

---

## Implementation Notes

- Tune for feel first: hit numbers, short attack animation, readable enemy spacing.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: waves and bosses
- Story 003: gold and shop

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Visual/Feel
**Required evidence**: a retained screenshot in `production/qa/evidence/` + sign-off in `production/qa/evidence/tower-auto-attack-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: 002
