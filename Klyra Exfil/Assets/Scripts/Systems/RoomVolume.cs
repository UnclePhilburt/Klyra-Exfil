using UnityEngine;
using System.Collections.Generic;

namespace Klyra.AI
{
    /// <summary>
    /// Defines a room volume that AI can be aware of.
    /// Place a cube with this component to define room boundaries.
    /// AI will know when they enter/exit rooms.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoomVolume : MonoBehaviour
    {
        [Header("Room Identity")]
        [Tooltip("Room name (e.g., 'Lobby', 'Room 203', 'Kitchen')")]
        public string roomName = "Unnamed Room";

        [Tooltip("Room type for AI behavior customization")]
        public RoomType roomType = RoomType.Generic;

        [Tooltip("Floor number (for multi-story buildings)")]
        public int floorNumber = 1;

        [Header("Room Properties")]
        [Tooltip("Is this a high-value room (objective, armory, etc.)?")]
        public bool isHighValueRoom = false;

        [Tooltip("Can AI use this room for fallback/regrouping?")]
        public bool canUseForRegroup = true;

        [Tooltip("Is this room defendable (good cover, limited entry points)?")]
        public bool isDefendable = true;

        [Header("Connected Rooms")]
        [Tooltip("Rooms connected to this one (for pathfinding and flanking)")]
        public List<RoomVolume> adjacentRooms = new List<RoomVolume>();

        [Header("Doors")]
        [Tooltip("Door connectors that lead into/out of this room (auto-populated)")]
        public List<DoorRoomConnector> doorConnectors = new List<DoorRoomConnector>();

        [Header("Tactical Info")]
        [Tooltip("Good cover points in this room")]
        public List<CoverPoint> coverPoints = new List<CoverPoint>();

        [Tooltip("Strategic positions (corners, doorways, etc.)")]
        public List<Transform> tacticalPositions = new List<Transform>();

        [Header("Debug")]
        [Tooltip("Show room boundaries in scene view")]
        public bool showGizmos = true;

        [Tooltip("Gizmo color")]
        public Color gizmoColor = new Color(0, 1, 1, 0.2f);

        // Runtime tracking
        private HashSet<GameObject> occupants = new HashSet<GameObject>();
        private BoxCollider roomCollider;

        public enum RoomType
        {
            Generic,        // Normal room
            Hallway,        // Corridor/hallway
            Stairwell,      // Stairs between floors
            Entrance,       // Building entrance/exit
            Office,         // Office room
            Storage,        // Storage/closet
            Bathroom,       // Bathroom
            Kitchen,        // Kitchen/break room
            Armory,         // Weapon storage
            ServerRoom,     // High-value tech room
            MeetingRoom,    // Conference room
            Lobby,          // Main lobby
            Exterior        // Outdoor area
        }

        void Awake()
        {
            roomCollider = GetComponent<BoxCollider>();
            if (roomCollider != null)
            {
                roomCollider.isTrigger = true; // Must be trigger to detect entry/exit
            }

            // Auto-register with RoomManager
            RoomManager.RegisterRoom(this);
        }

        void Start()
        {
            // Auto-find cover points if none assigned (done in Start so cover points exist)
            if (coverPoints.Count == 0)
            {
                AutoDetectCoverPoints();
            }

            // Note: Doors auto-connect themselves to rooms, no need to search for them here
        }

        void OnDestroy()
        {
            RoomManager.UnregisterRoom(this);
        }

        void OnTriggerEnter(Collider other)
        {
            // Track who enters this room
            GameObject entity = other.gameObject;

            // Check if it's AI or player - try the collider object first, then check parents
            var tacticalAI = entity.GetComponent<TacticalAI>();
            var player = entity.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

            // If not found, check parent (in case collider is on a child object like "Head")
            if (tacticalAI == null && player == null)
            {
                tacticalAI = entity.GetComponentInParent<TacticalAI>();
                player = entity.GetComponentInParent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

                // Use the parent as the entity to track
                if (tacticalAI != null)
                    entity = tacticalAI.gameObject;
                else if (player != null)
                    entity = player.gameObject;
            }

            if (tacticalAI != null || player != null)
            {
                if (occupants.Add(entity))
                {
                    // Notify AI they entered this room
                    if (tacticalAI != null)
                    {
                        tacticalAI.OnEnteredRoom(this);
                    }

                    Debug.Log($"[RoomVolume] {entity.name} entered {roomName}");
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            GameObject entity = other.gameObject;

            if (occupants.Remove(entity))
            {
                var tacticalAI = entity.GetComponent<TacticalAI>();
                if (tacticalAI != null)
                {
                    tacticalAI.OnExitedRoom(this);
                }

                if (showGizmos)
                {
                    Debug.Log($"[RoomVolume] {entity.name} exited {roomName}");
                }
            }
        }

        /// <summary>
        /// Get all current occupants in this room
        /// </summary>
        public HashSet<GameObject> GetOccupants()
        {
            return new HashSet<GameObject>(occupants);
        }

        /// <summary>
        /// Get all AI in this room
        /// </summary>
        public List<TacticalAI> GetAIInRoom()
        {
            List<TacticalAI> aiList = new List<TacticalAI>();
            foreach (var occupant in occupants)
            {
                var ai = occupant.GetComponent<TacticalAI>();
                if (ai != null)
                {
                    aiList.Add(ai);
                }
            }
            return aiList;
        }

        /// <summary>
        /// Get all players in this room
        /// </summary>
        public List<GameObject> GetPlayersInRoom()
        {
            List<GameObject> players = new List<GameObject>();
            foreach (var occupant in occupants)
            {
                var player = occupant.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                if (player != null)
                {
                    players.Add(occupant);
                }
            }
            return players;
        }

        /// <summary>
        /// Check if this room contains any players
        /// </summary>
        public bool HasPlayers()
        {
            foreach (var occupant in occupants)
            {
                var player = occupant.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                if (player != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if this room contains any AI
        /// </summary>
        public bool HasAI()
        {
            foreach (var occupant in occupants)
            {
                var ai = occupant.GetComponent<TacticalAI>();
                if (ai != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get number of occupants
        /// </summary>
        public int GetOccupantCount()
        {
            return occupants.Count;
        }

        /// <summary>
        /// Check if a position is inside this room
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            if (roomCollider == null)
            {
                Debug.LogWarning($"[RoomVolume] {roomName} has null collider!");
                return false;
            }

            bool contains = roomCollider.bounds.Contains(position);

            // Temporary debug
            if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                Debug.Log($"[RoomVolume] {roomName} checking position {position}: bounds={roomCollider.bounds.min} to {roomCollider.bounds.max}, contains={contains}");
            }

            return contains;
        }

        /// <summary>
        /// Get center of room
        /// </summary>
        public Vector3 GetCenter()
        {
            if (roomCollider != null)
                return roomCollider.bounds.center;
            return transform.position;
        }

        /// <summary>
        /// Get room size
        /// </summary>
        public Vector3 GetSize()
        {
            if (roomCollider != null)
                return roomCollider.bounds.size;
            return Vector3.one * 5f;
        }

        /// <summary>
        /// Automatically find cover points in this room
        /// </summary>
        void AutoDetectCoverPoints()
        {
            CoverPoint[] allCover = FindObjectsOfType<CoverPoint>();
            foreach (var cover in allCover)
            {
                if (ContainsPosition(cover.transform.position))
                {
                    if (!coverPoints.Contains(cover))
                    {
                        coverPoints.Add(cover);
                    }
                }
            }
        }

        /// <summary>
        /// Find doors that connect to a specific adjacent room
        /// </summary>
        public List<DoorRoomConnector> GetDoorsTo(RoomVolume otherRoom)
        {
            List<DoorRoomConnector> connectingDoors = new List<DoorRoomConnector>();
            foreach (var doorConnector in doorConnectors)
            {
                if (doorConnector.ConnectsRooms(this, otherRoom))
                {
                    connectingDoors.Add(doorConnector);
                }
            }
            return connectingDoors;
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;

            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            // Draw room volume
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);

            // Draw room outline
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);

            // Draw room label
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"{roomName}\nFloor {floorNumber}");
            #endif

            // Draw connections to adjacent rooms
            Gizmos.color = Color.yellow;
            foreach (var adjRoom in adjacentRooms)
            {
                if (adjRoom != null)
                {
                    Gizmos.DrawLine(GetCenter(), adjRoom.GetCenter());
                }
            }

            // Draw door connections
            Gizmos.color = Color.cyan;
            foreach (var doorConnector in doorConnectors)
            {
                if (doorConnector != null)
                {
                    Gizmos.DrawLine(GetCenter(), doorConnector.transform.position);
                }
            }
        }
    }
}
