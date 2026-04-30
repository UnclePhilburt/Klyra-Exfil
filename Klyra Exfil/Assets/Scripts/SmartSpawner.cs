using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

/// <summary>
/// Smart spawner that randomly picks spawn points each game for variety.
/// Ready or Not style - never the same layout twice.
/// </summary>
public class SmartSpawner : MonoBehaviour
{
    [Header("Multiplayer")]
    [Tooltip("Use Photon networking? (Only master client spawns)")]
    public bool usePhoton = true;

    [Tooltip("Path to enemy prefab in Resources folder (for Photon). Example: 'BikerCriminal'")]
    public string photonPrefabName = "BikerCriminal";

    [Header("Spawn Configuration")]
    [Tooltip("Enemy prefabs to spawn (will randomly choose). Used for offline mode.")]
    public GameObject[] enemyPrefabs;

    [Tooltip("Total number of enemies to spawn this round")]
    public int totalEnemiesToSpawn = 10;

    [Tooltip("Minimum enemies to spawn (if not enough valid spawn points)")]
    public int minEnemiesToSpawn = 5;

    [Header("Spawn Distribution")]
    [Tooltip("How many spawn points to use (0 = use all available)")]
    public int maxSpawnPointsToUse = 0;

    [Tooltip("Prefer spreading enemies across rooms vs clustering")]
    [Range(0f, 1f)]
    public float spreadFactor = 0.7f;

    [Header("Spawn Timing")]
    [Tooltip("Spawn all enemies at start?")]
    public bool spawnOnStart = true;

    [Tooltip("Delay between spawning each enemy (0 = instant)")]
    public float spawnDelay = 0.1f;

    [Header("References")]
    [Tooltip("All spawn points in the level (auto-finds if empty)")]
    public List<SmartSpawnPoint> spawnPoints = new List<SmartSpawnPoint>();

    [Header("Debug")]
    [Tooltip("Log spawn decisions")]
    public bool debugSpawning = true;

    private List<SmartSpawnPoint> activeSpawnPoints = new List<SmartSpawnPoint>();
    private int enemiesSpawned = 0;
    private Dictionary<string, int> roomEnemyCounts = new Dictionary<string, int>();

    void Start()
    {
        // Only master client spawns in multiplayer
        if (usePhoton && !PhotonNetwork.IsMasterClient)
        {
            if (debugSpawning) Debug.Log("SmartSpawner: Not master client, skipping spawn");
            return;
        }

        // Auto-find spawn points if not set
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            spawnPoints = new List<SmartSpawnPoint>(FindObjectsOfType<SmartSpawnPoint>());
            Debug.Log($"SmartSpawner: Auto-found {spawnPoints.Count} spawn points");
        }

        if (spawnOnStart)
        {
            SpawnEnemies();
        }
    }

    /// <summary>
    /// Main spawn function - call this to spawn enemies
    /// </summary>
    public void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("SmartSpawner: No enemy prefabs assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("SmartSpawner: No spawn points found!");
            return;
        }

        // Reset all spawn points
        foreach (var point in spawnPoints)
        {
            point.Reset();
        }

        // Select which spawn points to use this round
        SelectSpawnPoints();

        if (activeSpawnPoints.Count == 0)
        {
            Debug.LogWarning("SmartSpawner: No valid spawn points available!");
            return;
        }

        // Distribute enemies across selected spawn points
        DistributeAndSpawnEnemies();
    }

    /// <summary>
    /// Randomly select which spawn points to use this game
    /// </summary>
    void SelectSpawnPoints()
    {
        activeSpawnPoints.Clear();
        roomEnemyCounts.Clear();

        // Get all valid spawn points
        List<SmartSpawnPoint> validPoints = spawnPoints.Where(p => p != null && p.CanSpawn()).ToList();

        if (validPoints.Count == 0)
        {
            Debug.LogWarning("SmartSpawner: No valid spawn points!");
            return;
        }

        // Determine how many spawn points to use
        int pointsToUse = maxSpawnPointsToUse > 0
            ? Mathf.Min(maxSpawnPointsToUse, validPoints.Count)
            : validPoints.Count;

        // Select spawn points based on priority and randomness
        while (activeSpawnPoints.Count < pointsToUse && validPoints.Count > 0)
        {
            // Weighted random selection based on priority
            SmartSpawnPoint selected = SelectWeightedRandom(validPoints);
            activeSpawnPoints.Add(selected);
            validPoints.Remove(selected);

            // Track room counts for spreading
            if (!roomEnemyCounts.ContainsKey(selected.roomName))
            {
                roomEnemyCounts[selected.roomName] = 0;
            }
        }

        if (debugSpawning)
        {
            Debug.Log($"SmartSpawner: Selected {activeSpawnPoints.Count} spawn points from {spawnPoints.Count} total");
            foreach (var point in activeSpawnPoints)
            {
                Debug.Log($"  - {point.roomName} at {point.transform.position} (priority {point.priority})");
            }
        }
    }

    /// <summary>
    /// Select random spawn point weighted by priority
    /// </summary>
    SmartSpawnPoint SelectWeightedRandom(List<SmartSpawnPoint> points)
    {
        // Calculate total weight
        int totalWeight = points.Sum(p => p.priority);

        // Random value
        int randomValue = Random.Range(0, totalWeight);

        // Select based on weight
        int currentWeight = 0;
        foreach (var point in points)
        {
            currentWeight += point.priority;
            if (randomValue < currentWeight)
            {
                return point;
            }
        }

        // Fallback to last point
        return points[points.Count - 1];
    }

    /// <summary>
    /// Distribute enemies across spawn points and spawn them
    /// </summary>
    void DistributeAndSpawnEnemies()
    {
        enemiesSpawned = 0;
        int enemiesToSpawn = totalEnemiesToSpawn;

        // Create spawn distribution
        List<SmartSpawnPoint> spawnQueue = new List<SmartSpawnPoint>();

        // Distribute enemies across spawn points
        if (spreadFactor > 0.5f)
        {
            // High spread - try to put enemies in different rooms
            while (enemiesToSpawn > 0 && activeSpawnPoints.Count > 0)
            {
                foreach (var point in activeSpawnPoints)
                {
                    if (enemiesToSpawn <= 0) break;
                    if (point.currentEnemyCount < point.maxEnemiesAtPoint)
                    {
                        spawnQueue.Add(point);
                        point.OnEnemySpawned();
                        roomEnemyCounts[point.roomName]++;
                        enemiesToSpawn--;
                    }
                }
            }
        }
        else
        {
            // Low spread - cluster enemies more
            foreach (var point in activeSpawnPoints)
            {
                int enemiesAtPoint = Random.Range(1, Mathf.Min(point.maxEnemiesAtPoint + 1, enemiesToSpawn + 1));
                for (int i = 0; i < enemiesAtPoint && enemiesToSpawn > 0; i++)
                {
                    spawnQueue.Add(point);
                    point.OnEnemySpawned();
                    roomEnemyCounts[point.roomName]++;
                    enemiesToSpawn--;
                }
            }
        }

        // Spawn enemies from queue
        if (spawnDelay > 0)
        {
            StartCoroutine(SpawnWithDelay(spawnQueue));
        }
        else
        {
            foreach (var point in spawnQueue)
            {
                SpawnEnemyAt(point);
            }
        }

        if (debugSpawning)
        {
            Debug.Log($"SmartSpawner: Spawning {spawnQueue.Count} enemies");
            foreach (var room in roomEnemyCounts)
            {
                Debug.Log($"  - {room.Key}: {room.Value} enemies");
            }
        }
    }

    /// <summary>
    /// Spawn enemies with delay between each
    /// </summary>
    System.Collections.IEnumerator SpawnWithDelay(List<SmartSpawnPoint> spawnQueue)
    {
        foreach (var point in spawnQueue)
        {
            SpawnEnemyAt(point);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    /// <summary>
    /// Spawn a single enemy at a spawn point
    /// </summary>
    void SpawnEnemyAt(SmartSpawnPoint spawnPoint)
    {
        GameObject enemy = null;

        try
        {
            if (usePhoton)
            {
                // Photon multiplayer spawning
                if (string.IsNullOrEmpty(photonPrefabName))
                {
                    Debug.LogError("SmartSpawner: Photon prefab name is empty!");
                    return;
                }

                enemy = PhotonNetwork.Instantiate(photonPrefabName, spawnPoint.transform.position, spawnPoint.transform.rotation);

                if (debugSpawning)
                {
                    Debug.Log($"SmartSpawner: Spawned {photonPrefabName} via Photon at {spawnPoint.roomName} ({enemiesSpawned + 1}/{totalEnemiesToSpawn})");
                }
            }
            else
            {
                // Offline spawning
                if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                {
                    Debug.LogError("SmartSpawner: No enemy prefabs assigned!");
                    return;
                }

                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (prefab == null)
                {
                    Debug.LogError("SmartSpawner: Enemy prefab is NULL!");
                    return;
                }

                enemy = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

                if (debugSpawning)
                {
                    Debug.Log($"SmartSpawner: Spawned {prefab.name} at {spawnPoint.roomName} ({enemiesSpawned + 1}/{totalEnemiesToSpawn})");
                }
            }

            if (enemy != null)
            {
                // Start coroutine to initialize after UCC finishes setup
                StartCoroutine(InitializeSpawnedEnemy(enemy, spawnPoint));
                enemiesSpawned++;
            }
            else
            {
                Debug.LogError($"SmartSpawner: Failed to spawn enemy!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SmartSpawner: Exception spawning enemy: {e.Message}");
        }
    }

    /// <summary>
    /// Initialize spawned enemy after UCC finishes setup
    /// </summary>
    System.Collections.IEnumerator InitializeSpawnedEnemy(GameObject enemy, SmartSpawnPoint spawnPoint)
    {
        if (enemy == null) yield break;

        // Wait for UCC to initialize
        yield return null;

        // Get inventory component
        var inventory = enemy.GetComponent<Opsive.UltimateCharacterController.Inventory.InventoryBase>();
        if (inventory != null)
        {
            if (debugSpawning)
            {
                Debug.Log($"SmartSpawner: Manually calling LoadDefaultLoadout for {enemy.name}");
            }

            // Use reflection to call the LoadDefaultLoadout method
            var loadMethod = inventory.GetType().GetMethod("LoadDefaultLoadout",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (loadMethod != null)
            {
                loadMethod.Invoke(inventory, null);

                if (debugSpawning)
                {
                    Debug.Log($"SmartSpawner: Called LoadDefaultLoadout successfully");
                }
            }
            else if (debugSpawning)
            {
                Debug.LogWarning($"SmartSpawner: LoadDefaultLoadout method not found!");
            }

            // Wait for items to instantiate
            yield return null;
            yield return null;
        }

        if (debugSpawning)
        {
            // Check what items the inventory has
            var items = enemy.GetComponentsInChildren<Opsive.UltimateCharacterController.Items.CharacterItem>(true);
            Debug.Log($"SmartSpawner: {enemy.name} has {items.Length} items in hierarchy");

            foreach (var item in items)
            {
                Debug.Log($"  - Item: {item.name}, ItemDefinition: {item.ItemDefinition?.name ?? "NULL"}, IsActive: {item.gameObject.activeInHierarchy}");
            }
        }

        // Wait one more frame before trying to equip
        yield return null;

        // Try to manually equip items using event system
        Opsive.Shared.Events.EventHandler.ExecuteEvent(enemy, "OnItemSetManagerTryEquipItemSet", 0, true);

        if (debugSpawning)
        {
            Debug.Log($"SmartSpawner: Sent equip event for {enemy.name}");
        }

        // Subscribe to death event to update spawn point count
        var health = enemy.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
        if (health != null)
        {
            Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(
                enemy,
                "OnDeath",
                (pos, force, attacker) => spawnPoint.OnEnemyDied()
            );
        }
    }

    /// <summary>
    /// Reset spawner for new round
    /// </summary>
    public void ResetSpawner()
    {
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                point.Reset();
            }
        }

        activeSpawnPoints.Clear();
        roomEnemyCounts.Clear();
        enemiesSpawned = 0;
    }

    void OnDrawGizmos()
    {
        // Draw connections between spawner and active spawn points
        if (activeSpawnPoints != null && activeSpawnPoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in activeSpawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawLine(transform.position, point.transform.position);
                }
            }
        }
    }
}
