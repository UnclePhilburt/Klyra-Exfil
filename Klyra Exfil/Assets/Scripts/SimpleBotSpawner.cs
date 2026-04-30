using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simple bot spawner - just put empty GameObjects where you want bots to spawn.
/// Each spawn point can optionally have patrol waypoints as children.
/// Super easy to use!
/// </summary>
public class SimpleBotSpawner : MonoBehaviourPun
{
    [Header("Bot Settings")]
    [Tooltip("Bot prefab name in Resources folder")]
    public string botPrefabName = "BikerCriminal";

    [Header("How Many Bots")]
    [Tooltip("Minimum bots to spawn")]
    public int minBots = 5;

    [Tooltip("Maximum bots to spawn")]
    public int maxBots = 10;

    [Header("Spawn Points - Just Drag Empty GameObjects Here")]
    [Tooltip("Empty GameObjects positioned where you want bots to spawn")]
    public Transform[] spawnPoints;

    [Header("Movement Behavior")]
    [Tooltip("What % of bots stand still and guard")]
    [Range(0f, 1f)]
    public float percentIdle = 0.3f;

    [Tooltip("What % of bots patrol (if they have waypoints)")]
    [Range(0f, 1f)]
    public float percentPatrol = 0.4f;

    [Tooltip("Remaining % will roam around randomly")]
    public float PercentRoam => 1f - (percentIdle + percentPatrol);

    [Header("Optional Settings")]
    [Tooltip("Spawn bots in groups/squads?")]
    public bool spawnInSquads = true;

    [Tooltip("Add morale/fallback/surrender systems to bots?")]
    public bool addAISystems = true;

    [Tooltip("Randomize bot personalities?")]
    public bool randomizePersonalities = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private List<GameObject> spawnedBots = new List<GameObject>();

    void Start()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        Invoke(nameof(SpawnAllBots), 1f);
    }

    public void SpawnAllBots()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[SimpleBotSpawner] No spawn points assigned! Add empty GameObjects to Spawn Points array.");
            return;
        }

        // Decide how many bots
        int totalBots = Random.Range(minBots, maxBots + 1);

        if (showDebugLogs)
        {
            Debug.Log($"[SimpleBotSpawner] Spawning {totalBots} bots at {spawnPoints.Length} spawn points");
        }

        // Shuffle spawn points for variety
        List<Transform> shuffledSpawns = new List<Transform>(spawnPoints);
        Shuffle(shuffledSpawns);

        // Spawn bots
        StartCoroutine(SpawnBotsCoroutine(shuffledSpawns, totalBots));
    }

    IEnumerator SpawnBotsCoroutine(List<Transform> spawns, int totalBots)
    {
        int botsSpawned = 0;

        while (botsSpawned < totalBots)
        {
            // Pick spawn point (cycle through if more bots than spawn points)
            Transform spawnPoint = spawns[botsSpawned % spawns.Count];

            // Spawn bot
            GameObject bot = null;
            if (PhotonNetwork.IsConnected)
            {
                bot = PhotonNetwork.Instantiate(botPrefabName, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                Debug.LogWarning("[SimpleBotSpawner] Not connected to Photon - skipping spawn");
                yield break;
            }

            if (bot != null)
            {
                spawnedBots.Add(bot);

                // Configure bot behavior
                ConfigureBot(bot, spawnPoint);

                if (showDebugLogs)
                {
                    Debug.Log($"[SimpleBotSpawner] Spawned bot {botsSpawned + 1}/{totalBots} at {spawnPoint.name}");
                }

                botsSpawned++;

                // Small delay between spawns
                yield return new WaitForSeconds(0.2f);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[SimpleBotSpawner] Finished spawning {spawnedBots.Count} bots");
        }
    }

    void ConfigureBot(GameObject bot, Transform spawnPoint)
    {
        var tacticalAI = bot.GetComponent<TacticalAI>();
        if (tacticalAI == null)
        {
            Debug.LogWarning($"[SimpleBotSpawner] {bot.name} has no TacticalAI component!");
            return;
        }

        // Enable territory defense
        tacticalAI.defendTerritory = true;
        tacticalAI.territoryRadius = 25f;
        tacticalAI.maxChaseDistance = 35f;

        // Decide movement behavior
        float roll = Random.value;

        if (roll < percentIdle)
        {
            // Stand guard
            tacticalAI.movementMode = TacticalAI.MovementMode.Idle;

            if (showDebugLogs)
            {
                Debug.Log($"[SimpleBotSpawner] {bot.name} = GUARD (stationary)");
            }
        }
        else if (roll < percentIdle + percentPatrol)
        {
            // Check if spawn point has waypoints as children
            Transform[] waypoints = GetWaypointsFromSpawnPoint(spawnPoint);

            if (waypoints != null && waypoints.Length > 0)
            {
                // Patrol the waypoints
                tacticalAI.movementMode = TacticalAI.MovementMode.Patrol;
                tacticalAI.patrolWaypoints = waypoints;

                if (showDebugLogs)
                {
                    Debug.Log($"[SimpleBotSpawner] {bot.name} = PATROL ({waypoints.Length} waypoints)");
                }
            }
            else
            {
                // No waypoints, roam instead
                tacticalAI.movementMode = TacticalAI.MovementMode.Roam;
                tacticalAI.roamRadius = 20f;

                if (showDebugLogs)
                {
                    Debug.Log($"[SimpleBotSpawner] {bot.name} = ROAM (no waypoints found)");
                }
            }
        }
        else
        {
            // Roam around
            tacticalAI.movementMode = TacticalAI.MovementMode.Roam;
            tacticalAI.roamRadius = 20f;

            if (showDebugLogs)
            {
                Debug.Log($"[SimpleBotSpawner] {bot.name} = ROAM (random movement)");
            }
        }

        // Add AI systems if enabled
        if (addAISystems)
        {
            AddAISystems(bot);
        }
    }

    /// <summary>
    /// Look for child objects of spawn point named "Waypoint" to use as patrol route
    /// </summary>
    Transform[] GetWaypointsFromSpawnPoint(Transform spawnPoint)
    {
        List<Transform> waypoints = new List<Transform>();

        // Look for children with "Waypoint" in the name
        foreach (Transform child in spawnPoint)
        {
            if (child.name.ToLower().Contains("waypoint") || child.name.ToLower().Contains("patrol"))
            {
                waypoints.Add(child);
            }
        }

        return waypoints.Count > 0 ? waypoints.ToArray() : null;
    }

    void AddAISystems(GameObject bot)
    {
        // TEMPORARILY DISABLED - Morale system too aggressive
        // TODO: Re-enable after balancing morale loss rates
        /*
        // Add Morale System
        var moraleSystem = bot.GetComponent<Klyra.AI.AIMoraleSystem>();
        if (moraleSystem == null)
        {
            moraleSystem = bot.AddComponent<Klyra.AI.AIMoraleSystem>();
        }

        // Add Fallback System
        var fallbackSystem = bot.GetComponent<Klyra.AI.AIFallbackSystem>();
        if (fallbackSystem == null)
        {
            fallbackSystem = bot.AddComponent<Klyra.AI.AIFallbackSystem>();
        }

        // Add Surrender Animation
        var surrenderAnim = bot.GetComponent<AISurrenderAnimation>();
        if (surrenderAnim == null)
        {
            surrenderAnim = bot.AddComponent<AISurrenderAnimation>();
        }

        // Randomize personalities if enabled
        if (randomizePersonalities && moraleSystem != null)
        {
            RandomizePersonality(moraleSystem, fallbackSystem);
        }
        */

        // Add Room Awareness (ONLY system we're keeping for now)
        var roomAwareness = bot.GetComponent<Klyra.AI.AIRoomAwareness>();
        if (roomAwareness == null)
        {
            roomAwareness = bot.AddComponent<Klyra.AI.AIRoomAwareness>();
        }
    }

    void RandomizePersonality(Klyra.AI.AIMoraleSystem morale, Klyra.AI.AIFallbackSystem fallback)
    {
        float roll = Random.value;

        if (roll < 0.2f) // Coward
        {
            morale.surrenderMoraleThreshold = Random.Range(40f, 50f);
            morale.panicMoraleThreshold = Random.Range(15f, 25f);
            if (fallback) fallback.fallbackMoraleThreshold = Random.Range(55f, 65f);
        }
        else if (roll < 0.5f) // Normal
        {
            morale.surrenderMoraleThreshold = Random.Range(25f, 35f);
            morale.panicMoraleThreshold = Random.Range(8f, 12f);
            if (fallback) fallback.fallbackMoraleThreshold = Random.Range(45f, 55f);
        }
        else if (roll < 0.8f) // Brave
        {
            morale.surrenderMoraleThreshold = Random.Range(15f, 25f);
            morale.panicMoraleThreshold = Random.Range(5f, 10f);
            if (fallback) fallback.fallbackMoraleThreshold = Random.Range(35f, 45f);
        }
        else // Fearless
        {
            morale.surrenderMoraleThreshold = Random.Range(10f, 20f);
            morale.panicMoraleThreshold = Random.Range(2f, 8f);
            if (fallback) fallback.fallbackMoraleThreshold = Random.Range(25f, 35f);
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // Show spawn points in editor
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.green;
        foreach (var spawn in spawnPoints)
        {
            if (spawn != null)
            {
                // Draw spawn point
                Gizmos.DrawWireSphere(spawn.position, 0.5f);
                Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 2f);

                // Draw waypoints if any
                Transform[] waypoints = GetWaypointsFromSpawnPoint(spawn);
                if (waypoints != null && waypoints.Length > 0)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 0; i < waypoints.Length; i++)
                    {
                        if (waypoints[i] != null)
                        {
                            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

                            // Draw line to next waypoint
                            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                            {
                                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                            }
                        }
                    }
                    Gizmos.color = Color.green;
                }
            }
        }
    }
}
