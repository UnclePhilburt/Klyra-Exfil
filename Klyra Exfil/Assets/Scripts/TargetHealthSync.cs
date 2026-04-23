using UnityEngine;
using Photon.Pun;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Traits;

/// <summary>
/// Syncs damage to a target across the network.
/// Works with UCC's Health/Respawner system.
/// </summary>
public class TargetHealthSync : MonoBehaviourPun
{
    private Health health;
    private Respawner respawner;

    void Awake()
    {
        health = GetComponent<Health>();
        respawner = GetComponent<Respawner>();
    }

    void OnEnable()
    {
        // Listen for damage events
        EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(gameObject, "OnHealthDamage", OnDamage);
        EventHandler.RegisterEvent(gameObject, "OnRespawn", OnRespawn);
    }

    void OnDisable()
    {
        EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(gameObject, "OnHealthDamage", OnDamage);
        EventHandler.UnregisterEvent(gameObject, "OnRespawn", OnRespawn);
    }

    /// <summary>
    /// Called when target takes damage locally
    /// </summary>
    private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, object attackerObject, Collider hitCollider)
    {
        // Sync damage to all clients
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.Others, amount);
        }
    }

    /// <summary>
    /// Called when target respawns locally
    /// </summary>
    private void OnRespawn()
    {
        // Sync respawn to all clients
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_Respawn", RpcTarget.Others);
        }
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        // Apply damage on other clients
        if (health != null)
        {
            health.Damage(amount);
        }
    }

    [PunRPC]
    void RPC_Respawn()
    {
        // Trigger respawn on other clients
        if (respawner != null)
        {
            respawner.Respawn();
        }
    }
}
