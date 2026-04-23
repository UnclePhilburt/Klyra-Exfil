# AI Enemy Setup Guide

Complete guide for setting up tactical AI enemies in your Ready or Not style game.

---

## What I Created

**TacticalAI.cs** - Comprehensive AI system with:
- ✅ **Patrol** - Waypoint-based patrolling
- ✅ **Detection** - Sight and hearing-based player detection
- ✅ **Investigation** - Investigates last known player position
- ✅ **Combat** - Engages players with weapons
- ✅ **Voice Line Response** - 30% chance to comply, 70% to engage
- ✅ **Flashbang Reaction** - Gets stunned and disoriented
- ✅ **Alert System** - Gradually becomes alert/calm
- ✅ **NavMesh Integration** - Uses Unity NavMesh for pathfinding

---

## Prerequisites

Before setting up AI enemies:

1. **Bake a NavMesh** in your scene:
   - **Window** → **AI** → **Navigation**
   - Select your floor/ground objects
   - Mark as "Navigation Static"
   - Click **Bake** tab → **Bake**

2. **Create an AI character using Opsive:**
   - Follow Opsive's AI Agent creation process
   - Make sure "AI Agent" is checked (not player controlled)
   - Add NavMeshAgent Movement ability
   - Give it a weapon

---

## Step-by-Step Setup

### Step 1: Create the AI Enemy GameObject

1. **Use Opsive's Character Manager** to create an AI character
   - **Tools** → **Opsive** → **Ultimate Character Controller** → **Character Manager**
   - Select **AI Agent** (NOT player)
   - Enable **NavMeshAgent**
   - Choose your character model
   - Add weapons/items

2. **Or duplicate an existing AI prefab** from Opsive demos

### Step 2: Add TacticalAI Script

1. **Select your AI character** in the scene

2. **Add Component** → Search for **`TacticalAI`**

3. The script will automatically:
   - Find NavMeshAgent
   - Find UltimateCharacterLocomotion
   - Find Use ability (for shooting)
   - Create an eye position for line-of-sight

### Step 3: Configure Basic Settings

In the **TacticalAI** component:

**AI State:**
- Leave as "Patrol" (default starting state)

**Patrol Settings:**
- **Patrol Waypoints** Size: `3` (or more)
  - Create empty GameObjects around your level
  - Drag them into the waypoints array
- **Waypoint Wait Time**: `3` seconds
- **Patrol Speed**: `1.5` m/s

**Detection Settings:**
- **Sight Range**: `20` meters
- **Field Of View**: `90` degrees
- **Hearing Range**: `15` meters
- **Obstacle Mask**: Select layers that block vision (Default, Wall, etc.)
- **Alert Increase Rate**: `2` (how fast they get alert)
- **Alert Decrease Rate**: `0.5` (how fast they calm down)

**Combat Settings:**
- **Combat Speed**: `3` m/s
- **Combat Distance**: `10` meters (preferred fighting range)
- **Fire Rate**: `0.5` seconds (time between shots)
- **Accuracy**: `0.7` (70% accuracy)

**Voice Line Response:**
- **Compliance Chance**: `0.3` (30% chance to surrender)
- **Compliance Duration**: `5` seconds
- **Voice Command Range**: `10` meters

**Flashbang Response:**
- **Flashbang Stun Duration**: `5` seconds

### Step 4: Set Up Patrol Waypoints

1. **Create empty GameObjects** for waypoints:
   - Right-click in Hierarchy → Create Empty
   - Name them: "Waypoint_1", "Waypoint_2", etc.
   - Position them where you want the AI to patrol

2. **Assign to AI**:
   - Select your AI enemy
   - In TacticalAI component, expand **Patrol Waypoints**
   - Set Size to number of waypoints
   - Drag each waypoint into the array

3. **Waypoint Tips**:
   - Place on NavMesh (blue areas)
   - Space them 5-10 meters apart
   - Form a patrol route (AI will cycle through them)

### Step 5: Configure Eye Position (Optional)

The script auto-creates this, but you can customize:

1. **Find "EyePosition"** child object on your AI
2. **Adjust height** if needed (default 1.7m)
3. This is where line-of-sight checks originate from

### Step 6: Tag Your Player

Make sure your player is tagged properly:

1. **Select your player** GameObject
2. **Tag** dropdown (top of Inspector)
3. Select **"Player"**
4. AI will detect objects with this tag

### Step 7: Add PhotonView for Multiplayer

1. **Select your AI enemy**
2. **Add Component** → **Photon View**
3. Configure:
   - **Observed Components**: Add the TacticalAI component
   - **Ownership**: Leave as default

---

## Testing the AI

### Test Patrol:
1. **Press Play**
2. AI should walk between waypoints
3. Wait at each waypoint for 3 seconds
4. Cycle through all waypoints

### Test Detection:
1. **Walk near the AI** (within 20 meters)
2. **Stay in their FOV** (90 degree cone in front)
3. AI should detect you and **transition to Combat state**
4. Console shows: "Player detected! Engaging!"

### Test Combat:
1. **Let AI detect you**
2. AI should:
   - Move toward you
   - Maintain 10 meter distance
   - Face you
   - Shoot (if weapon equipped)

### Test Voice Commands:
1. **Walk near AI** (within 10 meters)
2. **Press V** (or your voice key)
3. **Select a command** like "GET DOWN"
4. **30% chance**: AI goes to Compliant state (hands up, stops)
5. **70% chance**: AI investigates your position

### Test Flashbang:
1. **Throw flashbang near AI**
2. AI should:
   - Enter Flashbanged state
   - Stop moving/shooting
   - Stay stunned for 5 seconds
   - Return to Patrol after recovery

---

## Visualizing AI Behavior

When you **select the AI** in the Scene view, you'll see:

- **Yellow sphere**: Sight range (20m)
- **Blue sphere**: Hearing range (15m)
- **Cyan lines**: Field of view cone
- **Red line**: Line to current target
- **Magenta sphere**: Last known player position

These help you debug AI behavior!

---

## AI States Explained

### Patrol (Default)
- Walks between waypoints
- Looks around casually
- Low alert level

### Investigate
- Moves to last known player position
- Alert and cautious
- Returns to Patrol if nothing found

### Combat
- Actively engaging target
- Maintains combat distance
- Shoots at player
- High alert level

### Compliant
- Surrendered to voice command
- Hands up (would show animation)
- Stays still for 5 seconds
- Then returns to previous state

### Flashbanged
- Stunned and blind
- Can't move or shoot
- Lasts 5 seconds
- Returns to Patrol after recovery

---

## Common Issues & Solutions

### AI doesn't move:
- **Check NavMesh** - Is it baked?
- **Check waypoints** - Are they on the NavMesh?
- **Check NavMeshAgent** - Is it enabled?

### AI doesn't detect player:
- **Check player tag** - Is it "Player"?
- **Check sight range** - Is player within 20m?
- **Check FOV** - Is player in the 90° cone?
- **Check obstacles** - Is line of sight blocked?

### AI doesn't shoot:
- **Check weapon** - Is one equipped?
- **Check Use ability** - Is it on the character?
- **Check Opsive setup** - Can the AI use items?

### AI shoots but misses every time:
- **Increase accuracy** - Try 0.9 or 1.0
- **Check weapon aim** - Might be Opsive weapon setup issue

### Voice commands don't work:
- **Check distance** - Within 10 meters?
- **Check VoiceLineSystem** - Is it on player?
- **Check SubtitleManager** - Is it in scene?

### Flashbang doesn't stun:
- **Check collider** - Does AI have a collider?
- **Check effect radius** - Is AI within range?
- **Check line of sight** - Walls might block it

---

## Advanced Tips

### Adjusting Difficulty:

**Easy AI:**
- Accuracy: `0.3`
- Fire Rate: `1.0`
- Compliance Chance: `0.7`
- Sight Range: `15`

**Hard AI:**
- Accuracy: `0.95`
- Fire Rate: `0.2`
- Compliance Chance: `0.1`
- Sight Range: `30`

### Multiple AI Enemies:

1. **Create one AI** and test it
2. **Save as prefab**
3. **Duplicate** in scene
4. Give each their own waypoints
5. They'll independently detect and engage

### Making AI Never Comply:

- Set **Compliance Chance** to `0`
- AI will always investigate voice commands instead

### Making AI Always Comply:

- Set **Compliance Chance** to `1`
- Every voice command will make them surrender

---

## Performance Optimization

For many AI enemies:

1. **Reduce update frequency**:
   - Only check for threats every 0.5 seconds
   - Use coroutines instead of Update

2. **Use object pooling**:
   - Reuse AI GameObjects instead of spawning/destroying

3. **Disable far AI**:
   - If player is >50m away, disable AI updates

4. **Simplify pathfinding**:
   - Reduce NavMesh precision
   - Use simpler patrol routes

---

## Future Enhancements

Ideas for expanding the AI:

- **Cover system** - AI hides behind objects
- **Grenades** - AI throws flashbangs back
- **Callouts** - AI communicates with each other
- **Morale system** - AI surrenders if buddies die
- **Hostages** - Protect hostages or take them
- **Reinforcements** - AI calls for backup

---

## Example Scene Setup

```
Police Station Level
├── NavMesh (baked)
├── Player (with VoiceLineSystem)
├── SubtitleManager
├── AI_Enemy_1
│   ├── TacticalAI (configured)
│   ├── NavMeshAgent
│   ├── UltimateCharacterLocomotion
│   └── Weapon
├── AI_Enemy_2
├── Waypoint_1
├── Waypoint_2
├── Waypoint_3
└── Doors (with snake cam, etc.)
```

That's it! Your tactical AI enemies are ready to go!
