# Test Evidence: Tower and mage auto-attack

> **Story**: `production/epics/arcane-spire/story-001-tower-auto-attack.md`
> **Story Type**: Visual/Feel
> **Date**: 2026-10-17
> **Tester**: Claude (window-free player run, images opened and read); sign-off by the developer
> **Build / Commit**: Win64 player built from working tree on top of 58b9514 (uncommitted)

---

## What Was Tested

The Win64 player was launched with `-batchmode --headless-capture` (no window; `Assets/Scripts/HeadlessCapture.cs`) at 1280x720 and the camera was rendered to a PNG after a fixed number of seconds. Four captures at default config show the steady state; one capture with `_mageDamage` temporarily set to 1 (then restored to 10, player rebuilt) shows enemies reaching the tower.

**Acceptance criteria covered**: AC-1, AC-2, AC-3 (partly), AC-5, AC-6 (partly), AC-8. AC-4 is verified from code (`CombatConfig`). AC-7 (idle / resume) was not observed.

---

## Acceptance Criteria Results

| # | Criterion (from story) | Result | Notes |
|---|----------------------|--------|-------|
| AC-1 | A tower with an HP value and a mage on top exist in the main scene, side-view 2D | PASS | 01..05: gray tower, purple mage on top, green ground |
| AC-2 | Enemies spawn off-screen and walk toward the tower | PASS | 03 (9 s) vs 04 (30 s): enemies spaced ~216 px, approaching from the right edge |
| AC-3 | The mage automatically targets the nearest living enemy within range | PASS (partly) | Damage numbers appear at the front enemy (01-04); steady state of 4 enemies on screen shows kills keep pace. "Nearest" itself is from code (`TargetSelector`) |
| AC-4 | Attack speed, damage and range are data-driven | PASS (code) | All in `Assets/Resources/CombatConfig.asset`; changing `_mageDamage` to 1 changed the observed outcome in 05 |
| AC-5 | An enemy reaching the tower deals damage to tower HP; HP is shown on screen | PASS | 05: "Tower HP: 20 / 100" after 25 s with mage damage 1; HP text visible in every capture |
| AC-6 | Each hit shows a floating damage number and a hit effect; a killed enemy is removed | PASS (partly) | Damage number visible in 01-05 ("10", "1"). Removal of killed enemies is inferred: the on-screen enemy count stays at 4 over 30 s. The hit flash itself (0.12 s) is too short for a still |
| AC-7 | With no targets the mage idles and resumes attacking | NOT OBSERVED | Code only (`Mage`, `AttackCooldown`). Needs a playtest |
| AC-8 | A retained screenshot is saved in production/qa/evidence/ | PASS | this folder |

---

## Screenshots / Video

| # | Filename | What It Shows | Acceptance Criterion |
|---|----------|--------------|----------------------|
| 1 | `tower-auto-attack/01-combat.png` | First window capture: tower, mage, 4 enemies, damage number "10", HP 100/100 | AC-1, AC-2, AC-6, AC-8 |
| 2 | `tower-auto-attack/02-after-review-fixes.png` | Same scene after the unity-specialist review fixes (no regression) | AC-1, AC-2 |
| 3 | `tower-auto-attack/03-headless.png` | Window-free capture at 9 s | AC-1, AC-2 |
| 4 | `tower-auto-attack/04-after-30s.png` | Window-free capture at 30 s: HP still 100, 4 enemies, the mage holds the line | AC-2, AC-3, AC-6 |
| 5 | `tower-auto-attack/05-contact-damage-test.png` | Mage damage temporarily 1: enemies reach the tower, HP 20/100, damage number "1" | AC-5 |

---

## Test Conditions

- **Game state at start**: fresh run, default `CombatConfig` (captures 01-04); `_mageDamage: 1` (capture 05, restored afterwards)
- **Platform / hardware**: Windows 11, Intel UHD 730, D3D12, Win64 player, 1280x720
- **Framerate during test**: not measured
- **Any special setup required**: `HeadlessCapture` routes the overlay HUD canvas through the camera so it appears in the render texture

---

## Observations

- Placeholder art only (squares and circles); no attack animation or visible projectile in any still.
- With default values the mage kills every enemy before it reaches the tower, so tower HP never drops. Balance is untuned (Story 003 / playtest).
- Feel (timing, weight, responsiveness) is NOT assessed by stills; needs a playtest (`/team-qa`).
- Damage numbers render through legacy `TextMesh` under URP 2D in the Windows player; unverified in WebGL.

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Lead (art-director / designer) | Ali Safi | 2026-10-17 | [x] Approved |

---

*Template: `.claude/docs/templates/test-evidence.md`*
