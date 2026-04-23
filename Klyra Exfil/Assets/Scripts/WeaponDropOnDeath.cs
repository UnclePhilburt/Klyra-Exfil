using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items;
using Photon.Pun;

/// <summary>
/// Drops all weapons when character dies. Works with Opsive's inventory system.
/// Dropped weapons become evidence that can be collected.
/// </summary>
public class WeaponDropOnDeath : MonoBehaviourPun
{
    [Header("Drop Settings")]
    [Tooltip("Force applied to dropped weapons")]
    public float dropForce = 3f;

    [Tooltip("Random torque applied to dropped weapons")]
    public float dropTorque = 5f;

    [Tooltip("Should weapons be marked as evidence?")]
    public bool markAsEvidence = true;

    private Opsive.UltimateCharacterController.Traits.Health healthComponent;
    private bool hasDroppedWeapons = false;

    void Start()
    {
        // Get health component
        healthComponent = GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
        if (healthComponent != null)
        {
            // Subscribe to death event with 3 parameters (position, force, attacker)
            Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnDeath);
            Debug.Log($"WeaponDropOnDeath: Subscribed to OnDeath event for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"WeaponDropOnDeath: No Health component found on {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthComponent != null)
        {
            Opsive.Shared.Events.EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnDeath);
        }
    }

    void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
    {
        if (hasDroppedWeapons) return;
        hasDroppedWeapons = true;

        Debug.Log($"{gameObject.name} died at {position} - dropping weapons (killed by {(attacker != null ? attacker.name : "unknown")})");

        // Drop weapons over network if multiplayer
        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_DropWeapons", RpcTarget.All);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            DropAllWeapons();
        }
    }

    [PunRPC]
    void RPC_DropWeapons()
    {
        DropAllWeapons();
    }

    void DropAllWeapons()
    {
        // Get inventory at time of death (not in Start)
        InventoryBase inventory = GetComponent<InventoryBase>();

        if (inventory == null)
        {
            Debug.LogWarning("Cannot drop weapons - no inventory");
            return;
        }

        // Get all items in inventory
        var items = inventory.GetAllCharacterItems();

        if (items.Count == 0)
        {
            Debug.Log($"{gameObject.name} has no items to drop");
            return;
        }

        Debug.Log($"Dropping {items.Count} items from {gameObject.name}");

        // Create simple evidence objects instead of using Opsive's drop system
        for (int i = 0; i < items.Count; i++)
        {
            CharacterItem item = items[i];
            if (item == null) continue;

            Debug.Log($"Creating evidence for: {item.ItemDefinition.name}");

            // Create a simple evidence object from the weapon's visual
            GameObject evidenceObject = CreateEvidenceFromItem(item);

            if (evidenceObject != null)
            {
                Debug.Log($"Successfully created evidence for {item.ItemDefinition.name} at position {evidenceObject.transform.position}");
            }
            else
            {
                Debug.LogWarning($"Failed to create evidence for {item.ItemDefinition.name}");
            }
        }
    }

    GameObject CreateEvidenceFromItem(CharacterItem item)
    {
        // Get the visible item object
        GameObject itemVisual = item.GetVisibleObject();
        if (itemVisual == null)
        {
            Debug.LogWarning($"No visible object for {item.ItemDefinition.name}");
            return null;
        }

        // Calculate drop position
        Vector3 dropPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1f;
        Quaternion dropRotation = Quaternion.Euler(
            Random.Range(-15f, 15f),
            Random.Range(0f, 360f),
            Random.Range(-15f, 15f)
        );

        // Create evidence container
        GameObject evidenceObj = new GameObject(item.ItemDefinition.name + " Evidence");
        evidenceObj.transform.position = dropPosition;
        evidenceObj.transform.rotation = dropRotation;

        // Copy the visual mesh
        CopyMeshToObject(itemVisual, evidenceObj);

        // Add physics
        Rigidbody rb = evidenceObj.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;

        // Add collider
        BoxCollider collider = evidenceObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.1f, 0.1f, 0.5f); // Generic weapon size

        // Apply drop force
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.2f, 0.5f),
            Random.Range(-1f, 1f)
        ).normalized;

        rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * dropTorque, ForceMode.Impulse);

        // Add Evidence component
        if (markAsEvidence)
        {
            Evidence evidence = evidenceObj.AddComponent<Evidence>();
            evidence.evidenceType = Evidence.EvidenceType.Weapon;
            evidence.evidenceName = item.ItemDefinition.name;
            evidence.showPrompt = true; // Make sure prompt is enabled
            Debug.Log($"Added Evidence component to {evidenceObj.name}, showPrompt={evidence.showPrompt}");

            // Force the Evidence component to start
            evidence.enabled = true;
        }

        return evidenceObj;
    }

    void CopyMeshToObject(GameObject source, GameObject target)
    {
        // Find all mesh renderers in source
        MeshRenderer[] sourceRenderers = source.GetComponentsInChildren<MeshRenderer>();
        MeshFilter[] sourceFilters = source.GetComponentsInChildren<MeshFilter>();

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            if (i >= sourceFilters.Length) break;

            // Create child object for each mesh
            GameObject meshObj = new GameObject("Mesh_" + i);
            meshObj.transform.SetParent(target.transform);
            meshObj.transform.localPosition = sourceFilters[i].transform.localPosition;
            meshObj.transform.localRotation = sourceFilters[i].transform.localRotation;
            meshObj.transform.localScale = sourceFilters[i].transform.localScale;

            // Copy mesh filter
            MeshFilter newFilter = meshObj.AddComponent<MeshFilter>();
            newFilter.mesh = sourceFilters[i].sharedMesh;

            // Copy mesh renderer
            MeshRenderer newRenderer = meshObj.AddComponent<MeshRenderer>();
            newRenderer.materials = sourceRenderers[i].sharedMaterials;
        }
    }
}
