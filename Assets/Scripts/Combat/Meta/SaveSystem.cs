using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Dev-only command-line options for the save system (all inert without their flag; default behaviour unchanged).
    /// <c>--save-key-suffix &lt;s&gt;</c> uses a separate PlayerPrefs key so test runs never touch the real save.
    /// The others REQUIRE a suffix (ignored with a warning otherwise, so they can never touch the real save):
    /// <c>--fake-away-hours &lt;h&gt;</c> pretends the last session ended h hours ago; <c>--wipe-save</c> deletes the
    /// save (and its corrupt backup) before loading; <c>--fake-best-wave &lt;n&gt;</c> raises the in-memory best wave at
    /// startup so a fresh save can still show the popup. Values may be given as <c>--flag value</c> or
    /// <c>--flag=value</c>; a value starting with "--" is rejected (it is the next flag, not a value).
    /// Deliberately not gated on Debug.isDebugBuild: evidence captures use release builds.
    /// </summary>
    public sealed class SaveDevOptions
    {
        /// <summary>Flag: key suffix.</summary>
        public const string SuffixFlag = "--save-key-suffix";
        /// <summary>Flag: fake hours away.</summary>
        public const string FakeAwayFlag = "--fake-away-hours";
        /// <summary>Flag: wipe the save.</summary>
        public const string WipeFlag = "--wipe-save";
        /// <summary>Flag: fake best wave.</summary>
        public const string FakeBestWaveFlag = "--fake-best-wave";

        /// <summary>Key suffix, or null.</summary>
        public string KeySuffix;
        /// <summary>Fake hours away, or null.</summary>
        public double? FakeAwayHours;
        /// <summary>True to delete the save before loading.</summary>
        public bool WipeSave;
        /// <summary>Fake best wave, or 0.</summary>
        public int FakeBestWave;
        /// <summary>Problems found while parsing (for the caller to log).</summary>
        public readonly List<string> Warnings = new List<string>();

        /// <summary>True if any dev save flag is in effect.</summary>
        public bool AnyActive => KeySuffix != null || WipeSave || FakeAwayHours.HasValue || FakeBestWave > 0;

        /// <summary>Parses the command line. Never throws.</summary>
        public static SaveDevOptions Parse(string[] args)
        {
            var o = new SaveDevOptions();
            if (args == null) return o;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.IsNullOrEmpty(a)) continue;
                if (a == WipeFlag) { o.WipeSave = true; continue; }
                if (TryValue(args, ref i, SuffixFlag, out string suffix))
                {
                    if (string.IsNullOrWhiteSpace(suffix) || suffix.StartsWith("--", StringComparison.Ordinal))
                        o.Warnings.Add($"{SuffixFlag} needs a value that does not start with \"--\"; ignored.");
                    else o.KeySuffix = suffix;
                }
                else if (TryValue(args, ref i, FakeAwayFlag, out string hours))
                {
                    if (double.TryParse(hours, NumberStyles.Float, CultureInfo.InvariantCulture, out double h)
                        && !double.IsNaN(h) && !double.IsInfinity(h) && h >= 0.0)
                        o.FakeAwayHours = h;
                    else o.Warnings.Add($"{FakeAwayFlag} value '{hours}' is not a non-negative number; ignored.");
                }
                else if (TryValue(args, ref i, FakeBestWaveFlag, out string wave))
                {
                    if (int.TryParse(wave, NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) && w > 0)
                        o.FakeBestWave = Math.Min(w, MetaSaveMapper.MaxBestWave);
                    else o.Warnings.Add($"{FakeBestWaveFlag} value '{wave}' is not a positive integer; ignored.");
                }
            }

            if (o.KeySuffix == null && (o.WipeSave || o.FakeAwayHours.HasValue || o.FakeBestWave > 0))
            {
                o.Warnings.Add($"{WipeFlag}, {FakeAwayFlag} and {FakeBestWaveFlag} require {SuffixFlag}; ignored so the real save is untouched.");
                o.WipeSave = false;
                o.FakeAwayHours = null;
                o.FakeBestWave = 0;
            }
            return o;
        }

        // A following argument that starts with "--" is the next flag: it is not consumed as this flag's value.
        private static bool TryValue(string[] args, ref int i, string flag, out string value)
        {
            value = null;
            string a = args[i];
            if (a == flag)
            {
                if (i + 1 < args.Length && args[i + 1] != null && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    value = args[i + 1];
                    i++;
                }
                return true;
            }
            if (a.StartsWith(flag + "=", StringComparison.Ordinal)) { value = a.Substring(flag.Length + 1); return true; }
            return false;
        }
    }

    /// <summary>
    /// Wires Story 007 (design/game-brief.md MVP feature 7) into the session: loads the <see cref="MetaProgress"/> from an
    /// <see cref="ISaveStore"/> at startup (missing save = normal fresh start; a present but unusable save = fresh start
    /// with its payload kept under a backup key and a warning logged before anything overwrites it), credits and
    /// announces offline progress (<see cref="OfflineProgress"/>, popup <see cref="OfflineProgressView"/>) at startup AND
    /// when the app regains focus / unpauses (e.g. a WebGL background tab), and saves: at most once per second after a
    /// skill purchase (dirty flag), immediately at run end (<see cref="RunLifecycle"/> calls <see cref="Save"/>), on focus
    /// loss / pause, on <see cref="Application.quitting"/>, and every <see cref="CombatConfig.AutosaveIntervalSeconds"/>
    /// (unscaled time, so it keeps running while the popup pauses the game). Time comes from an injectable UTC clock.
    ///
    /// Away accounting: on focus loss the real UTC time is remembered; frames that still run while unfocused (a
    /// run-in-background player or a throttled tab) are counted as live time. The saved timestamp is
    /// "remembered time + live seconds", never "now", so an autosave while unfocused cannot erase the away time, and on
    /// focus return only the non-live part of the absence is credited, exactly once per focus loss.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        /// <summary>PlayerPrefs key of the meta save (a dev suffix is appended as ".suffix").</summary>
        public const string BaseKey = "ArcaneSpire.MetaSave";
        /// <summary>Appended to the key to form the corrupt-payload backup key.</summary>
        public const string CorruptBackupSuffix = ".corrupt";
        /// <summary>Minimum seconds between batched (dirty-flag) saves.</summary>
        public const float DirtyFlushInterval = 1f;

        private CombatConfig _config;
        private MetaProgress _meta;
        private ISaveStore _store;
        private Func<DateTime> _utcNow;
        private string _key = BaseKey;
        private bool _initialized;
        private float _nextAutosave;
        private OfflineProgressView _popup;
        private OfflineResult _popupResult;

        private bool _dirty;
        private float _lastFlush = float.NegativeInfinity;

        // Focus/pause away tracking (null = not away).
        private DateTime? _awaySinceUtc;
        private double _liveSecondsWhileAway;

        /// <summary>Key the save is stored under.</summary>
        public string Key => _key;
        /// <summary>Corrupt-payload backup key.</summary>
        public string CorruptBackupKey => _key + CorruptBackupSuffix;
        /// <summary>True if the save existed but was unusable at startup.</summary>
        public bool LoadedCorruptSave { get; private set; }
        /// <summary>True if a valid save was restored at startup.</summary>
        public bool LoadedExistingSave { get; private set; }
        /// <summary>The most recent offline estimate (default if none was evaluated).</summary>
        public OfflineResult LastOffline { get; private set; }

        /// <summary>
        /// Loads, credits offline progress (once; a second call is ignored) and starts autosaving. Call before the first
        /// run is built so the restored skills and best wave apply to it. <paramref name="store"/>/<paramref name="utcNow"/>
        /// default to PlayerPrefs and <see cref="DateTime.UtcNow"/>; <paramref name="args"/> defaults to the command line.
        /// Never throws for bad saves or bad arguments: every failure is logged and leaves a fresh (or loaded) state.
        /// </summary>
        public void Initialize(CombatConfig config, MetaProgress meta, Font font, ISaveStore store = null,
            Func<DateTime> utcNow = null, string[] args = null)
        {
            if (_initialized) return; // guards against a second load double-crediting offline gains
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            _store = store ?? new PlayerPrefsSaveStore();
            _utcNow = utcNow ?? (() => DateTime.UtcNow);

            SaveDevOptions opts;
            try
            {
                opts = SaveDevOptions.Parse(args ?? Environment.GetCommandLineArgs());
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: could not parse the command line, dev save flags ignored: " + e.Message);
                opts = new SaveDevOptions();
            }
            foreach (string w in opts.Warnings) Debug.LogWarning("SaveSystem: " + w);
            _key = string.IsNullOrEmpty(opts.KeySuffix) ? BaseKey : BaseKey + "." + opts.KeySuffix;
            if (opts.AnyActive)
                Debug.LogWarning($"SaveSystem: DEV SAVE FLAGS ACTIVE (key '{_key}', wipe {opts.WipeSave}, fake away h {(opts.FakeAwayHours.HasValue ? opts.FakeAwayHours.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-")}, fake best wave {opts.FakeBestWave}).");

            _popup = new GameObject("OfflineProgressScreen").AddComponent<OfflineProgressView>();
            _popup.transform.SetParent(transform, false);
            _popup.Initialize(font);
            _popup.gameObject.SetActive(false);

            _initialized = true; // from here on Save() works, even if loading fails below

            if (opts.WipeSave)
            {
                try
                {
                    Debug.LogWarning($"SaveSystem: --wipe-save, deleting '{_key}' and '{CorruptBackupKey}'.");
                    _store.DeleteKey(_key);
                    _store.DeleteKey(CorruptBackupKey);
                    _store.Flush();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("SaveSystem: --wipe-save failed: " + e.Message);
                }
            }

            DateTime now = SafeNow();
            bool haveLastSession = false;
            DateTime lastSession = default;
            try
            {
                haveLastSession = LoadSave(out lastSession);
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: loading failed unexpectedly, starting fresh: " + e.Message);
                haveLastSession = false;
            }

            if (opts.FakeBestWave > 0 && _meta.RecordWave(opts.FakeBestWave))
                Debug.Log($"SaveSystem: dev --fake-best-wave, best wave set to {opts.FakeBestWave} (in memory).");
            if (opts.FakeAwayHours.HasValue)
            {
                lastSession = now - TimeSpan.FromHours(Math.Min(opts.FakeAwayHours.Value, 24.0 * 365.0 * 100.0));
                haveLastSession = true;
                Debug.Log($"SaveSystem: dev --fake-away-hours {opts.FakeAwayHours.Value.ToString("0.##", CultureInfo.InvariantCulture)}.");
            }

            if (haveLastSession) ApplyOffline((now - lastSession).TotalSeconds, "startup");

            // Persist immediately: the new timestamp and any credited gains are written in one value, so the
            // same absence can never be credited twice.
            Save();

            _meta.Tree.OnChanged += MarkDirty; // skill purchases: batched, flushed at most once per second
            Application.quitting += Save;
            _nextAutosave = Time.unscaledTime + _config.AutosaveIntervalSeconds;
        }

        private DateTime SafeNow()
        {
            try { return MetaSaveMapper.AsUtc(_utcNow()); }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: clock failed, using DateTime.UtcNow: " + e.Message);
                return DateTime.UtcNow;
            }
        }

        private bool LoadSave(out DateTime lastSession)
        {
            lastSession = default;
            string raw = _store.HasKey(_key) ? _store.GetString(_key, null) : null;
            if (string.IsNullOrEmpty(raw))
            {
                // Missing (no key / empty value) is a normal first launch: no warning, no backup.
                Debug.Log($"SaveSystem: no save under '{_key}', fresh start.");
                return false;
            }

            if (!MetaSaveSerializer.TryFromJson(raw, out MetaSaveData data, out string error))
            {
                FailLoad(raw, error);
                return false;
            }
            var warnings = new List<string>();
            if (!MetaSaveMapper.TryApply(data, _meta, warnings, out error))
            {
                FailLoad(raw, error);
                return false;
            }
            foreach (string w in warnings) Debug.LogWarning("SaveSystem: " + w);
            LoadedExistingSave = true;
            Debug.Log($"SaveSystem: loaded '{_key}': currency {_meta.Wallet.Amount.ToString("0.##", CultureInfo.InvariantCulture)}, best wave {_meta.BestWave}, skill levels [{string.Join(",", _meta.Tree.GetLevels())}], last rune loadout [{string.Join(",", _meta.LastRuneLoadout)}].");

            if (MetaSaveMapper.TryGetLastSession(data, out lastSession)) return true;
            Debug.LogWarning("SaveSystem: save has no valid last-session timestamp; no offline progress.");
            return false;
        }

        /// <summary>Backs a present-but-unusable payload up and warns BEFORE any later save can replace the main key.</summary>
        private void FailLoad(string raw, string reason)
        {
            LoadedCorruptSave = true;
            Debug.LogWarning($"SaveSystem: save '{_key}' is unusable ({reason}); starting fresh. The raw payload is kept under '{CorruptBackupKey}'.");
            try
            {
                _store.SetString(CorruptBackupKey, raw);
                _store.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: could not write the corrupt-save backup: " + e.Message);
            }
        }

        /// <summary>
        /// Credits one absence (startup or focus return): same cap, minimum and formula. A negative span (clock moved
        /// back / timestamp in the future) counts as 0. Shows the popup only when the gain is positive (which implies
        /// elapsed &gt;= the configured minimum); if the popup is still open, the results are summed.
        /// </summary>
        private void ApplyOffline(double elapsedSeconds, string source)
        {
            OfflineResult result = OfflineProgress.Compute(_config, _meta.BestWave, elapsedSeconds);
            LastOffline = result;
            Debug.Log($"SaveSystem: [{source}] away {OfflineProgress.FormatDuration(result.ElapsedSeconds)} (credited {OfflineProgress.FormatDuration(result.CreditedSeconds)}, capped {result.Capped}), offline gain {result.Currency.ToString("0", CultureInfo.InvariantCulture)}.");
            if (result.Currency <= 0.0) return;
            _meta.Wallet.Add(result.Currency);
            _popupResult = _popup.gameObject.activeSelf ? OfflineProgress.Combine(_popupResult, result) : result;
            _popup.Show(_popupResult, _config.OfflineMaxHours);
        }

        /// <summary>Writes the current meta state immediately (and clears the dirty flag). Never throws (failures are logged).</summary>
        public void Save()
        {
            if (!_initialized) return;
            _dirty = false;
            _lastFlush = Time.unscaledTime;
            try
            {
                _store.SetString(_key, MetaSaveSerializer.ToJson(MetaSaveMapper.ToSave(_meta, SaveTimestamp())));
                _store.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: save failed: " + e.Message);
            }
        }

        /// <summary>Requests a save; written within <see cref="DirtyFlushInterval"/> seconds (batched).</summary>
        public void MarkDirty() => _dirty = true;

        // While away, the session's "last live moment" is the remembered focus-loss time plus any frames that still
        // ran, never the current wall-clock time (which would erase the away time).
        private DateTime SaveTimestamp()
        {
            DateTime now = SafeNow();
            if (!_awaySinceUtc.HasValue) return now;
            DateTime live = _awaySinceUtc.Value.AddSeconds(_liveSecondsWhileAway);
            return live < now ? live : now;
        }

        private void BeginAway()
        {
            if (!_initialized || _awaySinceUtc.HasValue) return; // focus + pause both firing count as one absence
            _awaySinceUtc = SafeNow();
            _liveSecondsWhileAway = 0.0;
            Save();
        }

        private void EndAway()
        {
            if (!_initialized || !_awaySinceUtc.HasValue) return; // once per focus loss
            DateTime since = _awaySinceUtc.Value;
            double live = _liveSecondsWhileAway;
            _awaySinceUtc = null;
            _liveSecondsWhileAway = 0.0;
            double elapsed = (SafeNow() - since).TotalSeconds - live;
            ApplyOffline(elapsed, "focus return");
            Save();
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_awaySinceUtc.HasValue)
                _liveSecondsWhileAway += Math.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            float t = Time.unscaledTime;
            if (t >= _nextAutosave)
            {
                _nextAutosave = t + _config.AutosaveIntervalSeconds;
                Save();
            }
            else if (_dirty && t - _lastFlush >= DirtyFlushInterval)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) BeginAway();
            else EndAway();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) BeginAway();
            else EndAway();
        }

        private void OnDestroy()
        {
            Application.quitting -= Save;
            if (_meta != null) _meta.Tree.OnChanged -= MarkDirty;
        }
    }
}
