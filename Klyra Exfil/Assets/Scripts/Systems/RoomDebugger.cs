using UnityEngine;

namespace Klyra.AI
{
    /// <summary>
    /// Temporary debug script to test room system.
    /// Attach to player to see which room you're in.
    /// </summary>
    public class RoomDebugger : MonoBehaviour
    {
        private RoomVolume currentRoom;
        private float lastCheckTime;

        void Update()
        {
            // Check room every second
            if (Time.time - lastCheckTime > 1f)
            {
                lastCheckTime = Time.time;

                var room = RoomManager.GetRoomAtPosition(transform.position);

                if (room != currentRoom)
                {
                    currentRoom = room;

                    if (room != null)
                    {
                        Debug.Log($"[RoomDebugger] You entered: {room.roomName}");
                        Debug.Log($"[RoomDebugger] Adjacent rooms: {room.adjacentRooms.Count}");

                        foreach (var adjacent in room.adjacentRooms)
                        {
                            Debug.Log($"[RoomDebugger]   -> {adjacent.roomName}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[RoomDebugger] You are not in any room!");
                    }
                }
            }
        }
    }
}
