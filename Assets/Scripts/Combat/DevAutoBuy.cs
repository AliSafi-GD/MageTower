using System;
using System.Globalization;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Dev-only test hook for balance measurement. Created by <see cref="CombatBootstrap"/> only when the
    /// command line contains <c>--autobuy</c>. Every <c>--autobuy-interval</c> seconds (default 1) it buys the
    /// cheapest affordable, non-maxed upgrade through <see cref="ShopModel.TryBuy"/> (the same model call a
    /// button press makes, so gold is deducted from the wallet and the effects apply normally).
    /// </summary>
    public sealed class DevAutoBuy : MonoBehaviour
    {
        /// <summary>Flag that enables the hook.</summary>
        public const string Flag = "--autobuy";
        /// <summary>Option giving the purchase interval in seconds (InvariantCulture).</summary>
        public const string IntervalFlag = "--autobuy-interval";
        /// <summary>Flag making the hook prefer spell unlocks over stat upgrades (saves gold for them).</summary>
        public const string SpellsFirstFlag = "--autobuy-spells-first";
        /// <summary>Flag making the hook prefer rune unlocks over everything else (saves gold for them).</summary>
        public const string RunesFirstFlag = "--autobuy-runes-first";
        /// <summary>
        /// Flag making the hook also drive the Story 006 run-end flow: once the tower falls, buy the cheapest
        /// affordable skill-tree node (if any) and start a new run, so a long <c>--seconds</c> headless capture can
        /// show multiple runs and skill-tree purchases unattended. No effect without <see cref="Flag"/>.
        /// </summary>
        public const string MetaFlag = "--autobuy-meta";
        /// <summary>
        /// Option <c>--autoruns N</c> (or <c>--autoruns=N</c>): implies <see cref="MetaFlag"/> and stops starting new
        /// runs once run N has ended (the summary of run N stays up). Without it, meta driving never stops.
        /// </summary>
        public const string AutoRunsFlag = "--autoruns";
        /// <summary>Flag: pause <see cref="HoldSummarySeconds"/> on each run summary before acting, so a capture can land on it.</summary>
        public const string HoldSummaryFlag = "--autoruns-hold-summary";
        /// <summary>Seconds the run summary is held with <see cref="HoldSummaryFlag"/>.</summary>
        public const float HoldSummarySeconds = 20f;
        /// <summary>Interval used when none (or an invalid one) is given.</summary>
        public const float DefaultInterval = 1f;
        private const float MinInterval = 0.05f;

        private ShopModel _shop;
        private Tower _tower;
        private bool _spellsFirst;
        private bool _runesFirst;
        private RuneBook _runes;
        // Runes this hook unlocked itself and has not equipped yet. Each is auto-equipped at most once, so a rune
        // the player unequips by hand is never re-equipped.
        private bool[] _autoEquipPending;
        private float _interval;
        private float _timer;

        // Story 006 run-end driving (--autobuy-meta): inert unless both _shop (via --autobuy) and this are set.
        private SkillTree _skillTree;
        private MetaWallet _metaWallet;
        private bool _autoBuyMeta;
        private RunLifecycle _lifecycle;
        private int _maxRuns;
        private bool _holdSummary;
        /// <summary>Skips in-run shop/spell/rune purchases so runs end quickly; meta driving still runs.</summary>
        public const string NoShopFlag = "--autobuy-no-shop";
        private bool _noShop;
        /// <summary>With --autoruns-hold-summary: opens the skill tree halfway through the hold (after buying).</summary>
        public const string ShowTreeFlag = "--autoruns-show-tree";
        private bool _showTree;
        private bool _treeShown;
        private string _boughtSkill = "none";
        private bool _metaArmed;
        private bool _metaActionTaken;
        private float _metaTimer;

        /// <summary>
        /// Parses the command line. Returns true if <see cref="Flag"/> is present; <paramref name="interval"/> gets the
        /// value of <c>--autobuy-interval &lt;s&gt;</c> or <c>--autobuy-interval=&lt;s&gt;</c>, or <see cref="DefaultInterval"/>.
        /// </summary>
        public static bool IsRequested(string[] args, out float interval)
        {
            interval = DefaultInterval;
            if (args == null) return false;
            bool requested = false;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == Flag) requested = true;
                else if (a == IntervalFlag && i + 1 < args.Length) interval = ParseInterval(args[i + 1]);
                else if (a.StartsWith(IntervalFlag + "=", StringComparison.Ordinal))
                    interval = ParseInterval(a.Substring(IntervalFlag.Length + 1));
            }
            return requested;
        }

        /// <summary>True if <see cref="SpellsFirstFlag"/> is on the command line.</summary>
        public static bool PrefersSpells(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++) if (args[i] == SpellsFirstFlag) return true;
            return false;
        }

        /// <summary>True if <see cref="RunesFirstFlag"/> is on the command line.</summary>
        public static bool PrefersRunes(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++) if (args[i] == RunesFirstFlag) return true;
            return false;
        }

        /// <summary>True if <see cref="MetaFlag"/> is on the command line.</summary>
        public static bool PrefersMeta(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++) if (args[i] == MetaFlag) return true;
            return false;
        }

        /// <summary>True if meta driving was asked for: <see cref="MetaFlag"/> or <see cref="AutoRunsFlag"/>.</summary>
        public static bool WantsMeta(string[] args) => PrefersMeta(args) || MaxRuns(args) >= 0;

        /// <summary>True if <see cref="HoldSummaryFlag"/> is on the command line.</summary>
        public static bool HoldsSummary(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++) if (args[i] == HoldSummaryFlag) return true;
            return false;
        }

        /// <summary>Value of <c>--autoruns N</c> / <c>--autoruns=N</c> (N &gt;= 1), or -1 if absent or invalid.</summary>
        public static int MaxRuns(string[] args)
        {
            if (args == null) return -1;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string v = null;
                if (a == AutoRunsFlag) { if (i + 1 < args.Length) v = args[i + 1]; }
                else if (a != null && a.StartsWith(AutoRunsFlag + "=", StringComparison.Ordinal)) v = a.Substring(AutoRunsFlag.Length + 1);
                else continue;
                return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 1 ? n : -1;
            }
            return -1;
        }

        private static float ParseInterval(string s)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                && !float.IsNaN(v) && !float.IsInfinity(v) && v > 0f)
                return Mathf.Max(MinInterval, v);
            return DefaultInterval;
        }

        /// <summary>
        /// Injects the shop and tower (purchases stop once the tower has fallen). Optional Story 006 run-end
        /// driving: when <paramref name="autoBuyMeta"/> is true, <paramref name="skillTree"/>,
        /// <paramref name="metaWallet"/> and <paramref name="lifecycle"/> must all be supplied so that once the
        /// tower falls this hook can buy a skill node and ask <see cref="RunLifecycle"/> to start a new run
        /// (stopping after <paramref name="maxRuns"/> runs if &gt; 0; holding the summary 20 s if <paramref name="holdSummary"/>).
        /// </summary>
        public void Initialize(ShopModel shop, Tower tower, float interval, bool spellsFirst = false,
            RuneBook runes = null, bool runesFirst = false, SkillTree skillTree = null, MetaWallet metaWallet = null,
            bool autoBuyMeta = false, RunLifecycle lifecycle = null, int maxRuns = 0, bool holdSummary = false)
        {
            _spellsFirst = spellsFirst;
            _runesFirst = runesFirst;
            _runes = runes;
            _autoEquipPending = new bool[RuneBook.RuneCount];
            if (_runes != null)
            {
                _runes.OnUnlocked += LogUnlocked;
                _runes.OnEquipped += LogEquipped;
                _runes.OnUnequipped += LogUnequipped;
            }
            _shop = shop;
            _tower = tower;
            _skillTree = skillTree;
            _metaWallet = metaWallet;
            _autoBuyMeta = autoBuyMeta;
            _lifecycle = lifecycle;
            _maxRuns = Mathf.Max(0, maxRuns);
            _holdSummary = holdSummary;
            _interval = Mathf.Max(MinInterval, interval);
            _timer = _interval;
            var cmd = Environment.GetCommandLineArgs();
            _noShop = Array.IndexOf(cmd, NoShopFlag) >= 0;
            _showTree = Array.IndexOf(cmd, ShowTreeFlag) >= 0;
            Debug.Log($"DevAutoBuy: enabled, interval {_interval.ToString("0.###", CultureInfo.InvariantCulture)}s, spells first: {_spellsFirst}, runes first: {_runesFirst}, meta: {_autoBuyMeta}");
        }

        private void OnDestroy()
        {
            if (_runes == null) return;
            _runes.OnUnlocked -= LogUnlocked;
            _runes.OnEquipped -= LogEquipped;
            _runes.OnUnequipped -= LogUnequipped;
        }

        private string GoldLeft() => _shop.Wallet.Amount.ToString("0", CultureInfo.InvariantCulture);
        private void LogUnlocked(RuneType t) => Debug.Log($"DevAutoBuy: Rune {t} unlocked, gold left {GoldLeft()}");
        private void LogEquipped(RuneType t) => Debug.Log($"DevAutoBuy: Rune {t} equipped ({_runes.EquippedCount}/{_runes.SlotCount} slots)");
        private void LogUnequipped(RuneType t) => Debug.Log($"DevAutoBuy: Rune {t} unequipped ({_runes.EquippedCount}/{_runes.SlotCount} slots)");

        /// <summary>
        /// Rune step: equips runes this hook unlocked itself (once each, free action), then, if requested or cheapest,
        /// unlocks. Only wants runes that will fit in the slots, so gold is never wasted on a rune that cannot be equipped.
        /// Returns the cheapest affordable wanted rune (or -1); <paramref name="saving"/> = a wanted rune exists but is unaffordable.
        /// </summary>
        private int RuneStep(out double cost, out bool saving)
        {
            cost = double.PositiveInfinity;
            saving = false;
            if (_runes == null) return -1;
            EquipPending();
            if (_runes.UnlockedCount >= _runes.SlotCount) return -1;
            int best = -1;
            for (int i = 0; i < RuneBook.RuneCount; i++)
            {
                var t = (RuneType)i;
                if (_runes.IsUnlocked(t)) continue;
                saving = true;
                if (!_runes.CanUnlock(t)) continue;
                double c = _runes.UnlockCost(t);
                if (c < cost) { cost = c; best = i; }
            }
            return best;
        }

        /// <summary>Unlocks through the same model call the panel uses and queues the rune for a single auto-equip.</summary>
        private bool UnlockRune(RuneType type)
        {
            if (!_runes.TryUnlock(type)) return false;
            _autoEquipPending[(int)type] = true;
            EquipPending();
            return true;
        }

        /// <summary>Equips queued runes while a slot is free; each queue entry is consumed once equipped (by us or the player).</summary>
        private void EquipPending()
        {
            for (int i = 0; i < _autoEquipPending.Length; i++)
            {
                if (!_autoEquipPending[i]) continue;
                var t = (RuneType)i;
                if (_runes.IsEquipped(t) || _runes.TryEquip(t)) _autoEquipPending[i] = false;
            }
        }

        private void Update()
        {
            if (_tower != null && !_tower.IsAlive)
            {
                if (!_autoBuyMeta || _metaActionTaken) return;
                // Reuses _interval as a short, configurable pause so a headless capture can still show the result
                // of the run before the hook advances to the next one.
                if (!_metaArmed) { _metaArmed = true; _metaTimer = _holdSummary ? HoldSummarySeconds : _interval; }
                _metaTimer -= Time.deltaTime;
                // --autoruns-show-tree: buy first, then show the tree for the second half of the hold so a
                // capture can land on it with the purchase visible.
                if (_showTree && _holdSummary && !_treeShown && _metaTimer < HoldSummarySeconds * 0.5f)
                {
                    _treeShown = true;
                    BuyCheapestSkill(out _boughtSkill);
                    _lifecycle?.DevShowSkillTree();
                }
                if (_metaTimer > 0f) return;
                _metaActionTaken = true;
                RunMetaStep();
                return;
            }
            if (_shop == null || _noShop) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer += _interval;
            if (_timer < 0f) _timer = 0f; // no burst after a long frame

            int rune = RuneStep(out double runeCost, out bool runeSaving);
            if (_runesFirst)
            {
                if (rune >= 0 && UnlockRune((RuneType)rune)) return;
                if (runeSaving) return;
            }

            // Spells-first: buy the cheapest affordable spell, and while any spell is still unowned save gold for it.
            if (_spellsFirst && _shop.HasSpells)
            {
                int spell = -1;
                double spellCost = double.PositiveInfinity;
                bool anyUnowned = false;
                for (int i = 0; i < SpellBook.SpellCount; i++)
                {
                    var st = (SpellType)i;
                    if (_shop.IsSpellOwned(st)) continue;
                    anyUnowned = true;
                    if (!_shop.CanBuySpell(st)) continue;
                    double c = _shop.GetSpellCost(st);
                    if (c < spellCost) { spellCost = c; spell = i; }
                }
                if (spell >= 0) { BuySpell((SpellType)spell); return; }
                if (anyUnowned) return;
            }

            // Cheapest affordable not-yet-owned item among upgrades and spells.
            int best = -1;
            bool bestIsSpell = false;
            double bestCost = double.PositiveInfinity;
            for (int i = 0; i < ShopModel.UpgradeCount; i++)
            {
                var type = (UpgradeType)i;
                if (!_shop.CanBuy(type)) continue; // CanBuy excludes maxed and unaffordable upgrades
                double cost = _shop.GetCost(type);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = i;
                    bestIsSpell = false;
                }
            }
            if (_shop.HasSpells)
            {
                for (int i = 0; i < SpellBook.SpellCount; i++)
                {
                    var st = (SpellType)i;
                    if (!_shop.CanBuySpell(st)) continue;
                    double cost = _shop.GetSpellCost(st);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = i;
                        bestIsSpell = true;
                    }
                }
            }
            bool bestIsRune = false;
            if (rune >= 0 && runeCost < bestCost)
            {
                best = rune;
                bestIsRune = true;
            }
            if (best < 0) return;

            if (bestIsRune)
            {
                UnlockRune((RuneType)best);
                return;
            }
            if (bestIsSpell)
            {
                BuySpell((SpellType)best);
                return;
            }
            var chosen = (UpgradeType)best;
            if (_shop.TryBuy(chosen))
            {
                Debug.Log($"DevAutoBuy: {chosen} -> Lv {_shop.GetLevel(chosen)}, gold left {_shop.Wallet.Amount.ToString("0", CultureInfo.InvariantCulture)}");
            }
        }

        /// <summary>
        /// Story 006 run-end step: buys one level of the cheapest affordable skill node through the same model call
        /// the skill tree panel uses (if any is affordable), logs the run result, then starts a new run unless
        /// <c>--autoruns N</c> runs have been played.
        /// </summary>
        private void RunMetaStep()
        {
            string skill = _boughtSkill;
            if (!_treeShown) BuyCheapestSkill(out skill);
            int run = _lifecycle != null ? _lifecycle.RunNumber : 0;
            int wave = _lifecycle != null ? _lifecycle.LastWaveReached : 0;
            double earned = _lifecycle != null ? _lifecycle.CurrencyEarnedThisRun : 0.0;
            Debug.Log($"DevAutoBuy: Run {run} ended wave {wave} earned {earned.ToString("0", CultureInfo.InvariantCulture)}; skill {skill}");
            if (_lifecycle == null) return;
            if (_maxRuns > 0 && run >= _maxRuns)
            {
                Debug.Log($"DevAutoBuy: reached --autoruns {_maxRuns}, not starting another run");
                return;
            }
            Debug.Log($"DevAutoBuy: NEW RUN {run + 1}");
            _lifecycle.StartNewRun();
        }

        private void BuyCheapestSkill(out string skill)
        {
            skill = "none";
            if (_skillTree == null || _metaWallet == null) return;
            int best = -1;
            double bestCost = double.PositiveInfinity;
            for (int i = 0; i < SkillTree.NodeCount; i++)
            {
                var type = (SkillNodeType)i;
                if (!_skillTree.CanBuy(type, _metaWallet)) continue;
                double cost = _skillTree.Cost(type);
                if (cost < bestCost) { bestCost = cost; best = i; }
            }
            if (best >= 0 && _skillTree.TryBuy((SkillNodeType)best, _metaWallet))
                skill = $"{(SkillNodeType)best} -> Lv {_skillTree.Level((SkillNodeType)best)}";
        }

        private void BuySpell(SpellType type)
        {
            if (_shop.TryBuySpell(type))
                Debug.Log($"DevAutoBuy: unlocked {type}, gold left {_shop.Wallet.Amount.ToString("0", CultureInfo.InvariantCulture)}");
        }
    }
}
