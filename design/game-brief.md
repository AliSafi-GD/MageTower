# Game Brief: Arcane Spire

**One-sentence pitch:** A mage on a tower auto-casts spells at endless enemy waves and grows stronger with gold, runes and skills, even while the game is closed.

## Core loop
- Mage auto-attacks the nearest enemy -> kills drop gold
- Gold buys upgrades, spells and runes -> tower power rises
- Waves and bosses scale harder -> player pushes for one more wave

## Player goal & fail state — what "working" looks like
- Goal: get further each run than the last (higher wave record).
- Fail: tower HP hits 0. Run ends, permanent currency is kept, next run restarts at wave 1 with permanent upgrades applied.

## MVP — what must exist to be the game
1. Tower with HP + mage auto-attacking the nearest enemy
2. Endless waves with scaling difficulty + a boss every N waves
3. Gold from kills + upgrade shop (damage, attack speed, HP, crit) with rising costs
4. 3 auto-cast spells on cooldown (fireball, frost, lightning)
5. 3 runes with passive effects (equippable slots)
6. Run end + permanent upgrades (simple skill tree)
7. Save/load + offline progress calculated on return

## Out of scope — not building this
- In-app purchases, ads, "premium perks"
- Daily quests, leaderboards, multiplayer
- Multiple maps or towers, story, localization
- Mobile build (WebGL desktop first)

## Build order
1. Tower + enemy + auto-attack (combat feel, hit numbers)
2. Waves + gold + shop (economy curve — the riskiest part)
3. Spells
4. Runes
5. Run end + permanent upgrades
6. Save + offline progress
7. UI polish + WebGL build

---
**Who it's for / what they feel:** Browser idle/incremental fans who want short sessions and the "number go up" feeling.

**Art & audio direction:** 2D cartoon fantasy, big hit numbers and punchy spell VFX, short satisfying SFX.

**Reference game:** Mage Tower Idle Defense — keeps the auto-attack loop, upgrades and offline growth (~10%); no real-money systems or daily quests.
