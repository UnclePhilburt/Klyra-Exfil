using UnityEngine;

/// <summary>
/// Spawns a player character in offline/test mode without requiring Photon connection.
/// Use this in test scenes for quick iteration without multiplayer.
/// </summary>
public class OfflinePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [Tooltip("The player prefab to spawn (can be directly from project, doesn't need to be in Resources)")]
    public GameObject playerPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Spawn points in the scene (leave empty to use this object's position)")]
    public Transform[] spawnPoints;

    [Tooltip("Random spawn point?")]
    public bool randomSpawn = true;

    [Header("Camera Setup")]
    [Tooltip("Automatically assign the spawned character to the camera controller")]
    public bool autoAssignCamera = true;

    private GameObject spawnedPlayer;

    void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[OfflinePlayerSpawner] No player prefab assigned! Please assign a player prefab in the inspector.");
            return;
        }

        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        Debug.Log($"[OfflinePlayerSpawner] Spawning player '{playerPrefab.name}' at {spawnPosition}");

        // Instantiate the player
        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        if (spawnedPlayer != null)
        {
            Debug.Log($"[OfflinePlayerSpawner] Player spawned successfully: {spawnedPlayer.name}");

            // Auto-assign to camera if enabled
            if (autoAssignCamera)
            {
                AssignToCamera();
            }

            // Disable any Photon components on the player to prevent errors
            DisablePhotonComponents();
        }
        else
        {
            Debug.LogError("[OfflinePlayerSpawner] Failed to spawn player!");
        }
    }

    void AssignToCamera()
    {
        // Find the camera controller in the scene
        var cameraController = FindObjectOfType<Opsive.UltimateCharacterController.Camera.CameraController>();

        if (cameraController != null && spawnedPlayer != null)
        {
            cameraController.Character = spawnedPlayer;
            Debug.Log("[OfflinePlayerSpawner] Assigned player to camera controller");
        }
        else if (cameraController == null)
        {
            Debug.LogWarning("[OfflinePlayerSpawner] No CameraController found in scene");
        }
    }

    void DisablePhotonComponents()
    {
        if (spawnedPlayer == null) return;

        // Find and remove/disable PhotonView components
        var photonViews = spawnedPlayer.GetComponentsInChildren<Photon.Pun.PhotonView>(true);
        foreach (var pv in photonViews)
        {
            // Destroy the component entirely to prevent issues
            Destroy(pv);
        }

        // Remove PhotonTransformView components
        var photonTransformViews = spawnedPlayer.GetComponentsInChildren<Photon.Pun.PhotonTransformView>(true);
        foreach (var ptv in photonTransformViews)
        {
            Destroy(ptv);
        }

        var photonTransformViewClassics = spawnedPlayer.GetComponentsInChildren<Photon.Pun.PhotonTransformViewClassic>(true);
        foreach (var ptvc in photonTransformViewClassics)
        {
            Destroy(ptvc);
        }

        Debug.Log("[OfflinePlayerSpawner] Removed Photon components for offline play");

        // Make sure the character controller is enabled
        var characterLocomotion = spawnedPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (characterLocomotion != null)
        {
            characterLocomotion.enabled = true;
            Debug.Log("[OfflinePlayerSpawner] UltimateCharacterLocomotion is enabled");
        }

        // Make sure input is enabled
        var inputHandler = spawnedPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotionHandler>();
        if (inputHandler != null)
        {
            inputHandler.enabled = true;
            Debug.Log("[OfflinePlayerSpawner] UltimateCharacterLocomotionHandler is enabled");
        }
    }

    Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (randomSpawn)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            }
            else
            {
                return spawnPoints[0].position;
            }
        }

        // Default to this object's position
        return transform.position;
    }

    Quaternion GetSpawnRotation()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (randomSpawn)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)].rotation;
            }
            else
            {
                return spawnPoints[0].rotation;
            }
        }

        return transform.rotation;
    }

    // Visualize spawn points in the editor
    private void OnDrawGizmos()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // Draw default spawn at this object's position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
        else
        {
            // Draw all spawn points
            foreach (Transform spawn in spawnPoints)
            {
                if (spawn == null) continue;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawn.position, 0.5f);
                Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 2f);
            }
        }
    }
}
