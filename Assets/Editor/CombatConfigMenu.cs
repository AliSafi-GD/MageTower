using UnityEditor;
using UnityEngine;
using ArcaneSpire.Combat;

namespace ArcaneSpire.EditorTools
{
    /// <summary>Editor menu to create Assets/Resources/CombatConfig.asset with default values.</summary>
    public static class CombatConfigMenu
    {
        private const string AssetPath = "Assets/Resources/CombatConfig.asset";

        /// <summary>Creates the config asset if it does not exist, then selects it.</summary>
        [MenuItem("Arcane Spire/Create Combat Config")]
        public static void CreateCombatConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CombatConfig>(AssetPath);
            if (existing == null)
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
                existing = ScriptableObject.CreateInstance<CombatConfig>();
                AssetDatabase.CreateAsset(existing, AssetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = existing;
        }
    }
}
