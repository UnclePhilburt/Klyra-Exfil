using UnityEngine;

/// <summary>
/// Individual spawn point that can be part of a SmartSpawner.
/// Place these around your level to mark potential enemy spawn locations.
/// </summary>
public class SmartSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Room/area identifier (e.g., 'Lobby', 'Hallway A', 'Rooftop')")]
    public string roomName = "Room";

    [Tooltip("Priority for this spawn (higher = more likely to be chosen)")]
    [Range(1, 10)]
    public int priority = 5;

    [Tooltip("Can this spawn point be used?")]
    public bool isActive = true;

    [Tooltip("Maximum enemies that can spawn here")]
    public int maxEnemiesAtPoint = 1;

    [Header("Spawn Constraints")]
    [Tooltip("Minimum distance from player to spawn here")]
    public float minDistanceFromPlayer = 10f;

    [Tooltip("Only spawn if player can't see this point")]
    public bool requireOutOfSight = true;

    [Header("Visual")]
    [Tooltip("Color for this spawn point in editor")]
    public Color gizmoColor = Color.red;

    public int currentEnemyCount { get; private set; } = 0;

    /// <summary>
    /// Check if this spawn point can be used right now
    /// </summary>
    public bool CanSpawn()
    {
        if (!isActive) return false;
        if (currentEnemyCount >= maxEnemiesAtPoint) return false;

        // Check distance from player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistanceFromPlayer) return false;

            // Check line of sight to player if required
            if (requireOutOfSight)
            {
                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distance))
                {
                    // Player can see this point - don't spawn here
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Called when an enemy spawns here
    /// </summary>
    public void OnEnemySpawned()
    {
        currentEnemyCount++;
    }

    /// <summary>
    /// Called when an enemy at this spawn dies
    /// </summary>
    public void OnEnemyDied()
    {
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
    }

    /// <summary>
    /// Reset spawn point for new round
    /// </summary>
    public void Reset()
    {
        currentEnemyCount = 0;
    }

    void OnDrawGizmos()
    {
        // Draw spawn point indicator
        Gizmos.color = isActive ? gizmoColor : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            $"{roomName}\nPriority: {priority}\n{currentEnemyCount}/{maxEnemiesAtPoint}");
        #endif
    }

    void OnDrawGizmosSelected()
    {
        // Draw min distance from player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
    }
}
