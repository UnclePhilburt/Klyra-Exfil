using UnityEngine;
using Photon.Pun;

/// <summary>
/// All-in-one door setup component. Just add this to your door and it configures everything automatically.
/// </summary>
public class TacticalDoorSetup : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The actual door mesh that rotates (leave empty to auto-find)")]
    public Transform doorMeshTransform;

    [Tooltip("Is the door locked by default?")]
    public bool startLocked = false;

    [Tooltip("Can this door be breached?")]
    public bool canBreach = true;

    [Header("Door Animation")]
    [Tooltip("How far the door opens in degrees")]
    public float openAngle = 90f;

    [Tooltip("Time to open normally")]
    public float openSpeed = 1f;

    [Tooltip("Time to open when breached (faster)")]
    public float breachSpeed = 0.2f;

    [Header("Audio (Optional - will use defaults if empty)")]
    [Tooltip("Sound when door opens")]
    public AudioClip openSound;

    [Tooltip("Sound when door closes")]
    public AudioClip closeSound;

    [Tooltip("Sound when locked door is tried")]
    public AudioClip lockedSound;

    [Tooltip("Sound when door is breached")]
    public AudioClip breachSound;

    [Header("Breaching Charge")]
    [Tooltip("Breaching charge prefab (leave empty to use default)")]
    public GameObject breachingChargePrefab;

    [Tooltip("UCC Item Definition for a breach charge. When set, placement pulls from the player's inventory (loadout-driven).")]
    public Opsive.Shared.Inventory.ItemDefinitionBase breachChargeItem;

    [Tooltip("Legacy per-door charge count. Only used when Breach Charge Item is unset.")]
    public int breachChargesAvailable = 3;

    [Header("Optional Features")]
    [Tooltip("Enable snake cam (slide a cable camera under the door).")]
    public bool enableSnakeCam = true;

    [Header("Advanced")]
    [Tooltip("Auto-detect door mesh and colliders?")]
    public bool autoDetect = true;

    void Start()
    {
        SetupDoor();
    }

    void SetupDoor()
    {
        Debug.Log($"Setting up tactical door: {gameObject.name}");

        // 1. Auto-detect door mesh if not assigned
        if (doorMeshTransform == null && autoDetect)
        {
            // Try to find a child with a MeshRenderer
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                doorMeshTransform = renderers[0].transform;
                Debug.Log($"Auto-detected door mesh: {doorMeshTransform.name}");
            }
            else
            {
                doorMeshTransform = transform;
                Debug.LogWarning("No mesh renderer found, using parent transform");
            }
        }

        // 2. Add Door component
        Door doorScript = GetComponent<Door>();
        if (doorScript == null)
        {
            doorScript = gameObject.AddComponent<Door>();
            Debug.Log("Added Door component");
        }

        // Configure Door component
        doorScript.isLocked = startLocked;
        doorScript.canBeBreach = canBreach;
        doorScript.doorTransform = doorMeshTransform;
        doorScript.openAngle = openAngle;
        doorScript.openSpeed = openSpeed;
        doorScript.breachSpeed = breachSpeed;
        doorScript.openSound = openSound;
        doorScript.closeSound = closeSound;
        doorScript.lockedSound = lockedSound;
        doorScript.breachSound = breachSound;

        // 3. Setup colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        Collider doorCollider = null;
        Collider triggerCollider = null;

        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
            }
            else
            {
                doorCollider = col;
            }
        }

        // Create trigger collider if none exists
        if (triggerCollider == null)
        {
            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 2.5f, 2f); // Detection zone
            triggerCollider = trigger;
            Debug.Log("Created trigger collider for player detection");
        }

        // Create door blocking collider if none exists
        if (doorCollider == null && doorMeshTransform != null)
        {
            BoxCollider blocker = doorMeshTransform.gameObject.AddComponent<BoxCollider>();
            blocker.isTrigger = false;
            doorCollider = blocker;
            Debug.Log("Created blocking collider on door mesh");
        }

        doorScript.doorCollider = doorCollider;

        // 4. Add DoorInteractable component
        DoorInteractable interactable = GetComponent<DoorInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<DoorInteractable>();
            Debug.Log("Added DoorInteractable component");
        }

        interactable.breachingChargePrefab = breachingChargePrefab;
        interactable.breachChargeItem = breachChargeItem;
        interactable.breachingChargesAvailable = breachChargesAvailable;

        // Optional snake cam.
        if (enableSnakeCam && GetComponent<DoorSnakeCam>() == null)
        {
            gameObject.AddComponent<DoorSnakeCam>();
            Debug.Log("Added DoorSnakeCam component");
        }

        // 5. Add PhotonView for networking
        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
            Debug.Log("Added PhotonView component");

            // Configure PhotonView for scene objects
            // DON'T set ViewID to 0 - scene objects need their ViewID preserved!
            pv.OwnershipTransfer = Photon.Pun.OwnershipOption.Fixed; // Scene objects should be Fixed ownership
        }
        else
        {
            Debug.Log($"PhotonView already exists with ViewID: {pv.ViewID}");
            // DON'T overwrite existing ViewID for scene objects!
        }

        // 6. Add Rigidbody for triggers to work
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log("Added Rigidbody component");
        }

        Debug.Log($"Tactical door setup complete for {gameObject.name}!");
    }

    // Button to re-setup door in editor
    [ContextMenu("Setup Door")]
    void ManualSetup()
    {
        SetupDoor();
    }
}
