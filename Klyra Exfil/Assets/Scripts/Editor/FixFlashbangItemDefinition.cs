using UnityEngine;
using UnityEditor;
using Opsive.UltimateCharacterController.Inventory;

namespace Klyra.Editor
{
    /// <summary>
    /// Editor utility to fix the Flashbang ItemDefinition prefab reference.
    /// The Flashbang ItemType should reference FlashbangWeaponRightPun (the character item),
    /// not FlashbangProjectilePun (the throwable projectile).
    /// </summary>
    public class FixFlashbangItemDefinition : EditorWindow
    {
        private ItemCollection itemCollection;
        private GameObject flashbangWeaponPrefab;

        [MenuItem("Klyra/Fix Flashbang Item Definition")]
        public static void ShowWindow()
        {
            GetWindow<FixFlashbangItemDefinition>("Fix Flashbang");
        }

        private void OnGUI()
        {
            GUILayout.Label("Fix Flashbang ItemDefinition", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool fixes the Flashbang ItemDefinition to reference the correct prefab.\n\n" +
                "The Flashbang should reference 'FlashbangWeaponRightPun' (what the player holds), " +
                "not 'FlashbangProjectilePun' (what gets thrown).",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Let user assign the ItemCollection
            itemCollection = (ItemCollection)EditorGUILayout.ObjectField(
                "Item Collection",
                itemCollection,
                typeof(ItemCollection),
                false
            );

            // Let user assign the correct weapon prefab
            flashbangWeaponPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Flashbang Weapon Prefab",
                flashbangWeaponPrefab,
                typeof(GameObject),
                false
            );

            GUILayout.Space(10);

            // Auto-load button
            if (GUILayout.Button("Auto-Load Assets"))
            {
                AutoLoadAssets();
            }

            GUILayout.Space(10);

            // Show current status
            if (itemCollection != null)
            {
                ShowCurrentStatus();
            }

            GUILayout.Space(10);

            // Fix button
            GUI.enabled = itemCollection != null && flashbangWeaponPrefab != null;
            if (GUILayout.Button("Fix Flashbang ItemDefinition", GUILayout.Height(40)))
            {
                FixFlashbang();
            }
            GUI.enabled = true;
        }

        private void AutoLoadAssets()
        {
            // Try to find PunItemCollection
            string[] guids = AssetDatabase.FindAssets("PunItemCollection t:ItemCollection");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                itemCollection = AssetDatabase.LoadAssetAtPath<ItemCollection>(path);
                Debug.Log($"[FixFlashbang] Loaded ItemCollection from: {path}");
            }
            else
            {
                Debug.LogWarning("[FixFlashbang] Could not find PunItemCollection asset");
            }

            // Try to find FlashbangWeaponRightPun
            guids = AssetDatabase.FindAssets("FlashbangWeaponRightPun t:GameObject");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                flashbangWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[FixFlashbang] Loaded weapon prefab from: {path}");
            }
            else
            {
                Debug.LogWarning("[FixFlashbang] Could not find FlashbangWeaponRightPun prefab");
            }

            Repaint();
        }

        private void ShowCurrentStatus()
        {
            var itemTypes = itemCollection.ItemTypes;
            if (itemTypes == null) return;

            foreach (var itemType in itemTypes)
            {
                if (itemType == null || itemType.name != "Flashbang") continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Current Flashbang Configuration:", EditorStyles.boldLabel);

                var prefabs = itemType.Prefabs;
                if (prefabs == null || prefabs.Length == 0)
                {
                    EditorGUILayout.HelpBox("NO PREFABS ASSIGNED - This is the problem!", MessageType.Error);
                }
                else
                {
                    for (int i = 0; i < prefabs.Length; i++)
                    {
                        var prefab = prefabs[i];
                        if (prefab == null)
                        {
                            EditorGUILayout.HelpBox($"Index {i}: NULL PREFAB - This is the problem!", MessageType.Error);
                        }
                        else
                        {
                            string prefabName = prefab.name;
                            MessageType msgType = prefabName.Contains("Weapon") ? MessageType.Info : MessageType.Warning;
                            EditorGUILayout.HelpBox($"Index {i}: {prefabName}", msgType);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.HelpBox("Flashbang ItemType not found in collection", MessageType.Warning);
        }

        private void FixFlashbang()
        {
            if (itemCollection == null)
            {
                Debug.LogError("[FixFlashbang] ItemCollection is null!");
                return;
            }

            if (flashbangWeaponPrefab == null)
            {
                Debug.LogError("[FixFlashbang] FlashbangWeaponPrefab is null!");
                return;
            }

            var itemTypes = itemCollection.ItemTypes;
            if (itemTypes == null)
            {
                Debug.LogError("[FixFlashbang] ItemCollection has no ItemTypes!");
                return;
            }

            // Find the Flashbang ItemType
            ItemType flashbangItemType = null;
            foreach (var itemType in itemTypes)
            {
                if (itemType != null && itemType.name == "Flashbang")
                {
                    flashbangItemType = itemType;
                    break;
                }
            }

            if (flashbangItemType == null)
            {
                Debug.LogError("[FixFlashbang] Could not find Flashbang ItemType in collection!");
                return;
            }

            // Mark the asset as dirty so changes are saved
            EditorUtility.SetDirty(itemCollection);
            EditorUtility.SetDirty(flashbangItemType);

            // Get the current prefabs array
            var currentPrefabs = flashbangItemType.Prefabs;

            // Create a new array with the correct prefab
            GameObject[] newPrefabs = new GameObject[1];
            newPrefabs[0] = flashbangWeaponPrefab;

            // Use reflection to set the prefabs array since it might be private
            var field = typeof(ItemType).GetField("m_Prefabs",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(flashbangItemType, newPrefabs);
                Debug.Log($"[FixFlashbang] Successfully set Flashbang prefab to: {flashbangWeaponPrefab.name}");
            }
            else
            {
                // Fallback: try public property
                var property = typeof(ItemType).GetProperty("Prefabs");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(flashbangItemType, newPrefabs);
                    Debug.Log($"[FixFlashbang] Successfully set Flashbang prefab to: {flashbangWeaponPrefab.name}");
                }
                else
                {
                    Debug.LogError("[FixFlashbang] Could not find m_Prefabs field or Prefabs property!");
                    return;
                }
            }

            // Save the changes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FixFlashbang] FIX COMPLETE! Flashbang ItemDefinition has been updated.");
            Debug.Log("[FixFlashbang] The Flashbang should now work correctly in your loadout system.");

            // Refresh the UI
            Repaint();
        }
    }
}
