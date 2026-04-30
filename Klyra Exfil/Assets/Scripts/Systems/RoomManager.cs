using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Klyra.AI
{
    /// <summary>
    /// Central manager for all rooms and doors in the level.
    /// Provides queries for AI to understand building layout.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        private static RoomManager instance;

        [Header("Debug")]
        public bool debugRoomSystem = true;

        // Static registries
        private static List<RoomVolume> allRooms = new List<RoomVolume>();
        private static List<DoorRoomConnector> allDoorConnectors = new List<DoorRoomConnector>();
        private static Dictionary<int, List<RoomVolume>> roomsByFloor = new Dictionary<int, List<RoomVolume>>();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            BuildRoomGraph();
        }

        #region Static Registration

        public static void RegisterRoom(RoomVolume room)
        {
            if (!allRooms.Contains(room))
            {
                allRooms.Add(room);

                // Add to floor dictionary
                if (!roomsByFloor.ContainsKey(room.floorNumber))
                {
                    roomsByFloor[room.floorNumber] = new List<RoomVolume>();
                }
                roomsByFloor[room.floorNumber].Add(room);

                if (instance != null && instance.debugRoomSystem)
                {
                    Debug.Log($"[RoomManager] Registered room: {room.roomName} (Floor {room.floorNumber})");
                }
            }
        }

        public static void UnregisterRoom(RoomVolume room)
        {
            allRooms.Remove(room);

            if (roomsByFloor.ContainsKey(room.floorNumber))
            {
                roomsByFloor[room.floorNumber].Remove(room);
            }
        }

        public static void RegisterDoorConnector(DoorRoomConnector doorConnector)
        {
            if (!allDoorConnectors.Contains(doorConnector))
            {
                allDoorConnectors.Add(doorConnector);

                if (instance != null && instance.debugRoomSystem)
                {
                    Debug.Log($"[RoomManager] Registered door connector: {doorConnector.gameObject.name}");
                }
            }
        }

        public static void UnregisterDoorConnector(DoorRoomConnector doorConnector)
        {
            allDoorConnectors.Remove(doorConnector);
        }

        #endregion

        #region Building Graph

        /// <summary>
        /// Build connections between rooms based on doors
        /// </summary>
        void BuildRoomGraph()
        {
            if (debugRoomSystem)
            {
                Debug.Log($"[RoomManager] Building room graph - {allRooms.Count} rooms, {allDoorConnectors.Count} door connectors");
            }

            // Note: DoorRoomConnectors now handle adjacency connections automatically in their Start()
            // This method is kept for future expansion

            if (debugRoomSystem)
            {
                Debug.Log($"[RoomManager] Room graph built successfully");
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get all rooms in the level
        /// </summary>
        public static List<RoomVolume> GetAllRooms()
        {
            return new List<RoomVolume>(allRooms);
        }

        /// <summary>
        /// Get all door connectors in the level
        /// </summary>
        public static List<DoorRoomConnector> GetAllDoorConnectors()
        {
            return new List<DoorRoomConnector>(allDoorConnectors);
        }

        /// <summary>
        /// Get all rooms on a specific floor
        /// </summary>
        public static List<RoomVolume> GetRoomsByFloor(int floor)
        {
            if (roomsByFloor.ContainsKey(floor))
            {
                return new List<RoomVolume>(roomsByFloor[floor]);
            }
            return new List<RoomVolume>();
        }

        /// <summary>
        /// Find which room a position is in
        /// </summary>
        public static RoomVolume GetRoomAtPosition(Vector3 position)
        {
            Debug.Log($"[RoomManager] GetRoomAtPosition called for {position}, checking {allRooms.Count} rooms");

            foreach (var room in allRooms)
            {
                if (room != null && room.ContainsPosition(position))
                {
                    Debug.Log($"[RoomManager] Found room: {room.roomName}");
                    return room;
                }
            }

            Debug.LogWarning($"[RoomManager] No room found for position {position}! Registered rooms: {allRooms.Count}");
            return null;
        }

        /// <summary>
        /// Find the nearest room to a position
        /// </summary>
        public static RoomVolume GetNearestRoom(Vector3 position)
        {
            RoomVolume nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var room in allRooms)
            {
                float distance = Vector3.Distance(position, room.GetCenter());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = room;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find all rooms with players in them
        /// </summary>
        public static List<RoomVolume> GetRoomsWithPlayers()
        {
            List<RoomVolume> roomsWithPlayers = new List<RoomVolume>();
            foreach (var room in allRooms)
            {
                if (room.HasPlayers())
                {
                    roomsWithPlayers.Add(room);
                }
            }
            return roomsWithPlayers;
        }

        /// <summary>
        /// Find all rooms with AI in them
        /// </summary>
        public static List<RoomVolume> GetRoomsWithAI()
        {
            List<RoomVolume> roomsWithAI = new List<RoomVolume>();
            foreach (var room in allRooms)
            {
                if (room.HasAI())
                {
                    roomsWithAI.Add(room);
                }
            }
            return roomsWithAI;
        }

        /// <summary>
        /// Find doors between two rooms
        /// </summary>
        public static List<DoorRoomConnector> GetDoorsBetweenRooms(RoomVolume room1, RoomVolume room2)
        {
            List<DoorRoomConnector> connectingDoors = new List<DoorRoomConnector>();
            foreach (var doorConnector in allDoorConnectors)
            {
                if (doorConnector.ConnectsRooms(room1, room2))
                {
                    connectingDoors.Add(doorConnector);
                }
            }
            return connectingDoors;
        }

        /// <summary>
        /// Find path from one room to another (simple BFS)
        /// </summary>
        public static List<RoomVolume> FindRoomPath(RoomVolume start, RoomVolume end)
        {
            if (start == end) return new List<RoomVolume> { start };

            Queue<RoomVolume> queue = new Queue<RoomVolume>();
            Dictionary<RoomVolume, RoomVolume> cameFrom = new Dictionary<RoomVolume, RoomVolume>();
            HashSet<RoomVolume> visited = new HashSet<RoomVolume>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                RoomVolume current = queue.Dequeue();

                if (current == end)
                {
                    // Build path
                    List<RoomVolume> path = new List<RoomVolume>();
                    RoomVolume step = end;
                    while (step != null)
                    {
                        path.Add(step);
                        cameFrom.TryGetValue(step, out step);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in current.adjacentRooms)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return new List<RoomVolume>(); // No path found
        }

        /// <summary>
        /// Find rooms adjacent to a given room
        /// </summary>
        public static List<RoomVolume> GetAdjacentRooms(RoomVolume room)
        {
            return new List<RoomVolume>(room.adjacentRooms);
        }

        /// <summary>
        /// Get all high-value rooms (objectives, armories, etc.)
        /// </summary>
        public static List<RoomVolume> GetHighValueRooms()
        {
            return allRooms.Where(r => r.isHighValueRoom).ToList();
        }

        /// <summary>
        /// Get all defendable rooms
        /// </summary>
        public static List<RoomVolume> GetDefendableRooms()
        {
            return allRooms.Where(r => r.isDefendable).ToList();
        }

        /// <summary>
        /// Get all choke point doors
        /// </summary>
        public static List<DoorRoomConnector> GetChokePointDoors()
        {
            return allDoorConnectors.Where(d => d.isChokePoint).ToList();
        }

        /// <summary>
        /// Get doors on a specific floor
        /// </summary>
        public static List<DoorRoomConnector> GetDoorsOnFloor(int floor)
        {
            List<DoorRoomConnector> doorsOnFloor = new List<DoorRoomConnector>();
            List<RoomVolume> roomsOnFloor = GetRoomsByFloor(floor);

            foreach (var doorConnector in allDoorConnectors)
            {
                foreach (var room in doorConnector.connectedRooms)
                {
                    if (roomsOnFloor.Contains(room))
                    {
                        doorsOnFloor.Add(doorConnector);
                        break;
                    }
                }
            }

            return doorsOnFloor;
        }

        #endregion

        #region Debug

        [ContextMenu("Print Room Summary")]
        void PrintRoomSummary()
        {
            Debug.Log("=== ROOM SYSTEM SUMMARY ===");
            Debug.Log($"Total Rooms: {allRooms.Count}");
            Debug.Log($"Total Door Connectors: {allDoorConnectors.Count}");
            Debug.Log($"Floors: {roomsByFloor.Keys.Count}");

            foreach (var floor in roomsByFloor.Keys.OrderBy(f => f))
            {
                Debug.Log($"Floor {floor}: {roomsByFloor[floor].Count} rooms");
            }

            Debug.Log($"High-Value Rooms: {GetHighValueRooms().Count}");
            Debug.Log($"Defendable Rooms: {GetDefendableRooms().Count}");
            Debug.Log($"Choke Points: {GetChokePointDoors().Count}");
        }

        [ContextMenu("Print All Rooms")]
        void PrintAllRooms()
        {
            Debug.Log("=== ALL ROOMS ===");
            foreach (var room in allRooms.OrderBy(r => r.floorNumber))
            {
                string adjacent = room.adjacentRooms.Count > 0
                    ? string.Join(", ", room.adjacentRooms.Select(r => r.roomName))
                    : "None";

                Debug.Log($"[Floor {room.floorNumber}] {room.roomName} ({room.roomType}) - Adjacent: {adjacent}");
            }
        }

        [ContextMenu("Print All Doors")]
        void PrintAllDoors()
        {
            Debug.Log("=== ALL DOOR CONNECTORS ===");
            foreach (var doorConnector in allDoorConnectors)
            {
                string rooms = doorConnector.connectedRooms.Count > 0
                    ? string.Join(" <-> ", doorConnector.connectedRooms.Select(r => r.roomName))
                    : "None";

                string state = doorConnector.IsLocked() ? "LOCKED" : (doorConnector.IsOpen() ? "OPEN" : "CLOSED");
                Debug.Log($"{doorConnector.gameObject.name} [{state}] - Connects: {rooms}");
            }
        }

        #endregion
    }
}
