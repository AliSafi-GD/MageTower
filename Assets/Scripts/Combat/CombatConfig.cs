using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Data-driven tuning for the tower / mage / enemy combat loop.
    /// Implements design/game-brief.md MVP feature 1 (tower + mage auto-attack).
    /// Loaded from Resources/CombatConfig.asset; falls back to in-code defaults if the asset is missing.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Arcane Spire/Combat Config")]
    public sealed class CombatConfig : ScriptableObject
    {
        /// <summary>Resources path (without extension) of the config asset.</summary>
        public const string ResourcePath = "CombatConfig";

        [Header("Mage")]
        [SerializeField, Min(0f)] private float _mageDamage = 10f;
        [SerializeField, Min(0f)] private float _mageAttacksPerSecond = 1.5f;
        [SerializeField, Min(0f)] private float _mageRange = 7.5f;
        [Tooltip("Hard ceiling on attacks per second after upgrades.")]
        [SerializeField, Min(0.1f)] private float _maxAttacksPerSecond = 20f;

        [Header("Tower")]
        [SerializeField, Min(1f)] private float _towerMaxHp = 100f;

        [Header("Enemy")]
        [SerializeField, Min(1f)] private float _enemyMaxHp = 30f;
        [SerializeField, Min(0f)] private float _enemyMoveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _enemyContactDamage = 10f;
        [SerializeField, Min(0.1f)] private float _enemySpawnInterval = 2f;

        [Header("Waves (design/game-brief.md MVP feature 2)")]
        [Tooltip("Enemy HP multiplies by (1 + rate) per wave after wave 1.")]
        [SerializeField, Min(0f)] private float _enemyHpGrowthPerWave = 0.12f;
        [Tooltip("Enemy contact damage multiplies by (1 + rate) per wave after wave 1.")]
        [SerializeField, Min(0f)] private float _enemyDamageGrowthPerWave = 0.06f;
        [SerializeField, Min(1)] private int _baseEnemyCount = 3;
        [Tooltip("Extra enemies per wave after wave 1 (fractional, floored).")]
        [SerializeField, Min(0f)] private float _enemyCountGrowthPerWave = 0.75f;
        [SerializeField, Min(1)] private int _maxEnemyCount = 30;
        [Tooltip("Every N-th wave spawns a boss (0 disables bosses).")]
        [SerializeField, Min(0)] private int _bossEveryNWaves = 5;
        [SerializeField, Min(1f)] private float _bossHpMultiplier = 5f;
        [SerializeField, Min(1f)] private float _bossSizeMultiplier = 1.7f;
        [SerializeField] private Color _bossColor = new Color(0.45f, 0.1f, 0.6f, 1f);
        [Tooltip("Seconds between a wave being cleared and the next wave starting.")]
        [SerializeField, Min(0f)] private float _interWaveDelay = 3f;

        [Header("Economy and shop (design/game-brief.md MVP feature 3)")]
        [Tooltip("Gold for killing a wave-1 regular enemy.")]
        [SerializeField, Min(0f)] private float _goldPerKill = 5f;
        [Tooltip("Gold per kill multiplies by (1 + rate) per wave after wave 1.")]
        [SerializeField, Min(0f)] private float _goldGrowthPerWave = 0.08f;
        [Tooltip("Boss kills award this multiple of the regular kill gold.")]
        [SerializeField, Min(1f)] private float _bossGoldMultiplier = 10f;
        [SerializeField, Min(1f)] private float _damageBaseCost = 25f;
        [SerializeField, Min(1f)] private float _attackSpeedBaseCost = 30f;
        [SerializeField, Min(1f)] private float _towerHpBaseCost = 20f;
        [SerializeField, Min(1f)] private float _critChanceBaseCost = 40f;
        [Tooltip("Cost of level n+1 = baseCost * growth^n.")]
        [SerializeField, Min(1f)] private float _upgradeCostGrowth = 1.35f;
        [Tooltip("Damage multiplier = 1 + this * level.")]
        [SerializeField, Min(0f)] private float _damageBonusPerLevel = 0.25f;
        [Tooltip("Attacks/sec multiplier = 1 + this * level.")]
        [SerializeField, Min(0f)] private float _attackSpeedBonusPerLevel = 0.12f;
        [Tooltip("Tower max HP added per level (also heals by this amount).")]
        [SerializeField, Min(0f)] private float _towerHpPerLevel = 20f;
        [SerializeField, Range(0f, 1f)] private float _baseCritChance = 0f;
        [Tooltip("Crit chance added per level (total capped at Max Crit Chance).")]
        [SerializeField, Range(0f, 1f)] private float _critChancePerLevel = 0.03f;
        [Tooltip("Crit chance ceiling; the crit upgrade shows MAX once reached.")]
        [SerializeField, Range(0f, 1f)] private float _maxCritChance = 0.75f;
        [SerializeField, Min(1f)] private float _critDamageMultiplier = 2f;
        [SerializeField] private Color _critNumberColor = new Color(1f, 0.5f, 0.1f, 1f);
        [SerializeField, Min(1f)] private float _critNumberScale = 1.5f;

        [Header("Spells (design/game-brief.md MVP feature 4)")]
        [Tooltip("Fireball damage per hit, before the shop damage multiplier.")]
        [SerializeField, Min(0f)] private float _fireballDamage = 20f;
        [SerializeField, Min(0.1f)] private float _fireballCooldown = 6f;
        [SerializeField, Min(1f)] private float _fireballUnlockCost = 60f;
        [Tooltip("Blast radius around the targeted enemy (world units).")]
        [SerializeField, Min(0.1f)] private float _fireballRadius = 2.5f;
        [SerializeField, Min(0f)] private float _frostDamage = 5f;
        [SerializeField, Min(0.1f)] private float _frostCooldown = 8f;
        [SerializeField, Min(1f)] private float _frostUnlockCost = 80f;
        [Tooltip("Enemies within this radius of the targeted enemy are slowed.")]
        [SerializeField, Min(0.1f)] private float _frostRadius = 3.5f;
        [Tooltip("Fraction of speed removed while slowed (0.5 = 50% slower).")]
        [SerializeField, Range(0f, 0.95f)] private float _frostSlowFraction = 0.5f;
        [SerializeField, Min(0.1f)] private float _frostSlowDuration = 3f;
        [SerializeField, Min(0f)] private float _lightningDamage = 15f;
        [SerializeField, Min(0.1f)] private float _lightningCooldown = 5f;
        [SerializeField, Min(1f)] private float _lightningUnlockCost = 100f;
        [Tooltip("Max enemies one cast hits (including the first).")]
        [SerializeField, Min(1)] private int _lightningMaxTargets = 4;
        [Tooltip("Max distance from the last hit enemy to the next one.")]
        [SerializeField, Min(0.1f)] private float _lightningJumpRange = 3f;
        [SerializeField, Min(0.05f)] private float _spellEffectLifetime = 0.35f;
        [SerializeField, Min(0.01f)] private float _lightningThickness = 0.15f;
        [SerializeField] private Color _fireballColor = new Color(1f, 0.5f, 0.1f, 0.6f);
        [SerializeField] private Color _frostColor = new Color(0.55f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color _lightningColor = new Color(1f, 0.95f, 0.2f, 1f);
        [Tooltip("RGB = tint of slowed enemies, A = tint strength.")]
        [SerializeField] private Color _frostTintColor = new Color(0.3f, 0.55f, 1f, 0.65f);

        [Header("Runes (design/game-brief.md MVP feature 5)")]
        [Tooltip("Max runes equipped at once.")]
        [SerializeField, Min(0)] private int _runeSlotCount = 2;
        [Tooltip("Floor of the cooldown factor so cooldowns never reach 0 (0.2 = at most 80% faster).")]
        [SerializeField, Range(0.05f, 1f)] private float _minCooldownMultiplier = 0.2f;
        [Tooltip("Same modifier type adds, different types multiply. Set per rune; two runes may share a type.")]
        [SerializeField, Min(1f)] private float _mightUnlockCost = 90f;
        [SerializeField] private RuneModifierType _mightModifier = RuneModifierType.Damage;
        [Tooltip("Fractional value: 0.25 = +25% (Damage/Gold) or -25% (Cooldown).")]
        [SerializeField, Range(0f, 0.9f)] private float _mightValue = 0.25f;
        [SerializeField, Min(1f)] private float _greedUnlockCost = 70f;
        [SerializeField] private RuneModifierType _greedModifier = RuneModifierType.Gold;
        [SerializeField, Range(0f, 0.9f)] private float _greedValue = 0.25f;
        [SerializeField, Min(1f)] private float _hasteUnlockCost = 110f;
        [SerializeField] private RuneModifierType _hasteModifier = RuneModifierType.Cooldown;
        [SerializeField, Range(0f, 0.9f)] private float _hasteValue = 0.2f;

        [Header("Meta progression / permanent skill tree (design/game-brief.md MVP feature 6)")]
        [Tooltip("Run currency for a wave-1 regular kill.")]
        [SerializeField, Min(0f)] private float _runCurrencyPerKill = 1f;
        [Tooltip("Run currency per kill multiplies by (1 + rate) per wave after wave 1 (slower than gold's growth on purpose).")]
        [SerializeField, Min(0f)] private float _runCurrencyGrowthPerWave = 0.05f;
        [Tooltip("Boss kills award this multiple of the regular kill run currency.")]
        [SerializeField, Min(1f)] private float _runCurrencyBossMultiplier = 8f;
        [Tooltip("Maximum level of every skill node.")]
        [SerializeField, Range(1, 20)] private int _skillMaxLevel = 5;
        [Tooltip("Each skill level costs this multiple of the previous level (round(baseCost * growth^level)).")]
        [SerializeField, Range(1f, 5f)] private float _skillCostGrowth = 1.6f;
        [Tooltip("Arcane Power: run-currency cost of the first level (later levels scale by the cost growth).")]
        [SerializeField, Min(1f)] private float _arcanePowerCost = 40f;
        [Tooltip("Arcane Power: permanent +damage fraction per level (0.15 = +15%).")]
        [SerializeField, Range(0f, 2f)] private float _arcanePowerDamageBonus = 0.15f;
        [Tooltip("Swift Hands: run-currency cost of the first level.")]
        [SerializeField, Min(1f)] private float _swiftHandsCost = 60f;
        [Tooltip("Swift Hands: permanent +attack speed fraction per level.")]
        [SerializeField, Range(0f, 2f)] private float _swiftHandsAttackSpeedBonus = 0.15f;
        [Tooltip("Quick Recovery: run-currency cost of the first level.")]
        [SerializeField, Min(1f)] private float _quickRecoveryCost = 70f;
        [Tooltip("Quick Recovery: permanent cooldown reduction fraction per level (spells and basic attack interval; total floored by the min cooldown multiplier).")]
        [SerializeField, Range(0f, 0.9f)] private float _quickRecoveryCooldownReduction = 0.10f;
        [Tooltip("Golden Touch: run-currency cost of the first level.")]
        [SerializeField, Min(1f)] private float _goldenTouchCost = 50f;
        [Tooltip("Golden Touch: permanent +kill gold fraction per level.")]
        [SerializeField, Range(0f, 2f)] private float _goldenTouchGoldBonus = 0.20f;
        [Tooltip("Tower Bastion: run-currency cost of the first level.")]
        [SerializeField, Min(1f)] private float _towerBastionCost = 90f;
        [Tooltip("Tower Bastion: permanent flat tower max HP per level, applied at run start.")]
        [SerializeField, Min(0f)] private float _towerBastionHpBonus = 50f;

        [Header("Layout (world units)")]
        [SerializeField] private float _groundY = -3f;
        [SerializeField, Min(1f)] private float _cameraOrthoSize = 5f;
        [SerializeField, Min(0.1f)] private float _towerWidth = 1.6f;
        [SerializeField, Min(0.1f)] private float _towerHeight = 3.5f;
        [SerializeField, Min(0.1f)] private float _mageSize = 0.9f;
        [SerializeField, Min(0.1f)] private float _enemySize = 0.9f;
        [SerializeField] private float _towerMarginFromScreenEdge = 1.5f;
        [SerializeField] private float _spawnMarginOffScreen = 1.5f;

        [Header("Feedback")]
        [SerializeField, Min(0.05f)] private float _damageNumberLifetime = 0.8f;
        [SerializeField] private float _damageNumberRiseSpeed = 1.6f;
        [SerializeField, Min(0.01f)] private float _damageNumberCharacterSize = 0.12f;
        [SerializeField] private Color _damageNumberColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.12f;
        [SerializeField] private Color _hitFlashColor = Color.white;
        [SerializeField, Min(0f)] private float _hitPunchScale = 0.3f;
        [SerializeField, Min(0.01f)] private float _hitBurstLifetime = 0.2f;
        [SerializeField, Min(0.01f)] private float _hitBurstSize = 0.9f;
        [SerializeField, Min(0.01f)] private float _attackPunchDuration = 0.12f;
        [SerializeField, Min(0f)] private float _attackPunchScale = 0.25f;
        [SerializeField, Min(0.01f)] private float _boltLifetime = 0.08f;
        [SerializeField, Min(0.01f)] private float _boltThickness = 0.08f;
        [SerializeField] private Color _boltColor = new Color(0.6f, 0.8f, 1f, 1f);

        /// <summary>Damage dealt per mage attack.</summary>
        public float MageDamage => _mageDamage;
        /// <summary>Mage attacks per second.</summary>
        public float MageAttacksPerSecond => _mageAttacksPerSecond;
        /// <summary>Mage targeting range in world units.</summary>
        public float MageRange => _mageRange;
        /// <summary>Tower starting/maximum HP.</summary>
        public float TowerMaxHp => _towerMaxHp;
        /// <summary>Enemy starting HP.</summary>
        public float EnemyMaxHp => Mathf.Max(1f, _enemyMaxHp);
        /// <summary>Enemy walk speed in units per second.</summary>
        public float EnemyMoveSpeed => Mathf.Max(0.01f, _enemyMoveSpeed);
        /// <summary>Tower HP lost when one enemy reaches the tower.</summary>
        public float EnemyContactDamage => Mathf.Max(0f, _enemyContactDamage);
        /// <summary>Seconds between consecutive enemy spawns within a wave (spawn spacing).</summary>
        public float EnemySpawnInterval => Mathf.Max(0.05f, _enemySpawnInterval);
        /// <summary>Per-wave fractional growth of enemy HP.</summary>
        public float EnemyHpGrowthPerWave => Mathf.Max(0f, _enemyHpGrowthPerWave);
        /// <summary>Per-wave fractional growth of enemy contact damage.</summary>
        public float EnemyDamageGrowthPerWave => Mathf.Max(0f, _enemyDamageGrowthPerWave);
        /// <summary>Enemies in wave 1.</summary>
        public int BaseEnemyCount => Mathf.Max(1, _baseEnemyCount);
        /// <summary>Extra enemies per wave after wave 1.</summary>
        public float EnemyCountGrowthPerWave => Mathf.Max(0f, _enemyCountGrowthPerWave);
        /// <summary>Hard cap on enemies per wave (performance).</summary>
        public int MaxEnemyCount => Mathf.Max(1, _maxEnemyCount);
        /// <summary>A boss appears every N-th wave (0 = never).</summary>
        public int BossEveryNWaves => Mathf.Max(0, _bossEveryNWaves);
        /// <summary>Boss HP multiplier over the regular enemy HP of that wave.</summary>
        public float BossHpMultiplier => Mathf.Max(1f, _bossHpMultiplier);
        /// <summary>Boss scale multiplier over the regular enemy size.</summary>
        public float BossSizeMultiplier => Mathf.Max(0.1f, _bossSizeMultiplier);
        /// <summary>Boss tint colour.</summary>
        public Color BossColor => _bossColor;
        /// <summary>Seconds between a cleared wave and the next.</summary>
        public float InterWaveDelay => Mathf.Max(0f, _interWaveDelay);
        /// <summary>Gold for a wave-1 regular kill.</summary>
        public float GoldPerKill => Mathf.Max(0f, _goldPerKill);
        /// <summary>Per-wave fractional growth of kill gold.</summary>
        public float GoldGrowthPerWave => Mathf.Max(0f, _goldGrowthPerWave);
        /// <summary>Boss kill gold multiple.</summary>
        public float BossGoldMultiplier => Mathf.Max(1f, _bossGoldMultiplier);
        /// <summary>Base cost (level 0 -> 1) of the given upgrade.</summary>
        public float UpgradeBaseCost(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return Mathf.Max(1f, _damageBaseCost);
                case UpgradeType.AttackSpeed: return Mathf.Max(1f, _attackSpeedBaseCost);
                case UpgradeType.TowerHp: return Mathf.Max(1f, _towerHpBaseCost);
                default: return Mathf.Max(1f, _critChanceBaseCost);
            }
        }
        /// <summary>Per-level cost growth factor (at least 1).</summary>
        public float UpgradeCostGrowth => Mathf.Max(1f, _upgradeCostGrowth);
        /// <summary>Fractional damage bonus per damage level.</summary>
        public float DamageBonusPerLevel => Mathf.Max(0f, _damageBonusPerLevel);
        /// <summary>Fractional attack-rate bonus per attack-speed level.</summary>
        public float AttackSpeedBonusPerLevel => Mathf.Max(0f, _attackSpeedBonusPerLevel);
        /// <summary>Tower max HP added per tower-HP level.</summary>
        public float TowerHpPerLevel => Mathf.Max(0f, _towerHpPerLevel);
        /// <summary>Crit chance with no upgrades.</summary>
        public float BaseCritChance => Mathf.Clamp01(_baseCritChance);
        /// <summary>Crit chance added per crit level.</summary>
        public float CritChancePerLevel => Mathf.Clamp01(_critChancePerLevel);
        /// <summary>Crit chance ceiling (0..1).</summary>
        public float MaxCritChance => Mathf.Clamp01(_maxCritChance);
        /// <summary>Ceiling on attacks per second after upgrades.</summary>
        public float MaxAttacksPerSecond => Mathf.Max(0.1f, _maxAttacksPerSecond);
        /// <summary>Damage multiplier on a crit.</summary>
        public float CritDamageMultiplier => Mathf.Max(1f, _critDamageMultiplier);
        /// <summary>Damage number colour for crits.</summary>
        public Color CritNumberColor => _critNumberColor;
        /// <summary>Damage number size multiplier for crits.</summary>
        public float CritNumberScale => Mathf.Max(1f, _critNumberScale);
        /// <summary>World Y of the ground surface.</summary>
        public float GroundY => _groundY;
        /// <summary>Orthographic camera size.</summary>
        public float CameraOrthoSize => _cameraOrthoSize;
        /// <summary>Tower width in world units.</summary>
        public float TowerWidth => _towerWidth;
        /// <summary>Tower height in world units.</summary>
        public float TowerHeight => _towerHeight;
        /// <summary>Mage placeholder size in world units.</summary>
        public float MageSize => _mageSize;
        /// <summary>Enemy placeholder size in world units.</summary>
        public float EnemySize => Mathf.Max(0.1f, _enemySize);
        /// <summary>Distance from the left screen edge to the tower centre.</summary>
        public float TowerMarginFromScreenEdge => _towerMarginFromScreenEdge;
        /// <summary>Distance beyond the right screen edge where enemies spawn.</summary>
        public float SpawnMarginOffScreen => _spawnMarginOffScreen;
        /// <summary>Seconds a floating damage number lives.</summary>
        public float DamageNumberLifetime => _damageNumberLifetime;
        /// <summary>Units per second a damage number rises.</summary>
        public float DamageNumberRiseSpeed => _damageNumberRiseSpeed;
        /// <summary>TextMesh character size of damage numbers.</summary>
        public float DamageNumberCharacterSize => _damageNumberCharacterSize;
        /// <summary>Damage number colour.</summary>
        public Color DamageNumberColor => _damageNumberColor;
        /// <summary>Seconds an enemy flashes when hit.</summary>
        public float HitFlashDuration => _hitFlashDuration;
        /// <summary>Enemy flash colour when hit.</summary>
        public Color HitFlashColor => _hitFlashColor;
        /// <summary>Fractional scale added at the peak of an enemy hit punch.</summary>
        public float HitPunchScale => _hitPunchScale;
        /// <summary>Lifetime of the hit burst sprite.</summary>
        public float HitBurstLifetime => _hitBurstLifetime;
        /// <summary>Final size of the hit burst sprite.</summary>
        public float HitBurstSize => _hitBurstSize;
        /// <summary>Duration of the mage attack scale punch.</summary>
        public float AttackPunchDuration => _attackPunchDuration;
        /// <summary>Fractional scale added at the peak of the mage attack punch.</summary>
        public float AttackPunchScale => _attackPunchScale;
        /// <summary>Lifetime of the bolt line shown on each attack.</summary>
        public float BoltLifetime => _boltLifetime;
        /// <summary>Thickness of the bolt line.</summary>
        public float BoltThickness => _boltThickness;
        /// <summary>Colour of the bolt line.</summary>
        public Color BoltColor => _boltColor;

        /// <summary>Base damage of one hit of the spell (before the shop damage multiplier).</summary>
        public float SpellDamage(SpellType type)
        {
            switch (type)
            {
                case SpellType.Fireball: return Mathf.Max(0f, _fireballDamage);
                case SpellType.Frost: return Mathf.Max(0f, _frostDamage);
                default: return Mathf.Max(0f, _lightningDamage);
            }
        }
        /// <summary>Cooldown of the spell in seconds.</summary>
        public float SpellCooldown(SpellType type)
        {
            switch (type)
            {
                case SpellType.Fireball: return Mathf.Max(0.1f, _fireballCooldown);
                case SpellType.Frost: return Mathf.Max(0.1f, _frostCooldown);
                default: return Mathf.Max(0.1f, _lightningCooldown);
            }
        }
        /// <summary>Gold cost to unlock the spell in the shop.</summary>
        public float SpellUnlockCost(SpellType type)
        {
            switch (type)
            {
                case SpellType.Fireball: return Mathf.Max(1f, _fireballUnlockCost);
                case SpellType.Frost: return Mathf.Max(1f, _frostUnlockCost);
                default: return Mathf.Max(1f, _lightningUnlockCost);
            }
        }
        /// <summary>Rune slots available.</summary>
        public int RuneSlotCount => Mathf.Max(0, _runeSlotCount);
        /// <summary>Floor of the rune cooldown factor.</summary>
        public float MinCooldownMultiplier => Mathf.Clamp(_minCooldownMultiplier, 0.05f, 1f);
        /// <summary>Gold cost to unlock the rune.</summary>
        public float RuneUnlockCost(RuneType type)
        {
            switch (type)
            {
                case RuneType.Might: return Mathf.Max(1f, _mightUnlockCost);
                case RuneType.Greed: return Mathf.Max(1f, _greedUnlockCost);
                default: return Mathf.Max(1f, _hasteUnlockCost);
            }
        }
        /// <summary>Modifier type granted by the rune.</summary>
        public RuneModifierType RuneModifier(RuneType type)
        {
            switch (type)
            {
                case RuneType.Might: return _mightModifier;
                case RuneType.Greed: return _greedModifier;
                default: return _hasteModifier;
            }
        }
        /// <summary>Fractional modifier value of the rune (0.25 = 25%).</summary>
        public float RuneValue(RuneType type)
        {
            switch (type)
            {
                case RuneType.Might: return Mathf.Clamp(_mightValue, 0f, 0.9f);
                case RuneType.Greed: return Mathf.Clamp(_greedValue, 0f, 0.9f);
                default: return Mathf.Clamp(_hasteValue, 0f, 0.9f);
            }
        }
        /// <summary>Builds the per-rune definitions (indexed by <see cref="RuneType"/>) for a <see cref="RuneBook"/>.</summary>
        public RuneDef[] BuildRuneDefs()
        {
            var defs = new RuneDef[RuneBook.RuneCount];
            for (int i = 0; i < defs.Length; i++)
            {
                var t = (RuneType)i;
                defs[i] = new RuneDef(RuneUnlockCost(t), RuneModifier(t), RuneValue(t));
            }
            return defs;
        }

        /// <summary>Builds the single stat-modifier layer from this config (either source may be null).</summary>
        public CombatStats BuildStats(ShopModel shop, RuneBook runes) =>
            new CombatStats(shop, runes, DamageBonusPerLevel, AttackSpeedBonusPerLevel, MaxAttacksPerSecond,
                MinCooldownMultiplier, SpellCooldown);

        /// <summary>Run currency for a wave-1 regular kill.</summary>
        public float RunCurrencyPerKill => Mathf.Max(0f, _runCurrencyPerKill);
        /// <summary>Per-wave fractional growth of run currency per kill.</summary>
        public float RunCurrencyGrowthPerWave => Mathf.Max(0f, _runCurrencyGrowthPerWave);
        /// <summary>Boss kill run-currency multiple.</summary>
        public float RunCurrencyBossMultiplier => Mathf.Max(1f, _runCurrencyBossMultiplier);

        /// <summary>Maximum level of every skill node.</summary>
        public int SkillMaxLevel => Mathf.Clamp(_skillMaxLevel, 1, 20);
        /// <summary>Per-level cost growth factor of skill nodes.</summary>
        public float SkillCostGrowth => Mathf.Clamp(_skillCostGrowth, 1f, 5f);

        /// <summary>Currency cost of the skill node's first level.</summary>
        public double SkillNodeCost(SkillNodeType type)
        {
            switch (type)
            {
                case SkillNodeType.ArcanePower: return Mathf.Max(1f, _arcanePowerCost);
                case SkillNodeType.SwiftHands: return Mathf.Max(1f, _swiftHandsCost);
                case SkillNodeType.QuickRecovery: return Mathf.Max(1f, _quickRecoveryCost);
                case SkillNodeType.GoldenTouch: return Mathf.Max(1f, _goldenTouchCost);
                default: return Mathf.Max(1f, _towerBastionCost);
            }
        }
        /// <summary>Effect value of one level of the skill node (see <see cref="SkillNodeDef.Value"/>).</summary>
        public float SkillNodeValue(SkillNodeType type)
        {
            switch (type)
            {
                case SkillNodeType.ArcanePower: return Mathf.Max(0f, _arcanePowerDamageBonus);
                case SkillNodeType.SwiftHands: return Mathf.Max(0f, _swiftHandsAttackSpeedBonus);
                case SkillNodeType.QuickRecovery: return Mathf.Clamp(_quickRecoveryCooldownReduction, 0f, 0.9f);
                case SkillNodeType.GoldenTouch: return Mathf.Max(0f, _goldenTouchGoldBonus);
                default: return Mathf.Max(0f, _towerBastionHpBonus);
            }
        }
        /// <summary>Builds the per-node definitions (indexed by <see cref="SkillNodeType"/>) for a <see cref="SkillTree"/>.</summary>
        public SkillNodeDef[] BuildSkillNodeDefs()
        {
            var defs = new SkillNodeDef[SkillTree.NodeCount];
            for (int i = 0; i < defs.Length; i++)
            {
                var t = (SkillNodeType)i;
                defs[i] = new SkillNodeDef(SkillNodeCost(t), SkillNodeValue(t));
            }
            return defs;
        }

        /// <summary>Main visual colour of the spell (also its HUD bar colour).</summary>
        public Color SpellColor(SpellType type)
        {
            switch (type)
            {
                case SpellType.Fireball: return _fireballColor;
                case SpellType.Frost: return _frostColor;
                default: return _lightningColor;
            }
        }
        /// <summary>Fireball blast radius.</summary>
        public float FireballRadius => Mathf.Max(0.1f, _fireballRadius);
        /// <summary>Frost pulse radius around the targeted enemy.</summary>
        public float FrostRadius => Mathf.Max(0.1f, _frostRadius);
        /// <summary>Fraction of speed removed by frost (0..0.95).</summary>
        public float FrostSlowFraction => Mathf.Clamp(_frostSlowFraction, 0f, 0.95f);
        /// <summary>Seconds a frost slow lasts (refreshed, not stacked, by a new hit).</summary>
        public float FrostSlowDuration => Mathf.Max(0.1f, _frostSlowDuration);
        /// <summary>Maximum enemies hit by one lightning cast.</summary>
        public int LightningMaxTargets => Mathf.Max(1, _lightningMaxTargets);
        /// <summary>Maximum jump distance of lightning.</summary>
        public float LightningJumpRange => Mathf.Max(0.1f, _lightningJumpRange);
        /// <summary>Seconds a spell visual effect lives.</summary>
        public float SpellEffectLifetime => Mathf.Max(0.05f, _spellEffectLifetime);
        /// <summary>Thickness of lightning segments.</summary>
        public float LightningThickness => Mathf.Max(0.01f, _lightningThickness);
        /// <summary>Fireball colour.</summary>
        public Color FireballColor => _fireballColor;
        /// <summary>Frost colour.</summary>
        public Color FrostColor => _frostColor;
        /// <summary>Lightning colour.</summary>
        public Color LightningColor => _lightningColor;
        /// <summary>Tint of slowed enemies (RGB colour, alpha = strength).</summary>
        public Color FrostTintColor => _frostTintColor;

        /// <summary>
        /// Loads the config from Resources; if missing, returns a runtime instance with the default values.
        /// </summary>
        public static CombatConfig Load()
        {
            var config = Resources.Load<CombatConfig>(ResourcePath);
            if (config != null) return config;
            Debug.LogWarning("CombatConfig asset not found in Resources; using built-in defaults.");
            return CreateInstance<CombatConfig>();
        }
    }
}
