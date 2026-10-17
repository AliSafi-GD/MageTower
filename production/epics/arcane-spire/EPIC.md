# Epic: Arcane Spire

> **Source**: `design/game-brief.md` (minimal tier — no `/create-epics`)
> **Status**: Ready

## Goal
A mage on a tower auto-casts spells at endless enemy waves and grows stronger with gold, runes and skills, even while the game is closed.

## Scope (MVP)
1. Tower with HP + mage auto-attacking the nearest enemy
2. Endless waves with scaling difficulty + a boss every N waves
3. Gold from kills + upgrade shop (damage, attack speed, HP, crit) with rising costs
4. 3 auto-cast spells on cooldown (fireball, frost, lightning)
5. 3 runes with passive effects (equippable slots)
6. Run end + permanent upgrades (simple skill tree)
7. Save/load + offline progress calculated on return

## Ordering (brief build order)
Tower and attack -> waves, gold, shop -> spells -> runes -> run end and permanent upgrades -> save and offline -> UI polish and WebGL build (not yet a story).

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Tower and mage auto-attack | Visual/Feel | Ready | N/A |
| 002 | Endless waves and bosses | Logic | Ready | N/A |
| 003 | Gold and upgrade shop | Logic | Ready | N/A |
| 004 | Auto-cast spells | Logic | Ready | N/A |
| 005 | Runes | Logic | Ready | N/A |
| 006 | Run end and permanent upgrades | Integration | Ready | N/A |
| 007 | Save/load and offline progress | Integration | Ready | N/A |
