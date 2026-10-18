using System;
using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>JSON (de)serialisation of <see cref="MetaSaveData"/> via <see cref="JsonUtility"/> (WebGL-safe).</summary>
    public static class MetaSaveSerializer
    {
        /// <summary>Serialises the save data to a JSON string.</summary>
        public static string ToJson(MetaSaveData data) => JsonUtility.ToJson(data);

        /// <summary>Parses JSON; false (with a reason) on empty text, a parse failure or a null result. Never throws.</summary>
        public static bool TryFromJson(string json, out MetaSaveData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json)) { error = "empty save payload"; return false; }
            try
            {
                data = JsonUtility.FromJson<MetaSaveData>(json);
            }
            catch (Exception e)
            {
                error = "JSON parse failure: " + e.Message;
                data = null;
                return false;
            }
            if (data == null) { error = "JSON parsed to null"; return false; }
            return true;
        }
    }
}
