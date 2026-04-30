using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.UltimateCharacterController.Inventory;
using Klyra.Loadout;

/// <summary>
/// Simple grenade throwing - press G to throw a grenade from inventory.
/// No CharacterItem needed - just spawns and throws the projectile directly.
/// </summary>
public class SimpleGrenadeThrow : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to press to throw grenade")]
    public Key throwKey = Key.G;

    [Tooltip("Name of the grenade item in LoadoutManager (e.g. 'FragGrenade')")]
    public string grenadeItemName = "FragGrenade";

    [Tooltip("Grenade projectile prefab to spawn and throw")]
    public GameObject grenadePrefab;

    [Header("Throw Settings")]
    [Tooltip("How fast to throw the grenade")]
    public float throwForce = 20f;

    [Tooltip("Offset from camera to spawn grenade (in front of player)")]
    public Vector3 spawnOffset = new Vector3(0, 0, 1f);

    private InventoryBase inventory;
    private Camera playerCamera;

    void Start()
    {
        inventory = GetComponent<InventoryBase>();
        if (inventory == null)
        {
            inventory = GetComponentInChildren<InventoryBase>();
        }

        playerCamera = Camera.main;
    }

    void Update()
    {
        // Only throw if this is the local player
        var pv = GetComponent<Photon.Pun.PhotonView>();
        if (pv != null && !pv.IsMine) return;

        // Use new Input System
        if (Keyboard.current != null && Keyboard.current[throwKey].wasPressedThisFrame)
        {
            Debug.Log("[SimpleGrenadeThrow] G key pressed!");
            TryThrowGrenade();
        }
    }

    void TryThrowGrenade()
    {
        if (inventory == null)
        {
            Debug.LogWarning("[SimpleGrenadeThrow] No inventory found!");
            return;
        }

        if (grenadePrefab == null)
        {
            Debug.LogError("[SimpleGrenadeThrow] Grenade prefab not assigned!");
            return;
        }

        // Get the grenade ItemDefinition from LoadoutManager
        var manager = LoadoutManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[SimpleGrenadeThrow] LoadoutManager not found!");
            return;
        }

        var itemDef = manager.FindItem(grenadeItemName);
        if (itemDef == null)
        {
            Debug.LogWarning($"[SimpleGrenadeThrow] Item '{grenadeItemName}' not found in LoadoutManager!");
            return;
        }

        // Check if we have grenades in inventory
        var itemId = itemDef.CreateItemIdentifier();
        int count = inventory.GetItemIdentifierAmount(itemId);

        if (count <= 0)
        {
            Debug.Log("[SimpleGrenadeThrow] No grenades in inventory!");
            return;
        }

        // Remove one grenade from inventory
        inventory.RemoveItemIdentifierAmount(itemId, 1);

        // Spawn and throw the grenade
        ThrowGrenade();

        Debug.Log($"[SimpleGrenadeThrow] Threw grenade! Remaining: {inventory.GetItemIdentifierAmount(itemId)}");
    }

    void ThrowGrenade()
    {
        // Spawn position - in front of the player
        Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);

        // If we have a camera, spawn from camera position
        if (playerCamera != null)
        {
            spawnPos = playerCamera.transform.position + playerCamera.transform.forward * spawnOffset.z;
        }

        // Spawn the grenade
        GameObject grenade = null;

        if (Photon.Pun.PhotonNetwork.IsConnected)
        {
            // Network spawn - grenade prefab must be in Resources folder
            grenade = Photon.Pun.PhotonNetwork.Instantiate(grenadePrefab.name, spawnPos, Quaternion.identity);
        }
        else
        {
            // Local spawn
            grenade = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);
        }

        // Add throw force
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Throw direction - forward from camera if available
            Vector3 throwDir = transform.forward;
            if (playerCamera != null)
            {
                throwDir = playerCamera.transform.forward;
            }

            rb.AddForce(throwDir * throwForce, ForceMode.VelocityChange);
        }
    }
}
