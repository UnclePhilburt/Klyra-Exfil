using UnityEngine;
using System.Collections.Generic;

namespace Klyra.AI
{
    /// <summary>
    /// Connects your TacticalDoorSetup to the room system.
    /// Add this to the same GameObject as your TacticalDoorSetup component.
    /// It will automatically hook up the door to nearby rooms for AI awareness.
    /// </summary>
    public class DoorRoomConnector : MonoBehaviour
    {
        [Header("Room Connection")]
        [Tooltip("Rooms this door connects (automatically detected)")]
        public List<RoomVolume> connectedRooms = new List<RoomVolume>();

        [Header("Tactical Properties")]
        [Tooltip("Is this a choke point? (AI will prioritize defending it)")]
        public bool isChokePoint = true;

        [Tooltip("Cover quality when using door frame (1-10)")]
        [Range(1, 10)]
        public int doorFrameCoverQuality = 7;

        [Tooltip("Can AI use this door frame as cover?")]
        public bool canUseAsCover = true;

        [Header("Debug")]
        public bool debugDoorConnector = true;

        // Reference to TacticalDoorSetup
        private TacticalDoorSetup tacticalDoorSetup;

        public TacticalDoorSetup TacticalDoor => tacticalDoorSetup;

        void Awake()
        {
            tacticalDoorSetup = GetComponent<TacticalDoorSetup>();

            if (tacticalDoorSetup == null)
            {
                Debug.LogWarning($"[DoorRoomConnector] {gameObject.name} has no TacticalDoorSetup component!");
            }

            // Auto-register with RoomManager
            RoomManager.RegisterDoorConnector(this);
        }

        void Start()
        {
            // Auto-detect connected rooms (done in Start so all rooms are registered first)
            AutoDetectAndConnectRooms();
        }

        void OnDestroy()
        {
            RoomManager.UnregisterDoorConnector(this);
        }

        /// <summary>
        /// Automatically detect and connect to nearby rooms
        /// This is called in Start() so all rooms are already registered
        /// </summary>
        void AutoDetectAndConnectRooms()
        {
            // Clear any existing connections
            connectedRooms.Clear();

            // Get all rooms from RoomManager
            List<RoomVolume> allRooms = RoomManager.GetAllRooms();

            foreach (var room in allRooms)
            {
                // Check if door position is near room boundary
                // Use a slightly generous distance check
                float distance = Vector3.Distance(transform.position, room.GetCenter());
                float roomRadius = room.GetSize().magnitude / 2f;

                // Door should be within 3 units of room edge
                if (distance <= roomRadius + 3f)
                {
                    // Add this room to door's connections
                    if (!connectedRooms.Contains(room))
                    {
                        connectedRooms.Add(room);
                    }

                    // Add this door connector to room's door list
                    if (!room.doorConnectors.Contains(this))
                    {
                        room.doorConnectors.Add(this);
                    }

                    if (debugDoorConnector)
                    {
                        Debug.Log($"[DoorRoomConnector] {gameObject.name} connected to room: {room.roomName}");
                    }
                }
            }

            // Now connect the rooms to each other as adjacent
            // Since this door connects them, they are adjacent
            if (connectedRooms.Count >= 2)
            {
                for (int i = 0; i < connectedRooms.Count; i++)
                {
                    for (int j = i + 1; j < connectedRooms.Count; j++)
                    {
                        RoomVolume room1 = connectedRooms[i];
                        RoomVolume room2 = connectedRooms[j];

                        // Make room1 aware of room2
                        if (!room1.adjacentRooms.Contains(room2))
                        {
                            room1.adjacentRooms.Add(room2);
                        }

                        // Make room2 aware of room1
                        if (!room2.adjacentRooms.Contains(room1))
                        {
                            room2.adjacentRooms.Add(room1);
                        }

                        if (debugDoorConnector)
                        {
                            Debug.Log($"[DoorRoomConnector] {gameObject.name} linked adjacent rooms: {room1.roomName} <-> {room2.roomName}");
                        }
                    }
                }
            }

            if (debugDoorConnector && connectedRooms.Count > 0)
            {
                string roomNames = "";
                foreach (var room in connectedRooms)
                {
                    roomNames += room.roomName + ", ";
                }
                Debug.Log($"[DoorRoomConnector] {gameObject.name} auto-connected to: {roomNames}");
            }
            else if (debugDoorConnector)
            {
                Debug.LogWarning($"[DoorRoomConnector] {gameObject.name} found NO nearby rooms! Position: {transform.position}");
            }
        }

        /// <summary>
        /// Check if this door connects two specific rooms
        /// </summary>
        public bool ConnectsRooms(RoomVolume room1, RoomVolume room2)
        {
            return (connectedRooms.Contains(room1) && connectedRooms.Contains(room2));
        }

        /// <summary>
        /// Get the room on the other side of this door
        /// </summary>
        public RoomVolume GetOtherRoom(RoomVolume currentRoom)
        {
            foreach (var room in connectedRooms)
            {
                if (room != currentRoom)
                {
                    return room;
                }
            }
            return null;
        }

        /// <summary>
        /// Get tactical position for covering this door from a room
        /// </summary>
        public Vector3 GetCoverPositionFromRoom(RoomVolume room)
        {
            // Position slightly inside the room, facing the door
            Vector3 directionIntoRoom = (room.GetCenter() - transform.position).normalized;
            return transform.position + directionIntoRoom * 2f;
        }

        /// <summary>
        /// Check if door is open
        /// </summary>
        public bool IsOpen()
        {
            if (tacticalDoorSetup != null)
            {
                // TacticalDoorSetup creates a Door component, get it
                var door = GetComponent<global::Door>();
                return door != null && door.isOpen;
            }
            return false;
        }

        /// <summary>
        /// Check if door is locked
        /// </summary>
        public bool IsLocked()
        {
            if (tacticalDoorSetup != null)
            {
                // Check the startLocked property or the created Door component
                var door = GetComponent<global::Door>();
                return door != null && door.isLocked;
            }
            return false;
        }

        void OnDrawGizmos()
        {
            if (!debugDoorConnector) return;

            // Draw door position
            Gizmos.color = IsOpen() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw door direction
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);

            // Draw connections to rooms
            Gizmos.color = Color.cyan;
            foreach (var room in connectedRooms)
            {
                if (room != null)
                {
                    Gizmos.DrawLine(transform.position, room.GetCenter());
                }
            }

            // Draw door label
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up;
            string stateLabel = IsLocked() ? "[LOCKED]" : (IsOpen() ? "[OPEN]" : "[CLOSED]");
            UnityEditor.Handles.Label(labelPos, $"{gameObject.name} {stateLabel}");
            #endif

            // Draw choke point indicator
            if (isChokePoint)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawSphere(transform.position, 1f);
            }
        }
    }
}
