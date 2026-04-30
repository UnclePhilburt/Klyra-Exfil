using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Smart enemy spawner for Ready or Not style tactical shooter.
/// Spawns enemies at random positions across the entire NavMesh when the scene loads.
/// - Won't spawn in player's line of sight
/// - Creates mix of solo enemies and groups
/// - Works in multiplayer (only master client spawns)
/// </summary>
public class RandomNavMeshSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The enemy prefab to spawn (must be in a Resources folder for multiplayer)")]
    public GameObject enemyPrefab;

    [Tooltip("Minimum number of enemies to spawn")]
    public int minSpawnCount = 8;

    [Tooltip("Maximum number of enemies to spawn")]
    public int maxSpawnCount = 15;

    [Header("Group Settings")]
    [Tooltip("Percentage of enemies that should spawn in groups (0-1). Rest spawn solo.")]
    [Range(0f, 1f)]
    public float groupSpawnChance = 0.6f;

    [Tooltip("Minimum enemies per group")]
    [Range(2, 5)]
    public int minGroupSize = 2;

    [Tooltip("Maximum enemies per group")]
    [Range(2, 8)]
    public int maxGroupSize = 4;

    [Tooltip("Maximum distance between group members")]
    public float groupSpreadRadius = 5f;

    [Header("Player Visibility Settings")]
    [Tooltip("Minimum distance from any player to spawn enemies")]
    public float minDistanceFromPlayer = 15f;

    [Tooltip("Check line of sight to players (won't spawn if player can see the position)")]
    public bool checkLineOfSight = true;

    [Tooltip("Layers that block line of sight (walls, floors, etc)")]
    public LayerMask lineOfSightBlockers = ~0;

    [Header("Spacing Settings")]
    [Tooltip("Minimum distance between solo enemies or different groups")]
    public float minDistanceBetweenSpawns = 10f;

    [Header("NavMesh Settings")]
    [Tooltip("Maximum number of attempts to find a valid spawn position")]
    public int maxAttempts = 200;

    [Header("Multiplayer")]
    [Tooltip("If true, uses PhotonNetwork.Instantiate for multiplayer. If false, uses regular Instantiate.")]
    public bool usePhotonNetworking = true;

    [Tooltip("Path to the prefab in Resources folder (for Photon). Example: 'Enemies/BotEnemy'")]
    public string photonPrefabPath = "";

    [Header("Timing")]
    [Tooltip("Delay in seconds before spawning enemies (gives players time to spawn first)")]
    public float spawnDelay = 2f;

    [Header("Debug")]
    [Tooltip("Show debug information in the console")]
    public bool showDebug = true;

    [Tooltip("Show spawn positions as gizmos in scene view")]
    public bool showSpawnGizmos = true;

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<Vector3> debugSpawnPositions = new List<Vector3>();

    private void Start()
    {
        // In multiplayer, only the master client should spawn enemies
        if (usePhotonNetworking && !PhotonNetwork.IsMasterClient)
        {
            if (showDebug) Debug.Log("[RandomNavMeshSpawner] Not master client, skipping spawn.");
            return;
        }

        // Delay spawning to let players spawn first
        if (spawnDelay > 0f)
        {
            Invoke("SpawnEnemies", spawnDelay);
        }
        else
        {
            SpawnEnemies();
        }
    }

    private void SpawnEnemies()
    {
        // Calculate NavMesh bounds
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        if (navMeshData.vertices.Length == 0)
        {
            Debug.LogError("[RandomNavMeshSpawner] No NavMesh found in the scene! Please bake a NavMesh first.");
            return;
        }

        Bounds navMeshBounds = CalculateNavMeshBounds(navMeshData);

        // Pick a random spawn count within the range
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        if (showDebug)
        {
            Debug.Log($"[RandomNavMeshSpawner] NavMesh bounds: Center={navMeshBounds.center}, Size={navMeshBounds.size}");
            Debug.Log($"[RandomNavMeshSpawner] Spawning {spawnCount} enemies (range: {minSpawnCount}-{maxSpawnCount})");
        }

        int remainingToSpawn = spawnCount;
        int successfulSpawns = 0;
        int totalAttempts = 0;

        // Determine how many should spawn in groups vs solo
        int enemiesToGroup = Mathf.RoundToInt(spawnCount * groupSpawnChance);
        int soloEnemies = spawnCount - enemiesToGroup;

        if (showDebug)
        {
            Debug.Log($"[RandomNavMeshSpawner] Planning: {soloEnemies} solo, {enemiesToGroup} in groups");
        }

        // Spawn groups first
        while (enemiesToGroup > 0 && totalAttempts < maxAttempts * 2)
        {
            int groupSize = Mathf.Min(Random.Range(minGroupSize, maxGroupSize + 1), enemiesToGroup);

            if (TrySpawnGroup(navMeshBounds, groupSize, ref successfulSpawns, ref totalAttempts))
            {
                enemiesToGroup -= groupSize;
                if (showDebug)
                {
                    Debug.Log($"[RandomNavMeshSpawner] Spawned group of {groupSize}. Remaining to group: {enemiesToGroup}");
                }
            }
        }

        // Spawn remaining solo enemies
        while (soloEnemies > 0 && totalAttempts < maxAttempts * 2)
        {
            if (TrySpawnSolo(navMeshBounds, ref successfulSpawns, ref totalAttempts))
            {
                soloEnemies--;
            }
            else
            {
                totalAttempts++;
                if (totalAttempts >= maxAttempts * 2) break;
            }
        }

        if (showDebug)
        {
            Debug.Log($"[RandomNavMeshSpawner] Spawning complete: {successfulSpawns}/{spawnCount} enemies spawned in {totalAttempts} attempts.");
        }
    }

    private bool TrySpawnSolo(Bounds navMeshBounds, ref int successfulSpawns, ref int totalAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            totalAttempts++;

            Vector3 randomPoint = GetRandomPointInBounds(navMeshBounds);
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPoint, out hit, 100f, NavMesh.AllAreas))
            {
                if (!IsValidSpawnPosition(hit.position, minDistanceBetweenSpawns))
                {
                    continue;
                }

                GameObject enemy = SpawnEnemy(hit.position);
                if (enemy != null)
                {
                    spawnedPositions.Add(hit.position);
                    debugSpawnPositions.Add(hit.position);
                    successfulSpawns++;

                    if (showDebug)
                    {
                        Debug.Log($"[RandomNavMeshSpawner] Spawned solo enemy {successfulSpawns} at {hit.position}");
                    }
                    return true;
                }
            }
        }
        return false;
    }

    private bool TrySpawnGroup(Bounds navMeshBounds, int groupSize, ref int successfulSpawns, ref int totalAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            totalAttempts++;

            // Find a valid center point for the group
            Vector3 randomPoint = GetRandomPointInBounds(navMeshBounds);
            NavMeshHit hit;

            if (!NavMesh.SamplePosition(randomPoint, out hit, 100f, NavMesh.AllAreas))
            {
                continue;
            }

            Vector3 groupCenter = hit.position;

            if (!IsValidSpawnPosition(groupCenter, minDistanceBetweenSpawns))
            {
                continue;
            }

            // Try to spawn all group members around this center
            List<Vector3> groupPositions = new List<Vector3>();
            groupPositions.Add(groupCenter);

            // Find positions for the rest of the group
            for (int i = 1; i < groupSize; i++)
            {
                Vector3 memberPos;
                if (FindGroupMemberPosition(groupCenter, out memberPos))
                {
                    groupPositions.Add(memberPos);
                }
                else
                {
                    // Couldn't find enough positions, try a different group center
                    break;
                }
            }

            // If we found enough positions, spawn the group
            if (groupPositions.Count == groupSize)
            {
                foreach (Vector3 pos in groupPositions)
                {
                    GameObject enemy = SpawnEnemy(pos);
                    if (enemy != null)
                    {
                        spawnedPositions.Add(pos);
                        debugSpawnPositions.Add(pos);
                        successfulSpawns++;
                    }
                }

                if (showDebug)
                {
                    Debug.Log($"[RandomNavMeshSpawner] Spawned group of {groupPositions.Count} at {groupCenter}");
                }
                return true;
            }
        }
        return false;
    }

    private bool FindGroupMemberPosition(Vector3 groupCenter, out Vector3 position)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * groupSpreadRadius;
            randomOffset.y = 0; // Keep on same Y level
            Vector3 candidatePos = groupCenter + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePos, out hit, 5f, NavMesh.AllAreas))
            {
                // Don't need to check distance from other spawns since we're in a group
                if (IsValidSpawnPosition(hit.position, 1f, false)) // Just check player visibility
                {
                    position = hit.position;
                    return true;
                }
            }
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsValidSpawnPosition(Vector3 position, float minDistance, bool checkDistanceFromSpawns = true)
    {
        // Check distance from players
        GameObject[] players = FindPlayers();

        // If no players found yet, skip player checks (they haven't spawned yet)
        if (players.Length > 0)
        {
            foreach (GameObject player in players)
            {
                if (player == null) continue;

                float distToPlayer = Vector3.Distance(position, player.transform.position);

                // Too close to player
                if (distToPlayer < minDistanceFromPlayer)
                {
                    return false;
                }

                // Check line of sight
                if (checkLineOfSight)
                {
                    Vector3 dirToPlayer = (player.transform.position - position).normalized;
                    Ray ray = new Ray(position + Vector3.up, dirToPlayer);
                    RaycastHit hitInfo;

                    // If we can see the player (no wall blocking), this is a bad spawn
                    if (Physics.Raycast(ray, out hitInfo, distToPlayer, lineOfSightBlockers))
                    {
                        // Hit something between spawn point and player (good - blocked)
                    }
                    else
                    {
                        // Nothing blocking line of sight (bad - player can see spawn)
                        if (showDebug)
                        {
                            Debug.Log($"[RandomNavMeshSpawner] Rejected spawn at {position} - player can see it");
                        }
                        return false;
                    }
                }
            }
        }
        else if (showDebug)
        {
            Debug.Log("[RandomNavMeshSpawner] No players found yet, skipping player visibility checks");
        }

        // Check distance from other spawned enemies
        if (checkDistanceFromSpawns)
        {
            foreach (Vector3 spawnedPos in spawnedPositions)
            {
                if (Vector3.Distance(position, spawnedPos) < minDistance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private GameObject[] FindPlayers()
    {
        List<GameObject> players = new List<GameObject>();

        if (usePhotonNetworking)
        {
            // Find all PhotonViews that belong to players
            var pvs = Object.FindObjectsOfType<Photon.Pun.PhotonView>();
            foreach (var pv in pvs)
            {
                if (pv.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>() != null)
                {
                    players.Add(pv.gameObject);
                }
            }
        }
        else
        {
            // Find by tag in single player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                players.Add(player);
            }
        }

        return players.ToArray();
    }

    private Vector3 GetRandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private Bounds CalculateNavMeshBounds(NavMeshTriangulation navMeshData)
    {
        if (navMeshData.vertices.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Vector3 min = navMeshData.vertices[0];
        Vector3 max = navMeshData.vertices[0];

        for (int i = 1; i < navMeshData.vertices.Length; i++)
        {
            min = Vector3.Min(min, navMeshData.vertices[i]);
            max = Vector3.Max(max, navMeshData.vertices[i]);
        }

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;

        return new Bounds(center, size);
    }

    private GameObject SpawnEnemy(Vector3 position)
    {
        if (usePhotonNetworking)
        {
            if (string.IsNullOrEmpty(photonPrefabPath))
            {
                Debug.LogError("[RandomNavMeshSpawner] Photon prefab path is empty! Set the path to your prefab in Resources folder.");
                return null;
            }

            return PhotonNetwork.Instantiate(photonPrefabPath, position, Quaternion.identity);
        }
        else
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[RandomNavMeshSpawner] Enemy prefab is not assigned!");
                return null;
            }

            return Instantiate(enemyPrefab, position, Quaternion.identity);
        }
    }

    // Visualize the NavMesh bounds and spawn positions in the editor
    private void OnDrawGizmos()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        if (navMeshData.vertices.Length == 0) return;

        Bounds bounds = CalculateNavMeshBounds(navMeshData);

        // Draw the entire NavMesh bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw spawn positions
        if (showSpawnGizmos && debugSpawnPositions.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (Vector3 pos in debugSpawnPositions)
            {
                Gizmos.DrawSphere(pos, 0.5f);
            }
        }

        // Draw player safe zone
        GameObject[] players = FindPlayers();
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                Gizmos.DrawWireSphere(player.transform.position, minDistanceFromPlayer);
            }
        }
    }
}
