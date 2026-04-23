using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.UltimateCharacterController.Character;
using Opsive.Shared.Input;
using Photon.Pun;

/// <summary>
/// Simple door interaction using key press when player is near.
/// Add a trigger collider to detect when player is nearby.
/// </summary>
[RequireComponent(typeof(Door))]
public class DoorInteractable : MonoBehaviour
{
    private Door door;

    [Header("Interaction Settings")]
    [Tooltip("Button name from UCC Input (default is 'Interact')")]
    public string interactButton = "Interact";

    [Tooltip("Key to press to breach door (kick)")]
    public Key breachKey = Key.E;

    [Tooltip("Key to press to place breaching charge")]
    public Key placeChargeKey = Key.Q;

    [Header("Breaching Charge")]
    [Tooltip("Breaching charge prefab to spawn")]
    public GameObject breachingChargePrefab;

    [Tooltip("Player has breaching charges available?")]
    public int breachingChargesAvailable = 3;

    [Tooltip("How close player needs to be to interact")]
    public float interactionDistance = 2f;

    [Header("UI")]
    [Tooltip("Show interaction prompt?")]
    public bool showPrompt = true;

    [Tooltip("Text to display when near door")]
    public string promptText = "Press F to interact";

    private bool playerNearby = false;
    private GameObject nearbyPlayer;
    private DoorRadialMenu radialMenu;
    private bool isHoldingF = false;
    private GUIStyle promptStyle;

    void Awake()
    {
        door = GetComponent<Door>();

        // Add radial menu component
        radialMenu = gameObject.AddComponent<DoorRadialMenu>();
        radialMenu.SetDoor(door, this);

        // Make sure we have a rigidbody for triggers to work
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // Don't let physics move the door
        rb.useGravity = false;

        // Setup GUI style
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 18;
        promptStyle.normal.textColor = Color.white;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.fontStyle = FontStyle.Bold;
    }

    void Update()
    {
        // Check if player is nearby
        if (playerNearby && nearbyPlayer != null)
        {
            if (Keyboard.current != null)
            {
                // Check if F key is being held
                isHoldingF = Keyboard.current.fKey.isPressed;

                // Update radial menu
                radialMenu.UpdateMenu(isHoldingF);

                // Update selection while menu is open
                if (radialMenu.IsMenuOpen())
                {
                    radialMenu.UpdateSelection();
                }
            }
        }
        else
        {
            // Player left, close menu if open
            if (radialMenu != null && radialMenu.IsMenuOpen())
            {
                radialMenu.ForceClose();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name}");

        // Ignore our own colliders
        if (other.transform.IsChildOf(transform) || other.gameObject == gameObject)
        {
            Debug.Log("Ignoring self collision");
            return;
        }

        // Check if it's a character (check both the object and its parent)
        UltimateCharacterLocomotion characterLocomotion = other.GetComponent<UltimateCharacterLocomotion>();
        if (characterLocomotion == null)
        {
            characterLocomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
        }

        if (characterLocomotion != null)
        {
            // Check if character is alive (has Health component and is alive)
            Opsive.UltimateCharacterController.Traits.Health health = characterLocomotion.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
            if (health != null && !health.IsAlive())
            {
                Debug.Log("Dead character detected - ignoring");
                return;
            }

            Debug.Log("Player detected! You can now interact with the door.");
            playerNearby = true;
            nearbyPlayer = characterLocomotion.gameObject;
        }
        else
        {
            Debug.Log("Not a player character.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's the player leaving
        UltimateCharacterLocomotion characterLocomotion = other.GetComponent<UltimateCharacterLocomotion>();
        if (characterLocomotion == null)
        {
            characterLocomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
        }

        if (characterLocomotion != null && characterLocomotion.gameObject == nearbyPlayer)
        {
            Debug.Log("Player left interaction range.");
            playerNearby = false;
            nearbyPlayer = null;
        }
    }

    public void PlaceExplosiveCharge()
    {
        // Get the center of the door mesh - check all children for renderers
        Vector3 chargePosition = Vector3.zero;
        bool foundRenderer = false;

        // Check door transform and all children for renderers
        Renderer[] renderers = door.doorTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calculate combined bounds of all renderers
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            chargePosition = combinedBounds.center;
            foundRenderer = true;
            Debug.Log($"Found {renderers.Length} renderers, center at {chargePosition}");
        }

        if (!foundRenderer)
        {
            // Fallback to door transform position
            chargePosition = door.doorTransform.position;
            Debug.LogWarning("No renderer found, using transform position");
        }

        // Determine which side of the door the player is on
        Vector3 doorToPlayer = (nearbyPlayer.transform.position - chargePosition).normalized;
        Vector3 doorForward = door.doorTransform.forward;

        // Check which side the player is on
        float dot = Vector3.Dot(doorToPlayer, doorForward);

        // Place charge on player's side - if dot is positive, player is on forward side
        Vector3 offset = (dot > 0) ? doorForward : -doorForward;

        // Offset slightly from the door surface toward the player's side
        chargePosition += offset * 0.05f;

        Debug.Log($"Placing charge at: {chargePosition}, player side dot: {dot}, offset: {offset}");

        // Calculate rotation - face the charge toward the door (flip 180)
        Quaternion chargeRotation = Quaternion.LookRotation(offset, Vector3.up);

        // Spawn the charge over network
        GameObject charge;
        if (PhotonNetwork.IsConnected)
        {
            // Network spawn - must be in Resources folder
            Debug.Log($"Spawning charge via PhotonNetwork.Instantiate: {breachingChargePrefab.name}");
            charge = PhotonNetwork.Instantiate(breachingChargePrefab.name, chargePosition, chargeRotation);
            Debug.Log($"Spawned charge: {charge.name}, InstanceID: {charge.GetInstanceID()}");
        }
        else
        {
            // Local spawn for testing
            charge = Instantiate(breachingChargePrefab, chargePosition, chargeRotation);
        }

        BreachingCharge chargeScript = charge.GetComponent<BreachingCharge>();
        if (chargeScript != null)
        {
            // Only arm the charge if we own it (prevents double-arming from network)
            PhotonView chargeView = charge.GetComponent<PhotonView>();
            if (chargeView == null || chargeView.IsMine || !PhotonNetwork.IsConnected)
            {
                Debug.Log($"Arming charge {charge.name} (ID: {charge.GetInstanceID()}) on door {door.name}");
                chargeScript.Arm(door);
                breachingChargesAvailable--;
                Debug.Log($"Breaching charge placed! {breachingChargesAvailable} remaining.");
            }
            else
            {
                Debug.Log($"Skipping arm - charge {charge.name} is not mine (IsMine: {chargeView.IsMine})");
            }
        }
        else
        {
            Debug.LogError($"Spawned charge {charge.name} has no BreachingCharge component!");
        }
    }

    void OnGUI()
    {
        // Don't show prompt if door or doorTransform is destroyed or if radial menu is open
        if (showPrompt && playerNearby && door != null && door.doorTransform != null && !radialMenu.IsMenuOpen() && Camera.main != null)
        {
            // Get door position in world space
            Vector3 doorWorldPos = door.doorTransform.position + Vector3.up * 1.5f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(doorWorldPos);

            // Only show if in front of camera
            if (screenPos.z > 0)
            {
                // Flip Y coordinate (GUI coords are top-left origin)
                screenPos.y = Screen.height - screenPos.y;

                // Determine message
                string message;
                if (door.isOpen)
                {
                    message = "Tap F to close | Hold F for options";
                }
                else
                {
                    message = "Tap F to open | Hold F for options";
                }

                // Draw background box
                Rect bgRect = new Rect(screenPos.x - 200, screenPos.y - 15, 400, 30);
                GUI.Box(bgRect, "");

                // Draw text
                Rect textRect = new Rect(screenPos.x - 200, screenPos.y - 15, 400, 30);
                GUI.Label(textRect, message, promptStyle);
            }
        }
    }
}
