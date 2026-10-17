# Story 002: Endless waves and bosses

> **Epic**: arcane-spire
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: fill before starting
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: set by /dev-story when implementation begins

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 2`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.22f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (the Player goal & fail state field + the MVP feature this story implements), scoped to this story:*

- [ ] Waves spawn one after another with no end; the current wave number is shown.
- [ ] Enemy HP and damage scale per wave by a formula whose constants live in config.
- [ ] Enemy count per wave grows by a configured rule, capped to protect performance.
- [ ] Every N-th wave (N in config) spawns a boss with a distinct HP multiplier and look.
- [ ] The next wave starts after the current one is cleared, after a short configured delay.
- [ ] The wave counter and best-wave record are tracked in a plain C# model class, separate from MonoBehaviours.
- [ ] Wave number to enemy stats is deterministic: the same wave always yields the same stats.

---

## Implementation Notes

- Keep the scaling formula in one class so balance can be tuned and unit-tested.
- Gameplay values are data-driven (config / ScriptableObject), never hardcoded.
- Code root is `Assets/`; check `docs/engine-reference/unity/` before using any Unity API.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: gold drops
- Story 006: what happens when the tower falls

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

*Governed by `qa.level`: at `qa.level: minimal` tests are **waived** (advisory, never "must exist and pass"), but a Visual/Feel or UI story's retained screenshot is not.*

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/waves-and-bosses_test.cs` (waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001
- Unlocks: 003, 006
