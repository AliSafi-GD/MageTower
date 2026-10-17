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

        private static float ParseInterval(string s)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                && !float.IsNaN(v) && !float.IsInfinity(v) && v > 0f)
                return Mathf.Max(MinInterval, v);
            return DefaultInterval;
        }

        /// <summary>Injects the shop and tower (purchases stop once the tower has fallen).</summary>
        public void Initialize(ShopModel shop, Tower tower, float interval, bool spellsFirst = false,
            RuneBook runes = null, bool runesFirst = false)
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
            _interval = Mathf.Max(MinInterval, interval);
            _timer = _interval;
            Debug.Log($"DevAutoBuy: enabled, interval {_interval.ToString("0.###", CultureInfo.InvariantCulture)}s, spells first: {_spellsFirst}, runes first: {_runesFirst}");
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
            if (_shop == null || (_tower != null && !_tower.IsAlive)) return;
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

        private void BuySpell(SpellType type)
        {
            if (_shop.TryBuySpell(type))
                Debug.Log($"DevAutoBuy: unlocked {type}, gold left {_shop.Wallet.Amount.ToString("0", CultureInfo.InvariantCulture)}");
        }
    }
}
