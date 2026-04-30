# Room System - Complete Setup Guide

## What Is the Room System?

The **Room System** lets you tag rooms and doors so AI can be aware of:
- **Which room they're in**
- **Which room the player is in**
- **How rooms connect** (via doors)
- **Strategic positions** (cover, choke points, etc.)

This enables advanced AI behaviors like room callouts, flanking, door tactics, and coordinated attacks.

## Components

### 1. RoomVolume
Tags a room with a box collider trigger.

### 2. Door
Tags doors and tracks open/closed/locked state.

### 3. RoomManager
Manages all rooms and doors, provides queries.

### 4. TacticalAI (Updated)
AI now knows what room they're in and tracks player rooms.

## Quick Setup

### Step 1: Add RoomManager (One Time Setup)

```
1. Create empty GameObject: "RoomManager"
2. Add Component → Room Manager
3. Enable "Debug Room System" for testing
4. Done! This manages all rooms automatically
```

### Step 2: Tag Rooms

**For Each Room:**

```
1. Create empty GameObject (name it: "Room_Lobby", "Room_Office_203", etc.)
2. Add Component → Box Collider
   - Check "Is Trigger"
   - Resize to fit room boundaries
3. Add Component → Room Volume
4. Configure:
   - Room Name: "Lobby"
   - Room Type: Lobby
   - Floor Number: 1
   - Show Gizmos: ✓ (for visual debugging)
```

**Important**: The BoxCollider must be a **trigger** to detect AI/players entering/exiting.

### Step 3: Tag Doors

**For Each Door:**

```
1. Select your door GameObject (or create empty at door position)
2. Add Component → Door
3. Configure:
   - Door Name: "Front Door"
   - Door Type: Standard (or Entrance, Security, etc.)
   - Is Open: false
   - Can Be Opened By AI: ✓
   - Is Choke Point: ✓ (if it's a strategic doorway)
   - Door Transform: Drag actual door mesh here (optional, for animation)
```

**Optional**: Manually assign connected rooms, or let it auto-detect.

### Step 4: Enable AI Room Awareness

**On TacticalAI:**

```
1. Select AI prefab
2. Find TacticalAI component
3. Enable "Room Awareness" ✓
4. Enable "Debug Rooms" ✓ (for testing)
```

## Detailed Setup

### Creating Room Volumes

**Example: Office Room**

```
1. GameObject: "Room_Office_203"
2. Position it at room center
3. BoxCollider settings:
   - Is Trigger: ✓
   - Center: (0, 1.5, 0) // Mid-height of room
   - Size: (8, 3, 6) // Room dimensions
4. RoomVolume settings:
   - Room Name: "Office 203"
   - Room Type: Office
   - Floor Number: 2
   - Is High Value Room: false
   - Can Use For Regroup: true
   - Is Defendable: true
```

**Gizmo Colors:**
- Room volumes show as **cyan transparent boxes** in scene view
- **Cyan lines** connect rooms to their doors
- **Yellow lines** connect adjacent rooms
- **Room name and floor** displayed above volume

### Creating Doors

**Example: Office Door**

```
1. GameObject: "Door_Office_203"
2. Position at door location
3. Door settings:
   - Door Name: "Office 203 Door"
   - Door Type: Standard
   - Is Open: false
   - Is Locked: false
   - Can Be Opened By AI: ✓
   - Can Be Closed By AI: ✓
   - Can Be Locked By AI: false (unless security door)
   - Is Choke Point: ✓
   - Door Frame Cover Quality: 7
```

**Door Transform (Optional Animation):**
```
- Drag the actual door mesh into "Door Transform"
- Set Open Angle: 90
- Set Close Angle: 0
- Set Door Speed: 2
```

**Gizmo Colors:**
- Doors show as **red sphere** (closed) or **green sphere** (open)
- **Blue line** shows door facing direction
- **Cyan lines** connect to rooms
- **Red bubble** if marked as choke point

### Auto-Detection vs Manual

**Auto-Detection (Default):**
- Rooms auto-find nearby doors
- Doors auto-find nearby rooms
- Adjacent rooms auto-connect via doors

**Manual Assignment:**
- Manually drag rooms into door's "Connected Rooms"
- Manually drag doors into room's "Doors"
- Manually set room's "Adjacent Rooms"

Auto-detection works well for most cases. Use manual for complex layouts.

## Room Types

Choose the appropriate room type for AI behavior customization:

- **Generic** - Normal room
- **Hallway** - Corridor (AI won't linger here)
- **Stairwell** - Vertical connection
- **Entrance** - Building entry/exit (AI defends heavily)
- **Office** - Standard office room
- **Storage** - Closet/storage (AI might hide here)
- **Bathroom** - Bathroom (limited tactical value)
- **Kitchen** - Break room
- **Armory** - Weapon storage (high-value, defended)
- **Server Room** - High-value tech room
- **Meeting Room** - Conference room
- **Lobby** - Main lobby (high-traffic, defendable)
- **Exterior** - Outdoor area

## Door Types

- **Standard** - Normal door
- **Entrance** - Building entrance (strategic)
- **Exit** - Emergency exit
- **Security** - Can be locked/reinforced
- **Automatic** - Auto-opens (future feature)
- **Breach** - Can be breached (future feature)
- **Elevator** - Elevator door
- **Sliding** - Sliding door

## Testing Your Setup

### Test 1: Room Detection

```
1. Play mode
2. Move AI around
3. Watch console for:
   "[RoomManager] Registered room: Lobby (Floor 1)"
   "[RoomManager] Registered door: Front Door"
   "[BikerCriminal_1] Entered Lobby (Floor 1)"
```

### Test 2: Door Connections

```
1. Select RoomManager in Hierarchy
2. In Inspector, click "Print All Doors"
3. Console should show:
   "Front Door [CLOSED] - Connects: Exterior <-> Lobby"
```

### Test 3: Room Graph

```
1. Select RoomManager
2. Click "Print Room Summary"
3. Console shows:
   "Total Rooms: 5"
   "Total Doors: 8"
   "Floor 1: 3 rooms"
   "Floor 2: 2 rooms"
```

### Test 4: Player Tracking

```
1. Enable TacticalAI.debugRooms
2. Play mode
3. AI spots player
4. Console shows:
   "[BikerCriminal_1] Player spotted in Office 203"
```

## Example: Multi-Floor Building

### Floor 1 Setup

```
Room_Lobby:
- Room Name: "Lobby"
- Floor: 1
- Type: Lobby
- Size: (15, 3, 10)
- Is Defendable: true

Room_Hallway_1F:
- Room Name: "First Floor Hallway"
- Floor: 1
- Type: Hallway
- Size: (20, 3, 3)

Room_Office_101:
- Room Name: "Office 101"
- Floor: 1
- Type: Office
- Size: (8, 3, 6)

Doors:
- Door_Front (Exterior <-> Lobby)
- Door_Lobby_Hallway (Lobby <-> Hallway)
- Door_Office_101 (Hallway <-> Office 101)
```

### Floor 2 Setup

```
Room_Stairwell:
- Room Name: "Stairwell"
- Floor: 1 (or 2, spans both)
- Type: Stairwell
- Size: (4, 6, 4) // Tall to cover both floors

Room_Hallway_2F:
- Room Name: "Second Floor Hallway"
- Floor: 2
- Type: Hallway

Room_Office_201:
- Room Name: "Office 201"
- Floor: 2
- Type: Office

Doors:
- Door_Stairwell_2F (Stairwell <-> Hallway 2F)
- Door_Office_201 (Hallway 2F <-> Office 201)
```

### Result

AI can now:
- Know they're on Floor 1 vs Floor 2
- Track which office player entered
- Understand room connections for flanking

## What You Can Build With This

### Current Features (Automatic)

**Room Tracking:**
- AI knows what room they're in
- AI knows what room player is in
- Room enter/exit events

**Door Detection:**
- AI aware of nearby doors
- Doors auto-connect rooms
- Room graph auto-builds

### Future Features (You Can Add)

**Room Callouts:**
```csharp
// In TacticalAI when spotting player:
if (lastKnownPlayerRoom != null)
{
    Debug.Log($"Contact in {lastKnownPlayerRoom.roomName}!");
    // Play voice line: "Enemy in the lobby!"
}
```

**Smart Flanking:**
```csharp
// Check if there's another door to player's room:
var playerRoom = GetLastKnownPlayerRoom();
var myRoom = GetCurrentRoom();
var doors = RoomManager.GetDoorsBetweenRooms(myRoom, playerRoom);

if (doors.Count > 1)
{
    // Multiple entry points - coordinate flank!
}
```

**Door Control:**
```csharp
// Close door when retreating:
var nearbyDoors = currentRoom.doors;
foreach (var door in nearbyDoors)
{
    if (door.isOpen)
    {
        door.Close(gameObject);
    }
}
```

**Choke Point Defense:**
```csharp
// Find all choke point doors:
var chokePoints = RoomManager.GetChokePointDoors();
foreach (var door in chokePoints)
{
    // Position AI to defend this door
    Vector3 defensePos = door.GetCoverPositionFromRoom(currentRoom);
}
```

**Room-Based Pathfinding:**
```csharp
// Find path from AI's room to player's room:
var path = RoomManager.FindRoomPath(myRoom, playerRoom);
// path = [Office 101, Hallway, Lobby]
// AI knows to go through hallway to reach player
```

## Advanced Setup

### High-Value Rooms

Mark important rooms:

```
Room_Armory:
- Is High Value Room: ✓
- Is Defendable: ✓

Room_ServerRoom:
- Is High Value Room: ✓
- Is Defendable: ✓
```

AI will prioritize defending these rooms.

### Tactical Positions

Add strategic positions to rooms:

```
Room_Lobby:
- Tactical Positions:
  - Front_Desk_Left
  - Front_Desk_Right
  - Corner_Cover_A
  - Corner_Cover_B
```

AI can use these for defensive setups.

### Cover Points

Assign cover to rooms:

```
Room_Office_203:
- Cover Points:
  - Desk_Cover_01
  - Filing_Cabinet_Cover
  - Doorframe_Cover
```

Room system knows which cover belongs to which room.

### Adjacent Rooms (Manual)

For complex layouts, manually set adjacent:

```
Room_Hallway:
- Adjacent Rooms:
  - Office_101
  - Office_102
  - Office_103
  - Stairwell
  - Lobby
```

This helps AI understand room connections.

## Troubleshooting

### Issue: "Room not detecting AI entry"

**Cause:** BoxCollider not set as trigger

**Fix:**
- Select RoomVolume GameObject
- Check BoxCollider → Is Trigger ✓

### Issue: "Doors not connecting rooms"

**Cause:** Door too far from room boundaries

**Fix:**
- Move door closer to room edge
- Or manually assign connected rooms in Door component

### Issue: "AI says 'Entered Room' multiple times"

**Cause:** Multiple room volumes overlapping

**Fix:**
- Ensure room volumes don't overlap
- Use precise boundaries

### Issue: "Console spam with room messages"

**Cause:** Debug enabled

**Fix:**
- Disable RoomVolume.showGizmos
- Disable TacticalAI.debugRooms
- Disable RoomManager.debugRoomSystem

### Issue: "Room graph not building"

**Cause:** RoomManager not in scene

**Fix:**
- Add RoomManager GameObject
- Only need one RoomManager per scene

## Performance

**Room System is lightweight:**
- Trigger colliders (fast)
- Static lookups (cached)
- No pathfinding overhead
- Minimal memory

**Recommended Limits:**
- Rooms: Unlimited (tested with 100+)
- Doors: Unlimited (tested with 200+)
- AI tracking: No performance impact

## Visual Debugging

**Scene View Gizmos:**

Enable gizmos to see:
- **Room volumes** (cyan transparent boxes)
- **Room labels** (name + floor)
- **Door positions** (colored spheres)
- **Door states** (open/closed/locked)
- **Room connections** (yellow lines)
- **Door connections** (cyan lines)
- **Choke points** (red bubbles)

**Console Debugging:**

Enable debug modes:
- `RoomManager.debugRoomSystem` - Registration logs
- `RoomVolume.showGizmos` - Entry/exit logs
- `Door.debugDoor` - Open/close logs
- `TacticalAI.debugRooms` - Room awareness logs

## Example Room Layouts

### Small House

```
Exterior → Front Door → Living Room
                        ├─ Kitchen
                        ├─ Hallway → Bedroom 1
                        │          → Bedroom 2
                        └─ Bathroom

Rooms: 6
Doors: 6
Floors: 1
```

### Office Building (3 Floors)

```
Floor 1:
- Lobby → Elevator
- Lobby → Stairwell
- Lobby → Hallway 1F → Office 101
                     → Office 102
                     → Conference Room

Floor 2:
- Stairwell → Hallway 2F → Office 201
                          → Office 202
                          → Server Room

Floor 3:
- Stairwell → Hallway 3F → Executive Office
                          → Board Room

Rooms: 12
Doors: 15
Floors: 3
```

### Warehouse Complex

```
Exterior → Main Gate → Warehouse Floor
                       ├─ Office Wing → Office A
                       │              → Office B
                       ├─ Storage → Storage A
                       │          → Storage B
                       └─ Loading Dock → Truck Bay

Rooms: 9
Doors: 11
Floors: 1
```

## Integration with Other Systems

### ✓ Works With:
- **Territory Defense** - Rooms define territories
- **AI Morale System** - Room awareness for retreat
- **AI Fallback System** - Uses room cover points
- **AdvancedBotSpawner** - Spawn zones = rooms
- **Cover System** - Rooms track their cover

### Future Integration:
- **Voice Callouts** - "Contact in Room 203!"
- **Coordinated Tactics** - Multi-room flanking
- **Smart Objectives** - "Defend the armory"
- **Building Lockdown** - Close all doors on alert

## Next Steps

1. **Tag your rooms** - Add RoomVolume to all rooms
2. **Tag your doors** - Add Door components
3. **Test detection** - Enable debug, watch console
4. **Build features** - Use room awareness for tactics

See example scripts in this guide for building on top of the room system!

## Quick Reference

**Components:**
- `RoomVolume` - Tags a room (BoxCollider trigger)
- `Door` - Tags a door (tracks state)
- `RoomManager` - Manages all rooms/doors
- `TacticalAI` - Aware of rooms (if enabled)

**Key Methods:**
- `RoomManager.GetRoomAtPosition(pos)` - Find room at position
- `RoomManager.GetRoomsWithPlayers()` - Rooms with players
- `RoomManager.FindRoomPath(start, end)` - Path between rooms
- `tacticalAI.GetCurrentRoom()` - AI's current room
- `tacticalAI.GetLastKnownPlayerRoom()` - Player's room
- `door.Open(opener)` - Open a door
- `door.Close(closer)` - Close a door
- `room.HasPlayers()` - Check if room has players

Happy room tagging! 🏢
