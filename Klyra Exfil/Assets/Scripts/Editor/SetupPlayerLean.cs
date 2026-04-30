using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to automatically add and configure PlayerLean component
/// on all player prefabs in Resources folder.
///
/// Usage: Tools → Klyra → Setup Player Lean
/// </summary>
public class SetupPlayerLean : MonoBehaviour
{
    [MenuItem("Tools/Klyra/Setup Player Lean")]
    public static void AddLeanToPlayers()
    {
        // Find all player prefabs in Resources folder
        string[] playerPrefabNames = new string[]
        {
            "Swat",
            "Sheriff"
        };

        int successCount = 0;
        int skipCount = 0;

        foreach (string prefabName in playerPrefabNames)
        {
            // Load prefab
            GameObject prefab = Resources.Load<GameObject>(prefabName);

            if (prefab == null)
            {
                Debug.LogWarning($"[SetupPlayerLean] Could not find prefab: {prefabName}");
                continue;
            }

            // Check if already has PlayerLean
            PlayerLean existingLean = prefab.GetComponent<PlayerLean>();
            if (existingLean != null)
            {
                Debug.Log($"[SetupPlayerLean] {prefabName} already has PlayerLean - skipping");
                skipCount++;
                continue;
            }

            // Load prefab for editing
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabInstance == null)
            {
                Debug.LogError($"[SetupPlayerLean] Could not load prefab for editing: {prefabName}");
                continue;
            }

            // Add PlayerLean component
            PlayerLean lean = prefabInstance.AddComponent<PlayerLean>();

            // Configure optimal settings
            lean.autoFindBones = true;
            lean.spineLeanAngle = 15f;
            lean.upperSpineLeanAngle = 8f;
            lean.leanSpeed = 8f;
            lean.usePositionOffset = true;
            lean.positionOffset = 0.2f;
            lean.showDebug = true;

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabInstance);

            Debug.Log($"[SetupPlayerLean] ✓ Added PlayerLean to {prefabName}");
            successCount++;
        }

        // Show summary
        Debug.Log($"[SetupPlayerLean] === COMPLETE ===");
        Debug.Log($"[SetupPlayerLean] Added: {successCount}");
        Debug.Log($"[SetupPlayerLean] Skipped (already had lean): {skipCount}");

        if (successCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Player Lean Setup Complete",
                $"Successfully added PlayerLean to {successCount} player prefab(s)!\n\n" +
                "Settings configured:\n" +
                "• Spine Lean: 15°\n" +
                "• Upper Spine: 8°\n" +
                "• Lean Speed: 8\n" +
                "• Position Offset: 0.2m\n\n" +
                "Controls: Q = lean left, E = lean right",
                "OK"
            );
        }

        AssetDatabase.Refresh();
    }
}
