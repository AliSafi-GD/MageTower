using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Owns the run lifecycle across the whole play session (Story 006, design/game-brief.md MVP feature 6): the
    /// persistent <see cref="MetaProgress"/> (meta currency, skill tree, best wave), the shared placeholder sprites
    /// (<see cref="RunArt"/>), the run-summary and skill-tree screens, and
    /// rebuilding <see cref="CombatBootstrap"/>'s run hierarchy from scratch for every new run so gold, shop levels,
    /// wave number, spell unlocks and rune unlocks/equips all reset to their starting state while permanent upgrades,
    /// the meta currency and the best-wave record persist.
    ///
    /// Flow: Combat running -&gt; Tower destroyed -&gt; Run Summary (auto-shown) -&gt; [T] Skill Tree -&gt; [K] Back
    /// or [N] Start New Run; [N] also works directly from the Run Summary. Skill levels can only be bought between
    /// runs. During a run [K] opens the skill tree as a view-only translucent overlay (combat keeps running) and [K]
    /// closes it again. Run state (everything under the run root) and meta state (the single
    /// <see cref="MetaProgress"/> object, kept outside the run root) are separate, so Story 007 can save the meta
    /// object alone. The run pushes into the meta object (kill currency, best wave); nothing persistent subscribes
    /// to a run object except this class's single <see cref="Tower.OnDestroyed"/> handler, removed on teardown.
    /// </summary>
    public sealed class RunLifecycle : MonoBehaviour
    {
        private CombatConfig _config;
        private MetaProgress _meta;
        private RunArt _art;
        private RunHandles _current;
        private double _currencyEarnedThisRun;
        private int _bestWaveAtRunStart;
        private bool _runOver;
        private SaveSystem _save;

        private GameObject _summaryGo;
        private GameObject _skillTreeGo;
        private RunSummaryView _summaryView;
        private SkillTreeView _skillTreeView;

        /// <summary>1-based number of the current (or just-ended) run.</summary>
        public int RunNumber { get; private set; }
        /// <summary>True between the tower falling and the next run starting.</summary>
        public bool IsRunOver => _runOver;
        /// <summary>Wave reached in the run that just ended.</summary>
        public int LastWaveReached { get; private set; }
        /// <summary>Run currency earned in the run that just ended (or so far, mid-run).</summary>
        public double CurrencyEarnedThisRun => _currencyEarnedThisRun;

        /// <summary>The persistent meta state (what Story 007 saves).</summary>
        public MetaProgress Meta => _meta;

        /// <summary>Builds the persistent meta state and screens, then starts the first run.</summary>
        public void Initialize(CombatConfig config)
        {
            _config = config;
            _meta = new MetaProgress(new MetaWallet(),
                new SkillTree(config.BuildSkillNodeDefs(), config.SkillMaxLevel, config.SkillCostGrowth));
            _art = RunArt.Create();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _summaryGo = new GameObject("RunSummaryScreen");
            _summaryGo.transform.SetParent(transform, false);
            _summaryView = _summaryGo.AddComponent<RunSummaryView>();
            _summaryView.Initialize(font);
            _summaryView.OnOpenSkillTree += ShowSkillTreeFromSummary;
            _summaryView.OnStartNewRun += StartNewRun;
            _summaryGo.SetActive(false);

            _skillTreeGo = new GameObject("SkillTreeScreen");
            _skillTreeGo.transform.SetParent(transform, false);
            _skillTreeView = _skillTreeGo.AddComponent<SkillTreeView>();
            _skillTreeView.Initialize(_meta.Tree, _meta.Wallet, font);
            _skillTreeView.OnStartNewRun += StartNewRun;
            _skillTreeView.OnClose += CloseSkillTree;
            _skillTreeGo.SetActive(false);

            // Story 007: restore the saved meta state (and credit offline progress) BEFORE the first run is built,
            // so restored skills and the best-wave record apply to it from the first frame.
            // Never allowed to stop the first run from starting: any unexpected failure leaves a fresh meta state.
            try
            {
                _save = gameObject.AddComponent<SaveSystem>();
                _save.Initialize(config, _meta, font);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("RunLifecycle: save system failed to initialise, continuing without load: " + e);
            }

            StartNewRun();
        }

        /// <summary>
        /// Tears down the previous run (if any) and builds a fresh one: new <see cref="GoldWallet"/>,
        /// <see cref="ShopModel"/>, <see cref="SpellBook"/> and <see cref="RuneBook"/> all reset;
        /// <see cref="MetaWallet"/> and <see cref="SkillTree"/> (and the best-wave record) carry over.
        /// </summary>
        public void StartNewRun()
        {
            _summaryGo.SetActive(false);
            _skillTreeGo.SetActive(false);

            if (_current.Root != null)
            {
                _current.Tower.OnDestroyed -= HandleTowerDestroyed;
                // Deactivate first so the old run stops updating immediately (Destroy itself is end-of-frame).
                _current.Root.gameObject.SetActive(false);
                Destroy(_current.Root.gameObject);
            }

            RunNumber++;
            _runOver = false;
            _currencyEarnedThisRun = 0.0;
            _bestWaveAtRunStart = _meta.BestWave;
            _current = CombatBootstrap.BuildRun(transform, _config, _meta, _art,
                amount => _currencyEarnedThisRun += amount, this);
            _current.Tower.OnDestroyed += HandleTowerDestroyed;
        }

        private void HandleTowerDestroyed()
        {
            _current.Tower.OnDestroyed -= HandleTowerDestroyed;
            _runOver = true;
            LastWaveReached = _current.Waves.CurrentWave;
            _meta.RecordWave(LastWaveReached); // normally already recorded on OnWaveChanged
            bool isNewBest = LastWaveReached > _bestWaveAtRunStart;

            // Story 007: remember the loadout (stored in the save, NOT applied to the next run) and save now,
            // since the wave record and run currency just changed.
            var loadout = new System.Collections.Generic.List<RuneType>();
            if (_current.Runes != null)
                for (int i = 0; i < RuneBook.RuneCount; i++)
                    if (_current.Runes.IsEquipped((RuneType)i)) loadout.Add((RuneType)i);
            _meta.SetLastRuneLoadout(loadout);
            _save?.Save();

            _skillTreeGo.SetActive(false);
            _summaryView.Show(LastWaveReached, _meta.BestWave, isNewBest, _currencyEarnedThisRun, _meta.Wallet.Amount);
            _summaryGo.SetActive(true);
        }

        /// <summary>
        /// Dev/test entry point (used by DevAutoBuy for window-free captures): opens the skill tree from the
        /// summary, exactly as the [T] button does. Does nothing while a run is still in progress.
        /// </summary>
        public void DevShowSkillTree()
        {
            if (_runOver) ShowSkillTreeFromSummary();
        }

        private void ShowSkillTreeFromSummary()
        {
            _summaryGo.SetActive(false);
            _skillTreeView.SetContext(true);
            _skillTreeGo.SetActive(true);
        }

        private void CloseSkillTree()
        {
            _skillTreeGo.SetActive(false);
            if (!_runOver) return;
            _summaryView.SetTotalCurrency(_meta.Wallet.Amount); // purchases in the tree changed it
            _summaryGo.SetActive(true);
        }

        private void Update()
        {
            if (ModalGate.IsOpen) return;
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb[Key.K].wasPressedThisFrame) return;
            if (_skillTreeGo.activeSelf) CloseSkillTree();
            else if (!_runOver)
            {
                _skillTreeView.SetContext(false);
                _skillTreeGo.SetActive(true);
            }
        }
    }
}
