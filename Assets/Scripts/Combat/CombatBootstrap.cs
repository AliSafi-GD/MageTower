using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Builds the Story 001 combat scene at runtime inside the main scene
    /// (camera, ground, tower, mage, spawner, HUD) so no scene YAML needs hand-editing.
    /// </summary>
    public static class CombatBootstrap
    {
        /// <summary>Only this scene gets the combat setup.</summary>
        public const string TargetSceneName = "SampleScene";

        private const string RootName = "CombatRoot";

        // AfterSceneLoad fires once per play session, for the first loaded scene only,
        // so no duplicate-build guard is needed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName) return;

            CombatConfig config = CombatConfig.Load();
            var root = new GameObject(RootName).transform;
            Sprite square = PlaceholderArt.CreateSquare();
            Sprite circle = PlaceholderArt.CreateCircle();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Camera (side-view orthographic)
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = config.CameraOrthoSize;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.7f, 0.9f);

            // Horizontal positions and ground width are set by CombatLayout below.
            GameObject groundGo = CreateSprite(root, "Ground", square, new Color(0.3f, 0.55f, 0.25f),
                Vector3.zero, Vector3.one, 0);

            GameObject towerGo = CreateSprite(root, "Tower", square, new Color(0.5f, 0.5f, 0.55f),
                Vector3.zero, new Vector3(config.TowerWidth, config.TowerHeight, 1f), 1);
            var tower = towerGo.AddComponent<Tower>();
            tower.Initialize(config, 0f);

            // Mage (on top of tower)
            GameObject mageGo = CreateSprite(root, "Mage", square, new Color(0.55f, 0.25f, 0.8f),
                new Vector3(0f, config.GroundY + config.TowerHeight + config.MageSize * 0.5f, 0f),
                Vector3.one * config.MageSize, 2);

            // Spawner
            var spawnerGo = new GameObject("EnemySpawner");
            spawnerGo.transform.SetParent(root, false);
            var spawner = spawnerGo.AddComponent<EnemySpawner>();
            var waves = new WaveModel();
            spawner.Initialize(config, tower, waves, circle, new Color(0.8f, 0.2f, 0.2f), 0f);

            // Screen-edge layout (recomputed on resolution change)
            root.gameObject.AddComponent<CombatLayout>()
                .Initialize(config, cam, groundGo.transform, tower, mageGo.transform, spawner);

            // Mage wiring
            var mage = mageGo.AddComponent<Mage>();
            var wallet = new GoldWallet();
            // Runes (Story 005): equipped rune factors feed damage, kill gold and cooldowns. Nothing in the wave
            // logic touches the RuneBook, so equipped runes persist across waves.
            var runes = new RuneBook(wallet, config.BuildRuneDefs(), config.RuneSlotCount, config.MinCooldownMultiplier);
            CombatStats stats = null; // assigned below, before anything reads a cooldown
            var spellBook = new SpellBook(type => stats.SpellCooldown(type));
            // A shorter cooldown also shortens the cooldown already running, so the rune applies immediately.
            runes.OnChanged += spellBook.ClampRemainingToCooldown;
            var shop = new ShopModel(wallet,
                (type, level) => UpgradeEffects.Cost(config.UpgradeBaseCost(type), config.UpgradeCostGrowth, level),
                (type, level) => type == UpgradeType.CritChance &&
                    UpgradeEffects.IsCritMaxed(config.BaseCritChance, config.CritChancePerLevel, level, config.MaxCritChance),
                spellBook, type => config.SpellUnlockCost(type));
            // Single stat-modifier layer (shop + runes; Story 006 can AddSource) used by every stat read below.
            stats = config.BuildStats(shop, runes);
            mage.Initialize(config, spawner.Enemies, shop, stats: stats);

            // Only kills award gold (enemies that reach the tower raise OnRemoved but not OnEnemyKilled).
            spawner.OnEnemyKilled += enemy => wallet.Add(stats.ApplyGold(enemy.GoldReward));
            // Tower HP upgrade: raise max HP and heal by the added amount, immediately.
            shop.OnPurchased += (type, level) =>
            {
                if (type == UpgradeType.TowerHp) tower.AddMaxHp(config.TowerHpPerLevel);
            };

            // Feedback wiring
            var feedback = new CombatFeedback(config, square, circle, font);
            spawner.OnEnemyDamaged += feedback.ShowHit;
            mage.OnAttackFired += feedback.ShowBolt;

            // Spells (Story 004): auto-cast once unlocked in the shop.
            var spellEffects = new SpellEffects(config, square, circle, PlaceholderArt.CreateRing());
            var caster = mageGo.AddComponent<SpellCaster>();
            caster.Initialize(config, mage, spawner.Enemies, shop, spellBook, spellEffects, stats);

            // Stop combat when the tower falls (run-end flow belongs to a later story).
            tower.OnDestroyed += () =>
            {
                spawner.enabled = false;
                mage.enabled = false;
                caster.enabled = false;
            };

            // HUD
            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(root, false);
            hudGo.AddComponent<HudView>().Initialize(tower, waves, wallet, font);
            hudGo.AddComponent<SpellHudView>().Initialize(spellBook, config, font);
            var shopView = hudGo.AddComponent<ShopView>();
            shopView.Initialize(shop, config, font);
            tower.OnDestroyed += () => shopView.SetLocked(true);
            var runeView = hudGo.AddComponent<RuneView>();
            runeView.Initialize(runes, font);
            tower.OnDestroyed += () => runeView.SetLocked(true);

            // Dev-only test hook: does nothing unless launched with --autobuy.
            if (DevAutoBuy.IsRequested(System.Environment.GetCommandLineArgs(), out float autoBuyInterval))
                hudGo.AddComponent<DevAutoBuy>().Initialize(shop, tower, autoBuyInterval,
                    DevAutoBuy.PrefersSpells(System.Environment.GetCommandLineArgs()),
                    runes, DevAutoBuy.PrefersRunes(System.Environment.GetCommandLineArgs()));
        }

        private static GameObject CreateSprite(Transform parent, string name, Sprite sprite, Color color,
            Vector3 position, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
