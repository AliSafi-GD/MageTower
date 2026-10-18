using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Builds the combat scene at runtime inside the main scene (camera, ground, tower, mage, spawner, HUD) so no
    /// scene YAML needs hand-editing. <see cref="Build"/> runs once per play session and creates the persistent
    /// <see cref="RunLifecycle"/> (Story 006: owns the permanent <see cref="MetaWallet"/>/<see cref="SkillTree"/>
    /// and the run-summary/skill-tree screens); <see cref="BuildRun"/> is what actually builds one run's hierarchy,
    /// called by <see cref="RunLifecycle"/> once at startup and again every time a new run starts.
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
            var metaGo = new GameObject("MetaRoot");
            metaGo.AddComponent<RunLifecycle>().Initialize(config);
        }

        /// <summary>
        /// Builds one run's combat hierarchy (camera reused if present, ground/tower/mage/spawner/shop/spells/runes/HUD
        /// created fresh) under a new root parented to <paramref name="parent"/>. <paramref name="meta"/> is the
        /// persistent Story 006 meta state (same instance across every call): its best wave seeds the new
        /// <see cref="WaveModel"/>, and the run pushes wave progress and kill currency into it (the meta object never
        /// subscribes to anything in the run). <paramref name="art"/> is the shared sprite set, created once per session.
        /// <paramref name="onCurrencyEarned"/> is invoked with each kill's run-currency reward (for the run summary);
        /// <paramref name="lifecycle"/> is handed to the dev-only autobuy hook so <c>--autoruns</c> /
        /// <c>--autobuy-meta</c> can read results and start the next run itself. Returns the handles <see cref="RunLifecycle"/> needs to detect the run's end and
        /// tear it down before the next call.
        /// </summary>
        public static RunHandles BuildRun(Transform parent, CombatConfig config, MetaProgress meta, RunArt art,
            Action<double> onCurrencyEarned, RunLifecycle lifecycle)
        {
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            MetaWallet metaWallet = meta?.Wallet;
            SkillTree skillTree = meta?.Tree;
            if (art == null) art = RunArt.Create();
            Sprite square = art.Square;
            Sprite circle = art.Circle;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Camera (side-view orthographic). Not parented under root: it must survive a run rebuild.
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
            // Story 006 permanent upgrade: flat HP, applied directly (like the shop's Tower HP upgrade) from the
            // first frame of the run, before anything can damage the tower.
            if (skillTree != null && skillTree.TowerHpBonus > 0f) tower.AddMaxHp(skillTree.TowerHpBonus);

            // Mage (on top of tower)
            GameObject mageGo = CreateSprite(root, "Mage", square, new Color(0.55f, 0.25f, 0.8f),
                new Vector3(0f, config.GroundY + config.TowerHeight + config.MageSize * 0.5f, 0f),
                Vector3.one * config.MageSize, 2);

            // Spawner
            var spawnerGo = new GameObject("EnemySpawner");
            spawnerGo.transform.SetParent(root, false);
            var spawner = spawnerGo.AddComponent<EnemySpawner>();
            var waves = new WaveModel(meta != null ? meta.BestWave : 0);
            // Best wave is recorded into the meta object as soon as it is reached (run -> meta push; dies with the run).
            if (meta != null) waves.OnWaveChanged += (current, best) => meta.RecordWave(current);
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
            // Single stat-modifier layer (shop + runes; Story 006 adds the permanent skill tree) used by every stat
            // read below. SkillTree implements IStatModifierSource directly, so this one AddSource call is all that
            // is needed for every owned node except Tower Bastion (applied above) to take effect immediately.
            stats = config.BuildStats(shop, runes);
            if (skillTree != null) stats.AddSource(skillTree);
            mage.Initialize(config, spawner.Enemies, shop, stats: stats);

            // Only kills award gold/run-currency (enemies that reach the tower raise OnRemoved but not OnEnemyKilled).
            spawner.OnEnemyKilled += enemy =>
            {
                wallet.Add(stats.ApplyGold(enemy.GoldReward));
                if (metaWallet != null && enemy.MetaReward > 0.0)
                {
                    metaWallet.Add(enemy.MetaReward);
                    onCurrencyEarned?.Invoke(enemy.MetaReward);
                }
            };
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
            var spellEffects = new SpellEffects(config, square, circle, art.Ring);
            var caster = mageGo.AddComponent<SpellCaster>();
            caster.Initialize(config, mage, spawner.Enemies, shop, spellBook, spellEffects, stats);

            // Stop combat when the tower falls (the run-end/meta flow is RunLifecycle's job, wired by the caller).
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
            string[] args = Environment.GetCommandLineArgs();
            if (DevAutoBuy.IsRequested(args, out float autoBuyInterval))
                hudGo.AddComponent<DevAutoBuy>().Initialize(shop, tower, autoBuyInterval,
                    DevAutoBuy.PrefersSpells(args), runes, DevAutoBuy.PrefersRunes(args),
                    skillTree, metaWallet, DevAutoBuy.WantsMeta(args), lifecycle,
                    Mathf.Max(0, DevAutoBuy.MaxRuns(args)), DevAutoBuy.HoldsSummary(args));

            return new RunHandles(root, tower, waves, runes);
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
