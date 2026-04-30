using UnityEngine;

/// <summary>
/// Attach to characters with weapons to broadcast gunshot sounds to nearby AI.
/// Hooks into Opsive's ShootableAction to detect when weapons fire.
/// </summary>
public class GunshotBroadcaster : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far gunshots can be heard")]
    public float gunshotHearingRange = 50f;

    [Tooltip("Layer mask for AI that can hear gunshots")]
    public LayerMask aiLayer = ~0; // All layers by default

    private Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion characterLocomotion;

    void Start()
    {
        characterLocomotion = GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

        if (characterLocomotion != null)
        {
            // Subscribe to Use ability start event (when weapon fires)
            Opsive.Shared.Events.EventHandler.RegisterEvent<Opsive.UltimateCharacterController.Character.Abilities.Items.ItemAbility, bool>(
                gameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);

            Debug.Log($"GunshotBroadcaster initialized on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"GunshotBroadcaster: No UltimateCharacterLocomotion found on {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        if (characterLocomotion != null)
        {
            Opsive.Shared.Events.EventHandler.UnregisterEvent<Opsive.UltimateCharacterController.Character.Abilities.Items.ItemAbility, bool>(
                gameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);
        }
    }

    /// <summary>
    /// Called when character uses an item ability (like shooting)
    /// </summary>
    void OnItemAbilityActive(Opsive.UltimateCharacterController.Character.Abilities.Items.ItemAbility ability, bool active)
    {
        Debug.Log($"[GunshotBroadcaster] OnItemAbilityActive called: ability={ability.GetType().Name}, active={active}");

        // Only broadcast when ability starts (not when it stops)
        if (!active)
        {
            Debug.Log($"[GunshotBroadcaster] Ignoring - ability stopped");
            return;
        }

        // Check if it's the Use ability (shooting)
        if (ability is Opsive.UltimateCharacterController.Character.Abilities.Items.Use)
        {
            Debug.Log($"[GunshotBroadcaster] WEAPON FIRED! Broadcasting gunshot...");
            BroadcastGunshot();
        }
        else
        {
            Debug.Log($"[GunshotBroadcaster] Not a Use ability, ignoring");
        }
    }

    /// <summary>
    /// Notify all nearby AI that a gunshot was heard
    /// </summary>
    void BroadcastGunshot()
    {
        Debug.Log($"[GunshotBroadcaster] BroadcastGunshot START");

        // Find all colliders within hearing range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, gunshotHearingRange, aiLayer);
        Debug.Log($"[GunshotBroadcaster] Found {nearbyColliders.Length} nearby colliders");

        int notifiedCount = 0;

        foreach (var col in nearbyColliders)
        {
            // Don't notify self
            if (col.gameObject == gameObject) continue;

            // Check if it has TacticalAI component
            TacticalAI ai = col.GetComponent<TacticalAI>();
            if (ai != null && ai.enabled)
            {
                ai.OnGunshotHeard(transform.position, gameObject);
                notifiedCount++;
            }
        }

        Debug.Log($"[GunshotBroadcaster] Notified {notifiedCount} AI via OnGunshotHeard");

        // ALSO notify room awareness system for adjacent room detection
        Debug.Log($"[GunshotBroadcaster] About to call BroadcastToRoomAwareness...");
        BroadcastToRoomAwareness();
        Debug.Log($"[GunshotBroadcaster] BroadcastGunshot COMPLETE");
    }

    /// <summary>
    /// Notify AI in adjacent rooms about this gunshot
    /// </summary>
    void BroadcastToRoomAwareness()
    {
        Debug.Log($"[GunshotBroadcaster] BroadcastToRoomAwareness called");

        // Get the room this shooter is in
        var shooterRoom = Klyra.AI.RoomManager.GetRoomAtPosition(transform.position);
        if (shooterRoom == null)
        {
            Debug.LogWarning($"[GunshotBroadcaster] Shooter not in any room! Position: {transform.position}");
            return;
        }

        Debug.Log($"[GunshotBroadcaster] Shooter in room: {shooterRoom.roomName}, Adjacent rooms: {shooterRoom.adjacentRooms.Count}");

        // Notify all AI in adjacent rooms
        foreach (var adjacentRoom in shooterRoom.adjacentRooms)
        {
            if (adjacentRoom == null) continue;

            Debug.Log($"[GunshotBroadcaster] Checking adjacent room: {adjacentRoom.roomName}");

            // Get all AI in that adjacent room
            var aiInAdjacentRoom = adjacentRoom.GetAIInRoom();
            Debug.Log($"[GunshotBroadcaster] Found {aiInAdjacentRoom.Count} AI in {adjacentRoom.roomName}");

            foreach (var ai in aiInAdjacentRoom)
            {
                if (ai == null || ai.gameObject == gameObject) continue;

                float distance = Vector3.Distance(transform.position, ai.transform.position);
                Debug.Log($"[GunshotBroadcaster] AI {ai.name} is {distance:F1}m away (max: {gunshotHearingRange})");

                if (distance <= gunshotHearingRange)
                {
                    var roomAwareness = ai.GetComponent<Klyra.AI.AIRoomAwareness>();
                    if (roomAwareness != null)
                    {
                        Debug.Log($"[GunshotBroadcaster] Notifying {ai.name}'s AIRoomAwareness about gunshot from {shooterRoom.roomName}!");
                        // Notify AI that they heard gunshot from adjacent room
                        // Get the AI's current room
                        var aiRoom = ai.GetCurrentRoom();
                        if (aiRoom != null)
                        {
                            // Call OnHeardGunshotFromAdjacentRoom (shooter's room, listener's room)
                            roomAwareness.OnHeardGunshotFromAdjacentRoom(shooterRoom, aiRoom);
                            Debug.Log($"[GunshotBroadcaster] Successfully notified {ai.name} in {aiRoom.roomName} about gunshot from {shooterRoom.roomName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[GunshotBroadcaster] AI {ai.name} has no current room - can't notify!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[GunshotBroadcaster] AI {ai.name} has no AIRoomAwareness component!");
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize gunshot hearing range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, gunshotHearingRange);
    }
}
