using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// <see cref="ISaveStore"/> over <see cref="PlayerPrefs"/> (Story 007): Windows registry on standalone, the browser's
    /// IndexedDB in a WebGL build. <see cref="Flush"/> calls <see cref="PlayerPrefs.Save"/>.
    /// </summary>
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        /// <inheritdoc />
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        /// <inheritdoc />
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);

        /// <inheritdoc />
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        /// <inheritdoc />
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        /// <inheritdoc />
        public void Flush() => PlayerPrefs.Save();
    }
}
