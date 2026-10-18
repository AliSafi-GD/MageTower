using System.Collections.Generic;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// Minimal string key/value storage behind the save system (Story 007, design/game-brief.md MVP feature 7).
    /// Kept tiny so the real backend can be PlayerPrefs (browser IndexedDB in WebGL, no file I/O) while tests and the
    /// dev hook use <see cref="InMemorySaveStore"/>. Plain C#, no UnityEngine dependency.
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>True if a value exists for the key.</summary>
        bool HasKey(string key);

        /// <summary>The stored string, or <paramref name="defaultValue"/> if absent.</summary>
        string GetString(string key, string defaultValue);

        /// <summary>Stores a string (may be buffered until <see cref="Flush"/>).</summary>
        void SetString(string key, string value);

        /// <summary>Removes the key if present.</summary>
        void DeleteKey(string key);

        /// <summary>Persists anything buffered (PlayerPrefs.Save for the PlayerPrefs backend).</summary>
        void Flush();
    }

    /// <summary>Dictionary-backed <see cref="ISaveStore"/> (nothing persists past the process).</summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

        /// <summary>Number of times <see cref="Flush"/> was called.</summary>
        public int FlushCount { get; private set; }

        /// <inheritdoc />
        public bool HasKey(string key) => _data.ContainsKey(key);

        /// <inheritdoc />
        public string GetString(string key, string defaultValue) =>
            _data.TryGetValue(key, out string v) ? v : defaultValue;

        /// <inheritdoc />
        public void SetString(string key, string value) => _data[key] = value;

        /// <inheritdoc />
        public void DeleteKey(string key) => _data.Remove(key);

        /// <inheritdoc />
        public void Flush() => FlushCount++;
    }
}
