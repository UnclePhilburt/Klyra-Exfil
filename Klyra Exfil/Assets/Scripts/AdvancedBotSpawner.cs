using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Advanced bot spawner with full control over:
/// - Movement patterns (idle/patrol/roam)
/// - Room assignments
/// - Group behaviors
/// - Spawn zones
/// </summary>
public class AdvancedBotSpawner : MonoBehaviourPun
{
    [Header("Bot Prefab")]
    [Tooltip("Bot prefab name in Resources folder")]
    public string botPrefabName = "BikerCriminal";

    [Header("Spawn Count")]
    [Tooltip("Minimum bots to spawn")]
    public int minBots = 3;
    [Tooltip("Maximum bots to spawn")]
    public int maxBots = 10;

    [Header("Spawn Zones")]
    [Tooltip("Different areas where bots can spawn")]
    public SpawnZone[] spawnZones;

    [Header("Movement Patterns")]
    [Tooltip("What % of bots should be stationary (idle)")]
    [Range(0f, 1f)]
    public float idleBotPercentage = 0.3f;

    [Tooltip("What % should patrol between waypoints")]
    [Range(0f, 1f)]
    public float patrolBotPercentage = 0.4f;

    [Tooltip("Remaining % will roam randomly")]
    public float RoamPercentage => 1f - (idleBotPercentage + patrolBotPercentage);

    [Header("Group Behavior")]
    [Tooltip("Try to spawn bots in groups (squads)")]
    public bool spawnInGroups = true;

    [Tooltip("Min size of a squad")]
    public int minGroupSize = 2;

    [Tooltip("Max size of a squad")]
    public int maxGroupSize = 4;

    [Header("AI Systems")]
    [Tooltip("Auto-add morale/fallback/surrender systems")]
    public bool autoAddAISystems = true;

    [Tooltip("Randomize bot personalities")]
    public bool randomizePersonalities = true;

    [Header("Timing")]
    [Tooltip("Delay before spawning (seconds)")]
    public float spawnDelay = 1f;

    [Tooltip("Time between each bot spawn (stagger spawns)")]
    public float timeBetweenSpawns = 0.2f;

    [Header("Debug")]
    public bool debugSpawning = true;

    [System.Serializable]
    public class SpawnZone
    {
        [Tooltip("Name of this zone (e.g., 'Building 1st Floor')")]
        public string zoneName = "Zone";

        [Tooltip("Spawn points in this zone")]
        public Transform[] spawnPoints;

        [Tooltip("Patrol waypoints for bots assigned to this zone")]
        public Transform[] patrolWaypoints;

        [Tooltip("Center point for roaming bots")]
        public Transform roamCenter;

        [Tooltip("Roam radius from center")]
        public float roamRadius = 15f;

        [Tooltip("How many bots should spawn in this zone (0 = auto-distribute)")]
        public int botCount = 0;

        [Tooltip("Priority - higher = more likely to be selected")]
        [Range(1, 10)]
        public int priority = 5;
    }

    private List<GameObject> spawnedBots = new List<GameObject>();

    void Start()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        Invoke(nameof(SpawnAllBots), spawnDelay);
    }

    public void SpawnAllBots()
    {
        if (spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogError("[AdvancedBotSpawner] No spawn zones configured!");
            return;
        }

        // Determine total bot count
        int totalBots = Random.Range(minBots, maxBots + 1);

        if (debugSpawning)
        {
            Debug.Log($"[AdvancedBotSpawner] Spawning {totalBots} bots across {spawnZones.Length} zones");
        }

        // Distribute bots across zones
        List<SpawnAssignment> assignments = DistributeBotsAcrossZones(totalBots);

        // Spawn bots
        StartCoroutine(SpawnBotsCoroutine(assignments));
    }

    List<SpawnAssignment> DistributeBotsAcrossZones(int totalBots)
    {
        List<SpawnAssignment> assignments = new List<SpawnAssignment>();
        int remainingBots = totalBots;

        // First, assign fixed counts
        foreach (var zone in spawnZones)
        {
            if (zone.botCount > 0)
            {
                int count = Mathf.Min(zone.botCount, remainingBots);
                for (int i = 0; i < count; i++)
                {
                    assignments.Add(new SpawnAssignment { zone = zone });
                }
                remainingBots -= count;
            }
        }

        // Distribute remaining bots by priority
        List<SpawnZone> availableZones = new List<SpawnZone>(spawnZones);
        while (remainingBots > 0 && availableZones.Count > 0)
        {
            SpawnZone selectedZone = SelectZoneByPriority(availableZones);
            if (selectedZone.spawnPoints != null && selectedZone.spawnPoints.Length > 0)
            {
                assignments.Add(new SpawnAssignment { zone = selectedZone });
                remainingBots--;
            }
            else
            {
                availableZones.Remove(selectedZone);
            }
        }

        if (debugSpawning)
        {
            Debug.Log($"[AdvancedBotSpawner] Total assignments created: {assignments.Count}");

            // Count by actual zone object, not name
            var zoneObjectCounts = new Dictionary<SpawnZone, int>();
            foreach (var assignment in assignments)
            {
                if (!zoneObjectCounts.ContainsKey(assignment.zone))
                    zoneObjectCounts[assignment.zone] = 0;
                zoneObjectCounts[assignment.zone]++;
            }

            Debug.Log($"[AdvancedBotSpawner] Bot distribution across {zoneObjectCounts.Count} unique zone objects:");
            int zoneIndex = 0;
            foreach (var kvp in zoneObjectCounts)
            {
                string zoneName = string.IsNullOrEmpty(kvp.Key.zoneName) ? $"Zone #{zoneIndex}" : kvp.Key.zoneName;
                Debug.Log($"[AdvancedBotSpawner]   {zoneName}: {kvp.Value} bots, {kvp.Key.spawnPoints?.Length ?? 0} spawn points");
                zoneIndex++;
            }
        }

        return assignments;
    }

    SpawnZone SelectZoneByPriority(List<SpawnZone> zones)
    {
        int totalPriority = 0;
        foreach (var zone in zones)
            totalPriority += zone.priority;

        int roll = Random.Range(0, totalPriority);
        int current = 0;

        foreach (var zone in zones)
        {
            current += zone.priority;
            if (roll < current)
                return zone;
        }

        return zones[zones.Count - 1];
    }

    IEnumerator SpawnBotsCoroutine(List<SpawnAssignment> assignments)
    {
        int botIndex = 0;
        int totalBots = assignments.Count;

        // Shuffle for variety
        Shuffle(assignments);

        // Spawn in groups if enabled
        if (spawnInGroups)
        {
            while (botIndex < totalBots)
            {
                int groupSize = Random.Range(minGroupSize, maxGroupSize + 1);
                groupSize = Mathf.Min(groupSize, totalBots - botIndex);

                if (debugSpawning)
                {
                    Debug.Log($"[AdvancedBotSpawner] Spawning group of {groupSize} bots");
                }

                // Spawn group
                for (int i = 0; i < groupSize; i++)
                {
                    SpawnBot(assignments[botIndex], botIndex + 1, totalBots);
                    botIndex++;
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }

                yield return new WaitForSeconds(0.5f); // Pause between groups
            }
        }
        else
        {
            // Spawn individually
            foreach (var assignment in assignments)
            {
                SpawnBot(assignment, botIndex + 1, totalBots);
                botIndex++;
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
        }

        if (debugSpawning)
        {
            Debug.Log($"[AdvancedBotSpawner] Finished spawning {spawnedBots.Count} bots");
        }
    }

    void SpawnBot(SpawnAssignment assignment, int botNumber, int totalBots)
    {
        SpawnZone zone = assignment.zone;

        // Pick random spawn point in zone
        if (zone.spawnPoints == null || zone.spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[AdvancedBotSpawner] Zone '{zone.zoneName}' has no spawn points!");
            return;
        }

        Transform spawnPoint = zone.spawnPoints[Random.Range(0, zone.spawnPoints.Length)];

        if (debugSpawning)
        {
            string zoneName = string.IsNullOrEmpty(zone.zoneName) ? "Unnamed Zone" : zone.zoneName;
            Debug.Log($"[AdvancedBotSpawner] Spawning bot {botNumber}/{totalBots} at {zoneName}, position: {spawnPoint.position}");
        }

        // Spawn bot
        GameObject bot = null;
        if (PhotonNetwork.IsConnected)
        {
            bot = PhotonNetwork.Instantiate(botPrefabName, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            // For offline testing - would need prefab reference
            Debug.LogWarning("[AdvancedBotSpawner] Offline spawning not implemented - use Photon mode");
            return;
        }

        if (bot == null)
        {
            Debug.LogError($"[AdvancedBotSpawner] Failed to spawn bot!");
            return;
        }

        spawnedBots.Add(bot);


        // Configure bot
        StartCoroutine(ConfigureBot(bot, zone));
    }

    IEnumerator ConfigureBot(GameObject bot, SpawnZone zone)
    {
        yield return null; // Wait for bot to initialize

        var tacticalAI = bot.GetComponent<TacticalAI>();
        if (tacticalAI == null)
        {
            Debug.LogWarning($"[AdvancedBotSpawner] Bot {bot.name} has no TacticalAI component!");
            yield break;
        }

        // ENABLE TERRITORY DEFENSE
        // This makes bots defend their spawn zone instead of aggressively chasing players
        tacticalAI.defendTerritory = true;
        tacticalAI.territoryRadius = zone.roamRadius * 1.5f; // Territory is 1.5x the roam radius
        tacticalAI.maxChaseDistance = zone.roamRadius * 2f;  // Max chase is 2x roam radius

        // IMPROVE COMBAT EFFECTIVENESS
        tacticalAI.accuracy = 0.85f; // Increased from default 0.7
        tacticalAI.fireRate = 0.15f; // Faster fire rate (was 0.5s, now 0.15s)
        tacticalAI.threatCheckInterval = 0.1f; // Check for threats more frequently (was 0.2s)
        tacticalAI.sightRange = 30f; // Increased sight range
        tacticalAI.fieldOfView = 120f; // Wider FOV

        if (debugSpawning)
        {
            Debug.Log($"[AdvancedBotSpawner] {bot.name} defending {zone.zoneName} (territory: {tacticalAI.territoryRadius:F1}m, accuracy: 0.85, fireRate: 0.15s)");
        }

        // Determine movement mode
        float roll = Random.value;
        if (roll < idleBotPercentage)
        {
            // Idle bot
            tacticalAI.movementMode = TacticalAI.MovementMode.Idle;
            if (debugSpawning)
            {
                Debug.Log($"[AdvancedBotSpawner] {bot.name} = IDLE (stationary guard)");
            }
        }
        else if (roll < idleBotPercentage + patrolBotPercentage)
        {
            // Patrol bot
            tacticalAI.movementMode = TacticalAI.MovementMode.Patrol;
            if (zone.patrolWaypoints != null && zone.patrolWaypoints.Length > 0)
            {
                tacticalAI.patrolWaypoints = zone.patrolWaypoints;
                if (debugSpawning)
                {
                    Debug.Log($"[AdvancedBotSpawner] {bot.name} = PATROL ({zone.patrolWaypoints.Length} waypoints in {zone.zoneName})");
                }
            }
            else
            {
                // No waypoints - fall back to roam
                tacticalAI.movementMode = TacticalAI.MovementMode.Roam;
                if (debugSpawning)
                {
                    Debug.LogWarning($"[AdvancedBotSpawner] {bot.name} wanted PATROL but no waypoints in {zone.zoneName}, using ROAM instead");
                }
            }
        }
        else
        {
            // Roam bot
            tacticalAI.movementMode = TacticalAI.MovementMode.Roam;
            tacticalAI.roamRadius = zone.roamRadius;

            // If zone has roam center, use it
            if (zone.roamCenter != null)
            {
                // Set spawn position for roam radius calculation
                bot.transform.position = zone.roamCenter.position;
            }

            if (debugSpawning)
            {
                Debug.Log($"[AdvancedBotSpawner] {bot.name} = ROAM (radius {zone.roamRadius}m in {zone.zoneName})");
            }
        }

        // Add AI systems
        if (autoAddAISystems)
        {
            AddAISystems(bot);
        }
    }

    void AddAISystems(GameObject bot)
    {
        // Add Simple Morale System (new probability-based system)
        var simpleMorale = bot.GetComponent<Klyra.AI.SimpleMoraleSystem>();
        if (simpleMorale == null)
        {
            simpleMorale = bot.AddComponent<Klyra.AI.SimpleMoraleSystem>();
        }

        // Add Room Awareness
        var roomAwareness = bot.GetComponent<Klyra.AI.AIRoomAwareness>();
        if (roomAwareness == null)
        {
            roomAwareness = bot.AddComponent<Klyra.AI.AIRoomAwareness>();
        }

        // OLD SYSTEMS DISABLED - Using SimpleMoraleSystem instead
        // No more: AIMoraleSystem, AIFallbackSystem, AISurrenderAnimation
    }

    void RandomizePersonality(Klyra.AI.AIMoraleSystem morale, Klyra.AI.AIFallbackSystem fallback, string botName)
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

    private class SpawnAssignment
    {
        public SpawnZone zone;
    }

    void OnDrawGizmos()
    {
        if (spawnZones == null) return;

        foreach (var zone in spawnZones)
        {
            if (zone.spawnPoints == null) continue;

            // Draw spawn points
            Gizmos.color = Color.green;
            foreach (var point in zone.spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(point.position, point.position + point.forward);
                }
            }

            // Draw patrol waypoints
            if (zone.patrolWaypoints != null && zone.patrolWaypoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < zone.patrolWaypoints.Length; i++)
                {
                    if (zone.patrolWaypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(zone.patrolWaypoints[i].position, 0.3f);

                        // Draw line to next waypoint
                        if (i < zone.patrolWaypoints.Length - 1 && zone.patrolWaypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(zone.patrolWaypoints[i].position, zone.patrolWaypoints[i + 1].position);
                        }
                    }
                }
            }

            // Draw roam radius
            if (zone.roamCenter != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawWireSphere(zone.roamCenter.position, zone.roamRadius);
            }
        }
    }
}
