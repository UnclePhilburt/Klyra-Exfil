using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to disable Opsive's SingleCharacterSpawnManager and setup the custom PlayerSpawner
/// </summary>
public class SetupPlayerSpawner : EditorWindow
{
    [MenuItem("Tools/Setup Custom Player Spawner")]
    public static void SetupSpawner()
    {
        // Find the PunGame object with Opsive's spawner
        GameObject punGame = GameObject.Find("PunGame");
        if (punGame != null)
        {
            // Disable all components that have "SpawnManager" in their type name
            var components = punGame.GetComponents<MonoBehaviour>();
            bool foundOpsiveSpawner = false;
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name.Contains("SpawnManager"))
                {
                    component.enabled = false;
                    foundOpsiveSpawner = true;
                    Debug.Log($"Disabled {component.GetType().Name} on PunGame");
                }
            }

            if (!foundOpsiveSpawner)
            {
                Debug.LogWarning("No SpawnManager component found on PunGame");
            }
        }
        else
        {
            Debug.LogWarning("PunGame GameObject not found in scene. Make sure CrimeHouse scene is loaded.");
        }

        // Find existing PlayerSpawner or create new one
        GameObject spawnerObj = GameObject.Find("PlayerSpawner");
        if (spawnerObj == null)
        {
            spawnerObj = new GameObject("PlayerSpawner");
            Debug.Log("Created new PlayerSpawner GameObject");
        }

        // Add PlayerSpawner component if not present
        PlayerSpawner spawner = spawnerObj.GetComponent<PlayerSpawner>();
        if (spawner == null)
        {
            spawner = spawnerObj.AddComponent<PlayerSpawner>();
            Debug.Log("Added PlayerSpawner component");
        }

        // Try to find spawn points in the scene
        Transform spawnLocation = GameObject.Find("SpawnLocation")?.transform;
        if (spawnLocation != null)
        {
            spawner.spawnPoints = new Transform[] { spawnLocation };
            Debug.Log("Assigned SpawnLocation to PlayerSpawner");
        }

        // Set default character - you can change this in the inspector
        if (string.IsNullOrEmpty(spawner.playerPrefabName))
        {
            spawner.playerPrefabName = "Swat";
            Debug.Log("Set default player prefab to 'Swat'");
        }

        // Mark scene as dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("PlayerSpawner setup complete! Make sure to save the scene.");

        // Select the spawner so you can see it in the inspector
        Selection.activeGameObject = spawnerObj;
    }
}
