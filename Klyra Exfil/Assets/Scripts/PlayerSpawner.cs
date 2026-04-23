using UnityEngine;
using Photon.Pun;

/// <summary>
/// Spawns the player character when entering the game scene.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [Tooltip("The player prefab to spawn (must be in Resources folder)")]
    public string playerPrefabName = "Player";

    [Header("Spawn Settings")]
    [Tooltip("Spawn points in the scene (leave empty to use this object's position)")]
    public Transform[] spawnPoints;

    [Tooltip("Random spawn point?")]
    public bool randomSpawn = true;

    void Start()
    {
        // Only spawn if we're connected and don't already have a player
        if (PhotonNetwork.IsConnected && !HasSpawnedPlayer())
        {
            SpawnPlayer();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Not connected to Photon. Player will not spawn.");
        }
    }

    /// <summary>
    /// Destroy the current local player and spawn a new one at the same
    /// position using the currently-selected character. Inventory carries
    /// over via LoadoutApplier's snapshot on destroy.
    /// </summary>
    public static void RespawnLocalPlayer()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("RespawnLocalPlayer: not connected to Photon, skipping.");
            return;
        }

        PhotonView[] views = FindObjectsOfType<PhotonView>();
        GameObject localPlayer = null;
        foreach (PhotonView view in views)
        {
            if (view.IsMine && view.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>() != null)
            {
                localPlayer = view.gameObject;
                break;
            }
        }

        if (localPlayer == null)
        {
            Debug.LogWarning("RespawnLocalPlayer: no local player found.");
            return;
        }

        Vector3 pos = localPlayer.transform.position;
        Quaternion rot = localPlayer.transform.rotation;

        var lm = Klyra.Loadout.LoadoutManager.Instance;
        string prefabName = lm != null && !string.IsNullOrEmpty(lm.CurrentCharacter)
            ? lm.CurrentCharacter
            : null;
        if (string.IsNullOrEmpty(prefabName))
        {
            // Fall back to the existing spawner's default if present.
            var fallback = FindObjectOfType<PlayerSpawner>();
            if (fallback != null) prefabName = fallback.playerPrefabName;
        }
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError("RespawnLocalPlayer: no prefab name available (LoadoutManager.CurrentCharacter empty and no PlayerSpawner fallback).");
            return;
        }

        // Detach the camera from the old character before destroying it —
        // otherwise ObjectFader / look source components keep dangling
        // Transform refs and throw on the next Update.
        var camCtrl = FindObjectOfType<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (camCtrl != null) camCtrl.Character = null;

        PhotonNetwork.Destroy(localPlayer);

        var spawned = PhotonNetwork.Instantiate(prefabName, pos, rot);
        if (spawned == null)
        {
            Debug.LogError($"RespawnLocalPlayer: PhotonNetwork.Instantiate('{prefabName}') returned null. Is the prefab in Resources/?");
            return;
        }

        // Reattach camera to the new character so look / fade / IK rebind.
        if (camCtrl != null) camCtrl.Character = spawned;
    }

    bool HasSpawnedPlayer()
    {
        // Check if we already spawned a player (look for PhotonView owned by us)
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in views)
        {
            if (view.IsMine && view.gameObject.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void SpawnPlayer()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        // The player's saved character pick on the LoadoutManager takes
        // priority over the inspector's default. Falls back if unset.
        string prefabName = playerPrefabName;
        var lm = Klyra.Loadout.LoadoutManager.Instance;
        if (lm != null && !string.IsNullOrEmpty(lm.CurrentCharacter))
        {
            prefabName = lm.CurrentCharacter;
        }

        Debug.Log($"Spawning player '{prefabName}' at {spawnPosition}");

        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPosition, spawnRotation);

        if (player != null)
        {
            Debug.Log($"Player spawned successfully: {player.name}");
        }
        else
        {
            Debug.LogError($"Failed to spawn player! Make sure '{prefabName}' exists in Resources folder.");
        }
    }

    public Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (randomSpawn)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            }
            else
            {
                // Use spawn point based on player number
                int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
                return spawnPoints[index].position;
            }
        }

        // Default to this object's position
        return transform.position;
    }

    public Quaternion GetSpawnRotation()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (randomSpawn)
            {
                return spawnPoints[Random.Range(0, spawnPoints.Length)].rotation;
            }
            else
            {
                int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
                return spawnPoints[index].rotation;
            }
        }

        return transform.rotation;
    }
}
