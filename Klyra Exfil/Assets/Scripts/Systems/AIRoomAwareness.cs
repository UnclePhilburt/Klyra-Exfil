using UnityEngine;
using System.Collections.Generic;

namespace Klyra.AI
{
    /// <summary>
    /// Makes AI respond to sounds/combat in adjacent rooms.
    /// AI will go on alert and face doors when hearing gunfire nearby.
    /// </summary>
    [RequireComponent(typeof(TacticalAI))]
    public class AIRoomAwareness : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("How far away can AI hear gunfire (in meters)")]
        public float gunshotHearingRange = 30f;

        [Tooltip("How long AI stays alert after hearing gunfire")]
        public float alertDuration = 10f;

        [Tooltip("Should AI face the door when hearing sounds from adjacent room?")]
        public bool faceDoorOnAlert = true;

        [Header("Debug")]
        public bool debugRoomAwareness = true;

        // Components
        private TacticalAI tacticalAI;
        private RoomVolume currentRoom;
        private bool isAlert = false;
        private float alertTimer = 0f;
        private DoorRoomConnector doorToWatch = null;

        // Static list to broadcast gunshot events
        private static List<AIRoomAwareness> allAI = new List<AIRoomAwareness>();

        void Awake()
        {
            tacticalAI = GetComponent<TacticalAI>();
            allAI.Add(this);
        }

        void OnDestroy()
        {
            allAI.Remove(this);
        }

        void Update()
        {
            // Update current room
            currentRoom = tacticalAI.GetCurrentRoom();

            // Handle alert state
            if (isAlert)
            {
                alertTimer -= Time.deltaTime;
                if (alertTimer <= 0f)
                {
                    isAlert = false;
                    doorToWatch = null;

                    if (debugRoomAwareness)
                    {
                        Debug.Log($"[AIRoomAwareness] {gameObject.name} alert expired, returning to normal");
                    }
                }
                else
                {
                    // Face the door while alert
                    if (faceDoorOnAlert && doorToWatch != null)
                    {
                        Vector3 doorDirection = (doorToWatch.transform.position - transform.position).normalized;
                        doorDirection.y = 0; // Keep on horizontal plane

                        if (doorDirection != Vector3.zero)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(doorDirection);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call this when this AI fires their weapon
        /// Broadcasts to nearby AI in adjacent rooms
        /// </summary>
        public void OnWeaponFired()
        {
            if (debugRoomAwareness)
            {
                Debug.Log($"[AIRoomAwareness] {gameObject.name} fired weapon! Current room: {(currentRoom != null ? currentRoom.roomName : "NONE")}");
            }

            if (currentRoom == null)
            {
                if (debugRoomAwareness)
                {
                    Debug.LogWarning($"[AIRoomAwareness] {gameObject.name} not in any room - cannot notify adjacent rooms!");
                }
                return;
            }

            if (debugRoomAwareness)
            {
                Debug.Log($"[AIRoomAwareness] {gameObject.name} in {currentRoom.roomName}, checking {currentRoom.adjacentRooms.Count} adjacent rooms");
            }

            // Notify all AI in adjacent rooms
            foreach (var adjacentRoom in currentRoom.adjacentRooms)
            {
                if (adjacentRoom == null) continue;

                // Get all AI in that adjacent room
                List<TacticalAI> aiInAdjacentRoom = adjacentRoom.GetAIInRoom();

                if (debugRoomAwareness)
                {
                    Debug.Log($"[AIRoomAwareness] Adjacent room {adjacentRoom.roomName} has {aiInAdjacentRoom.Count} AI");
                }

                foreach (var ai in aiInAdjacentRoom)
                {
                    if (ai == null || ai.gameObject == gameObject) continue;

                    float distance = Vector3.Distance(transform.position, ai.transform.position);
                    if (distance <= gunshotHearingRange)
                    {
                        var roomAwareness = ai.GetComponent<AIRoomAwareness>();
                        if (roomAwareness != null)
                        {
                            if (debugRoomAwareness)
                            {
                                Debug.Log($"[AIRoomAwareness] Notifying {ai.name} in {adjacentRoom.roomName} about gunshot!");
                            }
                            roomAwareness.OnHeardGunshotFromAdjacentRoom(currentRoom, adjacentRoom);
                        }
                        else
                        {
                            if (debugRoomAwareness)
                            {
                                Debug.LogWarning($"[AIRoomAwareness] {ai.name} has no AIRoomAwareness component!");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when AI hears gunfire from an adjacent room
        /// </summary>
        public void OnHeardGunshotFromAdjacentRoom(RoomVolume sourceRoom, RoomVolume myRoom)
        {
            if (debugRoomAwareness)
            {
                string sourceName = sourceRoom.roomName ?? "Unknown Room";
                string myRoomName = myRoom.roomName ?? "My Room";
                Debug.Log($"[AIRoomAwareness] {gameObject.name} heard gunfire from {sourceName} while in {myRoomName}!");
            }

            // Go on alert
            isAlert = true;
            alertTimer = alertDuration;

            // Find door that connects to the source room
            doorToWatch = FindDoorToRoom(sourceRoom);

            if (doorToWatch != null && debugRoomAwareness)
            {
                Debug.Log($"[AIRoomAwareness] {gameObject.name} watching door at {doorToWatch.transform.position}");
            }

            // Increase alert level in TacticalAI
            if (tacticalAI != null)
            {
                tacticalAI.alertLevel = Mathf.Max(tacticalAI.alertLevel, 0.7f);
            }
        }

        /// <summary>
        /// Find the door that connects current room to target room
        /// </summary>
        DoorRoomConnector FindDoorToRoom(RoomVolume targetRoom)
        {
            if (currentRoom == null) return null;

            // Check all doors in current room
            foreach (var door in currentRoom.doorConnectors)
            {
                if (door.ConnectsRooms(currentRoom, targetRoom))
                {
                    return door;
                }
            }

            return null;
        }

        /// <summary>
        /// Static method to broadcast gunshot to all nearby AI
        /// Call this from weapon fire code
        /// </summary>
        public static void BroadcastGunshot(Vector3 position, GameObject shooter)
        {
            foreach (var ai in allAI)
            {
                if (ai == null || ai.gameObject == shooter) continue;

                float distance = Vector3.Distance(position, ai.transform.position);
                if (distance <= ai.gunshotHearingRange)
                {
                    ai.OnHeardGunshot(position, shooter);
                }
            }
        }

        /// <summary>
        /// Called when AI hears any gunshot nearby
        /// </summary>
        void OnHeardGunshot(Vector3 position, GameObject shooter)
        {
            // Check if shooter is in an adjacent room
            var shooterAI = shooter.GetComponent<TacticalAI>();
            if (shooterAI != null)
            {
                var shooterRoom = shooterAI.GetCurrentRoom();
                if (shooterRoom != null && currentRoom != null && currentRoom.adjacentRooms.Contains(shooterRoom))
                {
                    OnHeardGunshotFromAdjacentRoom(shooterRoom, currentRoom);
                }
            }
        }

        /// <summary>
        /// Check if AI is currently on alert from adjacent room sounds
        /// </summary>
        public bool IsOnAlert()
        {
            return isAlert;
        }

        /// <summary>
        /// Get the door this AI is currently watching
        /// </summary>
        public DoorRoomConnector GetWatchedDoor()
        {
            return doorToWatch;
        }

        void OnDrawGizmos()
        {
            if (!debugRoomAwareness) return;

            // Draw alert status
            if (isAlert && doorToWatch != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, doorToWatch.transform.position);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
            }
        }
    }
}
