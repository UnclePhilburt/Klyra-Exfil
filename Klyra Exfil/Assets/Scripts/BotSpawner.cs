using UnityEngine;
using Photon.Pun;

/// <summary>
/// Spawns a random number of AI bots at designated spawn points
/// </summary>
public class BotSpawner : MonoBehaviour
{
    [Header("Bot Settings")]
    [Tooltip("Bot prefab to spawn (must be in Resources folder)")]
    public string botPrefabName = "BikerCriminal";

    [Header("Spawn Amount Randomization")]
    [Tooltip("Minimum number of bots to spawn (can be 0)")]
    public int minBots = 0;

    [Tooltip("Maximum number of bots to spawn")]
    public int maxBots = 5;

    [Header("Spawn Points")]
    [Tooltip("Locations where bots can spawn")]
    public Transform[] spawnPoints;

    [Tooltip("Should spawn points be reused if there are more bots than spawn points?")]
    public bool reuseSpawnPoints = true;

    void Start()
    {
        // Wait a bit for scene to fully load before spawning
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(SpawnBots), 1f);
        }
    }

    void SpawnBots()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("BotSpawner: No spawn points assigned!");
            return;
        }

        // Randomly decide how many bots to spawn
        int botCount = Random.Range(minBots, maxBots + 1);
        Debug.Log($"BotSpawner: Spawning {botCount} bots");

        // If no bots, we're done
        if (botCount == 0)
        {
            Debug.Log("BotSpawner: Random roll resulted in 0 bots - no enemies this round!");
            return;
        }

        // Shuffle spawn points for variety
        Transform[] shuffledSpawns = (Transform[])spawnPoints.Clone();
        ShuffleArray(shuffledSpawns);

        // Spawn bots
        for (int i = 0; i < botCount; i++)
        {
            Transform spawnPoint;

            if (reuseSpawnPoints)
            {
                // Wrap around if more bots than spawn points
                spawnPoint = shuffledSpawns[i % shuffledSpawns.Length];
            }
            else
            {
                // Stop if we run out of spawn points
                if (i >= shuffledSpawns.Length)
                {
                    Debug.LogWarning($"BotSpawner: Not enough spawn points! Only spawned {i} of {botCount} bots");
                    break;
                }
                spawnPoint = shuffledSpawns[i];
            }

            // Spawn the bot
            Vector3 position = spawnPoint.position;
            Quaternion rotation = spawnPoint.rotation;

            GameObject bot = PhotonNetwork.Instantiate(botPrefabName, position, rotation);
            if (bot != null)
            {
                Debug.Log($"Bot {i + 1}/{botCount} spawned at {spawnPoint.name}");
            }
            else
            {
                Debug.LogError($"Failed to spawn bot! Make sure '{botPrefabName}' is in Resources folder");
            }
        }
    }

    void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    // Helper to visualize spawn points in editor
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (var spawn in spawnPoints)
        {
            if (spawn != null)
            {
                Gizmos.DrawWireSphere(spawn.position, 0.5f);
                Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 1f);
            }
        }
    }
}
