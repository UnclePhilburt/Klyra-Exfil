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

    [Header("AI Systems")]
    [Tooltip("Automatically add morale/fallback/surrender systems to spawned bots")]
    public bool autoAddAISystems = true;

    [Tooltip("Randomize morale settings for variety (each bot has different courage levels)")]
    public bool randomizeMoraleSettings = true;

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

                // Enable territory defense and improve combat effectiveness
                var tacticalAI = bot.GetComponent<TacticalAI>();
                if (tacticalAI != null)
                {
                    tacticalAI.defendTerritory = true;
                    tacticalAI.territoryRadius = 25f; // Default territory
                    tacticalAI.maxChaseDistance = 35f;

                    // IMPROVE COMBAT EFFECTIVENESS
                    tacticalAI.accuracy = 0.85f; // Increased from default 0.7
                    tacticalAI.fireRate = 0.15f; // Faster fire rate (was 0.5s, now 0.15s)
                    tacticalAI.threatCheckInterval = 0.1f; // Check for threats more frequently (was 0.2s)
                    tacticalAI.sightRange = 30f; // Increased sight range
                    tacticalAI.fieldOfView = 120f; // Wider FOV

                    Debug.Log($"[BotSpawner] {bot.name} configured: accuracy=0.85, fireRate=0.15s, sightRange=30m, FOV=120°");
                }

                // Add AI systems if enabled
                if (autoAddAISystems)
                {
                    SetupAISystems(bot);
                }
            }
            else
            {
                Debug.LogError($"Failed to spawn bot! Make sure '{botPrefabName}' is in Resources folder");
            }
        }
    }

    void SetupAISystems(GameObject bot)
    {
        // Add Simple Morale System (new probability-based system)
        var simpleMorale = bot.GetComponent<Klyra.AI.SimpleMoraleSystem>();
        if (simpleMorale == null)
        {
            simpleMorale = bot.AddComponent<Klyra.AI.SimpleMoraleSystem>();
            Debug.Log($"Added SimpleMoraleSystem to {bot.name}");
        }

        // Add Room Awareness
        var roomAwareness = bot.GetComponent<Klyra.AI.AIRoomAwareness>();
        if (roomAwareness == null)
        {
            roomAwareness = bot.AddComponent<Klyra.AI.AIRoomAwareness>();
            Debug.Log($"Added AIRoomAwareness to {bot.name}");
        }

        // OLD SYSTEMS REMOVED - Using SimpleMoraleSystem instead
        // No more: AIMoraleSystem, AIFallbackSystem, AISurrenderAnimation
    }

    void RandomizeBotMorale(Klyra.AI.AIMoraleSystem morale, Klyra.AI.AIFallbackSystem fallback)
    {
        // Create personality types
        float personalityRoll = Random.value;

        if (personalityRoll < 0.2f) // 20% - Coward
        {
            morale.surrenderMoraleThreshold = Random.Range(40f, 50f);
            morale.panicMoraleThreshold = Random.Range(15f, 25f);
            if (fallback != null)
            {
                fallback.fallbackMoraleThreshold = Random.Range(55f, 65f);
                fallback.hitsBeforeFallback = Random.Range(2, 3);
            }
            Debug.Log($"{morale.gameObject.name}: Personality = COWARD (easily scared)");
        }
        else if (personalityRoll < 0.5f) // 30% - Normal
        {
            morale.surrenderMoraleThreshold = Random.Range(25f, 35f);
            morale.panicMoraleThreshold = Random.Range(8f, 12f);
            if (fallback != null)
            {
                fallback.fallbackMoraleThreshold = Random.Range(45f, 55f);
                fallback.hitsBeforeFallback = 3;
            }
            Debug.Log($"{morale.gameObject.name}: Personality = NORMAL (balanced)");
        }
        else if (personalityRoll < 0.8f) // 30% - Brave
        {
            morale.surrenderMoraleThreshold = Random.Range(15f, 25f);
            morale.panicMoraleThreshold = Random.Range(5f, 10f);
            if (fallback != null)
            {
                fallback.fallbackMoraleThreshold = Random.Range(35f, 45f);
                fallback.hitsBeforeFallback = Random.Range(4, 5);
            }
            Debug.Log($"{morale.gameObject.name}: Personality = BRAVE (tough fighter)");
        }
        else // 20% - Fearless
        {
            morale.surrenderMoraleThreshold = Random.Range(10f, 20f);
            morale.panicMoraleThreshold = Random.Range(2f, 8f);
            if (fallback != null)
            {
                fallback.fallbackMoraleThreshold = Random.Range(25f, 35f);
                fallback.hitsBeforeFallback = Random.Range(5, 7);
            }
            Debug.Log($"{morale.gameObject.name}: Personality = FEARLESS (will fight to the death)");
        }

        // Randomize other morale factors slightly
        morale.moraleLossOnDamage = Random.Range(3f, 7f);
        morale.moraleLossOnAllyDeath = Random.Range(12f, 18f);
        morale.moraleLossWhenIsolated = Random.Range(1.5f, 2.5f);
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
