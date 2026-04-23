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

        Debug.Log($"Spawning player at {spawnPosition}");

        // Spawn the player using Photon
        GameObject player = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, spawnRotation);

        if (player != null)
        {
            Debug.Log($"Player spawned successfully: {player.name}");
        }
        else
        {
            Debug.LogError($"Failed to spawn player! Make sure '{playerPrefabName}' exists in Resources folder.");
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
                // Use spawn point based on player number
                int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
                return spawnPoints[index].position;
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
                int index = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
                return spawnPoints[index].rotation;
            }
        }

        return transform.rotation;
    }
}
