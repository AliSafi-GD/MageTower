using System;
using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>One saved skill-node level, keyed by the node's enum name so reordering the enum cannot corrupt old saves.</summary>
    [Serializable]
    public sealed class SkillLevelEntry
    {
        /// <summary><see cref="SkillNodeType"/> name.</summary>
        public string node;
        /// <summary>Purchased level.</summary>
        public int level;
    }

    /// <summary>
    /// Plain-C# save DTO for <see cref="MetaProgress"/> (Story 007, design/game-brief.md MVP feature 7). Public fields so
    /// <c>JsonUtility</c> can (de)serialise it. <see cref="version"/> is for future migration.
    /// </summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        /// <summary>Save format version (see <see cref="MetaSaveMapper.CurrentVersion"/>).</summary>
        public int version;
        /// <summary>Permanent run-currency balance.</summary>
        public double currency;
        /// <summary>Highest wave ever reached.</summary>
        public int bestWave;
        /// <summary>Last-session timestamp as UTC ticks (<see cref="DateTime.Ticks"/>).</summary>
        public long lastSessionUtcTicks;
        /// <summary>Permanent skill levels, one entry per node.</summary>
        public SkillLevelEntry[] skillLevels = new SkillLevelEntry[0];
        /// <summary>Last equipped rune loadout (<see cref="RuneType"/> names). Stored only; not applied to new runs.</summary>
        public string[] lastRuneLoadout = new string[0];
    }

    /// <summary>
    /// Maps <see cref="MetaProgress"/> to/from <see cref="MetaSaveData"/> and validates loaded data (no UnityEngine).
    /// Loading is all-or-nothing: invalid data (wrong version, NaN/negative currency, bad best wave) is rejected and
    /// leaves the target untouched; recoverable oddities (unknown names, levels above max) are clamped/ignored with a warning.
    /// </summary>
    public static class MetaSaveMapper
    {
        /// <summary>Current save format version.</summary>
        public const int CurrentVersion = 1;
        /// <summary>Sanity ceiling for a saved best wave.</summary>
        public const int MaxBestWave = 1000000;

        /// <summary>Snapshots the meta state with the given UTC time as the last-session timestamp.</summary>
        public static MetaSaveData ToSave(MetaProgress meta, DateTime nowUtc)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            var data = new MetaSaveData
            {
                version = CurrentVersion,
                currency = meta.Wallet.Amount,
                bestWave = meta.BestWave,
                lastSessionUtcTicks = AsUtc(nowUtc).Ticks,
                skillLevels = new SkillLevelEntry[SkillTree.NodeCount],
                lastRuneLoadout = new string[meta.LastRuneLoadout.Count]
            };
            for (int i = 0; i < data.skillLevels.Length; i++)
                data.skillLevels[i] = new SkillLevelEntry { node = ((SkillNodeType)i).ToString(), level = meta.Tree.Level((SkillNodeType)i) };
            for (int i = 0; i < data.lastRuneLoadout.Length; i++)
                data.lastRuneLoadout[i] = meta.LastRuneLoadout[i].ToString();
            return data;
        }

        /// <summary>An Unspecified-kind time is taken to already be UTC (not shifted by the local offset); Local is converted.</summary>
        public static DateTime AsUtc(DateTime t) =>
            t.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : t.ToUniversalTime();

        /// <summary>
        /// Validates <paramref name="data"/> and restores it into <paramref name="meta"/>. Returns false (meta unchanged,
        /// reason in <paramref name="error"/>) if the data is unusable. Clamping notes are appended to <paramref name="warnings"/>.
        /// </summary>
        public static bool TryApply(MetaSaveData data, MetaProgress meta, List<string> warnings, out string error)
        {
            error = null;
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (data == null) { error = "save data is null"; return false; }
            if (data.version != CurrentVersion) { error = $"unsupported save version {data.version} (expected {CurrentVersion})"; return false; }
            if (double.IsNaN(data.currency) || double.IsInfinity(data.currency) || data.currency < 0.0)
            { error = $"invalid currency {data.currency}"; return false; }
            if (data.bestWave < 0 || data.bestWave > MaxBestWave) { error = $"invalid best wave {data.bestWave}"; return false; }

            var levels = new int[SkillTree.NodeCount];
            if (data.skillLevels != null)
            {
                foreach (SkillLevelEntry e in data.skillLevels)
                {
                    if (e == null || string.IsNullOrEmpty(e.node) || !Enum.TryParse(e.node, false, out SkillNodeType type)
                        || !Enum.IsDefined(typeof(SkillNodeType), type))
                    {
                        warnings?.Add($"ignored unknown skill node '{e?.node}'");
                        continue;
                    }
                    int clamped = Math.Max(0, Math.Min(e.level, meta.Tree.MaxLevel));
                    if (clamped != e.level) warnings?.Add($"clamped {e.node} level {e.level} to {clamped}");
                    levels[(int)type] = clamped;
                }
            }

            var loadout = new List<RuneType>();
            if (data.lastRuneLoadout != null)
            {
                foreach (string name in data.lastRuneLoadout)
                {
                    if (string.IsNullOrEmpty(name) || !Enum.TryParse(name, false, out RuneType rune)
                        || !Enum.IsDefined(typeof(RuneType), rune))
                    {
                        warnings?.Add($"ignored unknown rune '{name}'");
                        continue;
                    }
                    if (!loadout.Contains(rune)) loadout.Add(rune);
                }
            }

            if (!meta.Restore(data.currency, levels, data.bestWave, loadout)) { error = "restore rejected the data"; return false; }
            return true;
        }

        /// <summary>
        /// Reads the saved last-session time. False if the stored ticks are not a valid date (treated as "no timestamp"
        /// by the caller, so no offline gain).
        /// </summary>
        public static bool TryGetLastSession(MetaSaveData data, out DateTime lastSessionUtc)
        {
            lastSessionUtc = default;
            if (data == null || data.lastSessionUtcTicks <= 0L || data.lastSessionUtcTicks > DateTime.MaxValue.Ticks) return false;
            lastSessionUtc = new DateTime(data.lastSessionUtcTicks, DateTimeKind.Utc);
            return true;
        }
    }
}
