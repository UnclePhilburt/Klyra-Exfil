# Advanced Bot Spawner - Complete Setup Guide

## Overview

The **AdvancedBotSpawner** gives you full control over:
- **Where** bots spawn (multiple zones across the map)
- **How** they move (idle/patrol/roam percentages)
- **Group behavior** (spawn in squads vs individually)
- **Zone assignments** (different areas with different waypoints)
- **Territory defense** (bots defend their spawn zones instead of chasing across map)

## When to Use Which Spawner?

### Use **BotSpawner.cs** when:
- Simple spawning (just pick random spawn points)
- Don't care about movement patterns
- Quick setup for testing

### Use **AdvancedBotSpawner.cs** when:
- Multi-floor buildings with different patrol routes per floor
- Want guards to patrol specific areas
- Need some bots idle, some patrolling, some roaming
- Want realistic squad-based spawning
- Complex level design with zones

## Setup Guide

### Step 1: Create Spawn Zones

Each spawn zone represents an area of your map (e.g., "1st Floor", "Warehouse", "Parking Lot").

**Create Zone Objects:**
```
1. Create empty GameObject: "SpawnZone_1stFloor"
2. Inside it, create:
   - Empty GameObject: "SpawnPoints" (folder for spawn locations)
   - Empty GameObject: "PatrolWaypoints" (folder for patrol path)
   - Empty GameObject: "RoamCenter" (center point for roaming)
```

**Add Spawn Points:**
```
1. Under "SpawnPoints", create multiple empty GameObjects
2. Name them: "Spawn_01", "Spawn_02", etc.
3. Position them where bots should appear
4. Rotate them so forward (blue arrow) faces the direction bots should face
```

**Add Patrol Waypoints (if using patrol):**
```
1. Under "PatrolWaypoints", create empty GameObjects
2. Name them: "Waypoint_01", "Waypoint_02", etc.
3. Position them in order along patrol route
4. Bots will patrol: Waypoint_01 → Waypoint_02 → Waypoint_03 → loop
```

**Set Roam Center (if using roam):**
```
1. Position "RoamCenter" in middle of area
2. This is where roaming bots will wander around
```

### Step 2: Configure AdvancedBotSpawner Component

**Add Component:**
```
1. Create empty GameObject: "AdvancedBotSpawner"
2. Add Component → AdvancedBotSpawner
```

**Bot Prefab:**
- Set `Bot Prefab Name` to your bot (e.g., "BikerCriminal")

**Spawn Count:**
- `Min Bots`: Minimum to spawn (e.g., 3)
- `Max Bots`: Maximum to spawn (e.g., 10)

**Movement Patterns:**
- `Idle Bot Percentage`: 0.3 = 30% will stand guard
- `Patrol Bot Percentage`: 0.4 = 40% will patrol waypoints
- `Roam Percentage`: Auto-calculated (remaining 30% will roam)

**Group Behavior:**
- `Spawn In Groups`: ✓ Enable for squad-based spawning
- `Min Group Size`: 2 (smallest squad)
- `Max Group Size`: 4 (largest squad)

**AI Systems:**
- `Auto Add AI Systems`: ✓ Enable (adds morale/fallback/surrender)
- `Randomize Personalities`: ✓ Enable (varied AI behavior)

### Step 3: Setup Spawn Zones Array

**In AdvancedBotSpawner Inspector:**

1. Set `Spawn Zones` size to number of zones (e.g., 3)

2. **Zone 0 - 1st Floor:**
   - `Zone Name`: "Building 1st Floor"
   - `Spawn Points`: Drag all spawn point Transforms from 1st floor
   - `Patrol Waypoints`: Drag patrol waypoints for 1st floor
   - `Roam Center`: Drag roam center Transform
   - `Roam Radius`: 15 (meters)
   - `Bot Count`: 0 (auto-distribute) or fixed number like 4
   - `Priority`: 5 (higher = more likely)

3. **Zone 1 - 2nd Floor:**
   - `Zone Name`: "Building 2nd Floor"
   - `Spawn Points`: Drag 2nd floor spawn points
   - `Patrol Waypoints`: Drag 2nd floor patrol waypoints
   - `Roam Center`: Drag 2nd floor roam center
   - `Roam Radius`: 12
   - `Bot Count`: 0
   - `Priority`: 5

4. **Zone 2 - Exterior:**
   - `Zone Name`: "Parking Lot"
   - `Spawn Points`: Drag exterior spawn points
   - `Patrol Waypoints`: Drag exterior waypoints
   - `Roam Center`: Drag exterior roam center
   - `Roam Radius`: 25 (larger outdoor area)
   - `Bot Count`: 0
   - `Priority`: 3 (less likely than building)

### Step 4: Visual Setup with Gizmos

**In Scene View:**

- **Green spheres** = Spawn points (with forward line showing direction)
- **Yellow spheres** = Patrol waypoints (with lines connecting them)
- **Cyan wireframe sphere** = Roam radius

Use these to verify your setup looks correct!

## Examples

### Example 1: Office Building (3 Floors)

**Setup:**
```
Spawn Zones: 3
- Zone 0: "Floor 1" - 4 spawn points, 6 patrol waypoints, roam radius 10m
- Zone 1: "Floor 2" - 3 spawn points, 5 patrol waypoints, roam radius 10m
- Zone 2: "Floor 3" - 2 spawn points, 4 patrol waypoints, roam radius 8m

Movement:
- Idle: 20% (2 guards standing watch)
- Patrol: 60% (6 guards patrolling floors)
- Roam: 20% (2 guards wandering)

Total Bots: 8-12 random
```

**Result:**
- Each floor gets guards
- Most are patrolling their floor's waypoints
- Some stand at key positions
- Some roam around randomly

### Example 2: Warehouse Raid

**Setup:**
```
Spawn Zones: 4
- Zone 0: "Main Warehouse" - 8 spawns, 10 waypoints, roam 20m, priority 8
- Zone 1: "Office Area" - 3 spawns, 5 waypoints, roam 10m, priority 5
- Zone 2: "Loading Bay" - 4 spawns, 6 waypoints, roam 15m, priority 6
- Zone 3: "Roof Access" - 2 spawns, 3 waypoints, roam 8m, priority 3

Movement:
- Idle: 30% (guards at doors/key positions)
- Patrol: 40% (regular patrols)
- Roam: 30% (unpredictable movement)

Total Bots: 10-15 random
Spawn In Groups: YES (squads of 2-4)
```

**Result:**
- Main warehouse gets most bots (priority 8)
- Squads spawn together
- Mixed behaviors create realistic guard patterns
- Roof access gets fewer guards (priority 3)

### Example 3: Stealth Mission (Few Guards, Strategic Placement)

**Setup:**
```
Spawn Zones: 2
- Zone 0: "Perimeter" - 6 spawns, 8 waypoints, roam 30m, priority 5
- Zone 1: "Interior" - 4 spawns, 6 waypoints, roam 12m, priority 5

Movement:
- Idle: 50% (half are stationary guards)
- Patrol: 40% (patrol predictable routes)
- Roam: 10% (minimal random movement)

Total Bots: 4-6 random
Spawn In Groups: NO (individual guards)
Personalities: Randomize OFF, manually set to "Coward" (easy to scare)
```

**Result:**
- Low bot count
- Predictable patrols (stealth-friendly)
- Easy to avoid or scare off

## Movement Mode Details

### Idle Mode (Stationary Guards)
**What it does:**
- Bot spawns and stays in place
- Still detects and shoots enemies
- Perfect for door guards, sentries

**Best for:**
- Key positions (doors, stairs, objectives)
- Sniper positions
- Guard posts

### Patrol Mode (Waypoint Patrol)
**What it does:**
- Bot walks between waypoints in order
- Waypoint_01 → Waypoint_02 → Waypoint_03 → loop
- Continues patrol until enemy spotted

**Best for:**
- Hallways
- Perimeter routes
- Floor patrols

**Important:**
- Zone must have patrol waypoints set!
- If no waypoints exist, bot falls back to Roam mode

### Roam Mode (Random Wandering)
**What it does:**
- Bot picks random points within roam radius
- Wanders around roam center
- Unpredictable movement

**Best for:**
- Large open areas
- Creating uncertainty
- Realistic "on break" behavior

## Bot Distribution

**How bots are assigned to zones:**

1. **Fixed Counts First:**
   - If zone has `Bot Count = 5`, it gets 5 bots guaranteed

2. **Priority Distribution:**
   - Remaining bots distributed by priority
   - Zone with priority 10 is 2x more likely than priority 5

**Example:**
```
Total Bots: 10
Zone A: Bot Count = 3, Priority = 5
Zone B: Bot Count = 0, Priority = 8
Zone C: Bot Count = 0, Priority = 2

Result:
- Zone A: 3 bots (fixed)
- Remaining 7 bots split by priority:
  - Zone B: ~5 bots (priority 8)
  - Zone C: ~2 bots (priority 2)
```

## Group Spawning

**When enabled:**
- Bots spawn in squads of 2-4
- Creates realistic deployment
- Groups tend to stick together initially

**When disabled:**
- Bots spawn one by one
- More spread out
- Independent behavior

## Personality System Integration

**Automatic Personalities:**

With `Randomize Personalities` enabled, spawned bots get:
- 20% Coward (surrender easy)
- 30% Normal (balanced)
- 30% Brave (tough)
- 20% Fearless (fight to death)

See `BOT_SPAWNER_GUIDE.md` for full personality details.

## Debug Console Output

**What you'll see:**

```
[AdvancedBotSpawner] Spawning 12 bots across 3 zones
[AdvancedBotSpawner] Building 1st Floor: 5 bots
[AdvancedBotSpawner] Building 2nd Floor: 4 bots
[AdvancedBotSpawner] Parking Lot: 3 bots

[AdvancedBotSpawner] Spawning group of 3 bots
[AdvancedBotSpawner] Spawned bot 1/12 at Building 1st Floor
[AdvancedBotSpawner] Bot_1 = IDLE (stationary guard)
[AdvancedBotSpawner] Spawned bot 2/12 at Building 1st Floor
[AdvancedBotSpawner] Bot_2 = PATROL (6 waypoints in Building 1st Floor)
[AdvancedBotSpawner] Spawned bot 3/12 at Building 1st Floor
[AdvancedBotSpawner] Bot_3 = ROAM (radius 15m in Building 1st Floor)

... etc
```

**Disable debug:**
- Uncheck `Debug Spawning` to reduce console spam

## Troubleshooting

### Issue: Bots all spawn in same zone

**Cause:** Only one zone has spawn points, or priorities heavily favor one zone

**Fix:**
- Check all zones have spawn points assigned
- Balance priority values (5-5-5 for equal distribution)

### Issue: Patrol bots stand still

**Cause:** Zone has no patrol waypoints assigned

**Console says:** "Bot_X wanted PATROL but no waypoints in ZoneName, using ROAM instead"

**Fix:**
- Add patrol waypoints to that zone
- Or reduce patrol percentage

### Issue: Roam bots don't move

**Cause:** Roam radius is 0 or roam center not set

**Fix:**
- Set roam radius to 10-30 meters
- Assign roam center Transform

### Issue: Bots spawn but fall through floor

**Cause:** NavMesh not baked

**Fix:**
- Window → AI → Navigation
- Select floors, mark "Navigation Static"
- Bake NavMesh

### Issue: Group spawning doesn't work

**Cause:** Not enough bots to form groups

**Fix:**
- Increase max bots
- Or disable group spawning

## Performance Tips

**For large bot counts (20+):**
- Pre-add AI systems to prefab instead of runtime
- Disable `Auto Add AI Systems`
- Manually add morale/fallback/surrender to bot prefab

**For complex scenes:**
- Use multiple smaller AdvancedBotSpawners
- Each spawner handles one building/area
- Keeps organization clean

## Migration from BotSpawner

**If you have existing BotSpawner:**

1. Keep it for simple scenarios
2. Add AdvancedBotSpawner for complex levels
3. Can use both in same project!

**Convert to Advanced:**
```
Old BotSpawner:
- spawnPoints = [Spawn_01, Spawn_02, Spawn_03]

New AdvancedBotSpawner:
- Create 1 spawn zone
- Assign same spawn points
- Add movement patterns
- Done!
```

## Advanced Tips

### Tip 1: Mixed Zone Strategies

**Example: Building with exterior guards**

```
Zone "Exterior": High patrol % (guards walk perimeter)
Zone "Lobby": High idle % (guards at doors)
Zone "Offices": High roam % (guards wandering around)
```

Each zone can have different behavior!

### Tip 2: Priority for Dynamic Difficulty

**Easy Mode:**
```
Zone "Safe Area": Priority 10, Bot Count 2-3
Zone "Objective": Priority 1, Bot Count 0-1
```

**Hard Mode:**
```
Zone "Safe Area": Priority 1, Bot Count 0
Zone "Objective": Priority 10, Bot Count 8-10
```

Adjust priorities to control where enemies concentrate!

### Tip 3: Time-Based Spawning

**Want guards to change shifts?**

```csharp
// In your game manager
void Start()
{
    // Morning shift - perimeter guards
    advancedSpawner.spawnZones[0].priority = 10; // Exterior
    advancedSpawner.spawnZones[1].priority = 2;  // Interior
    advancedSpawner.SpawnAllBots();

    // Later, despawn and respawn with different priorities
    Invoke(nameof(EveningShift), 300f); // After 5 minutes
}

void EveningShift()
{
    // Evening shift - interior guards
    advancedSpawner.spawnZones[0].priority = 2;  // Exterior
    advancedSpawner.spawnZones[1].priority = 10; // Interior
    // Despawn old bots, spawn new ones
}
```

## Quick Reference

**Idle Bots:**
- Stationary guards
- Best at doors, key positions

**Patrol Bots:**
- Follow waypoint routes
- Predictable but thorough

**Roam Bots:**
- Random wandering
- Unpredictable

**Spawn In Groups:**
- Realistic squad deployment
- Bots spawn together

**Bot Count:**
- 0 = auto-distribute by priority
- Fixed number = guaranteed count for that zone

**Priority:**
- 1-10 scale
- Higher = more bots assigned

**Roam Radius:**
- Small (5-10m): Confined areas
- Medium (10-20m): Rooms, floors
- Large (20-40m): Outdoor areas

## Testing Checklist

- [ ] Spawn zones have spawn points assigned
- [ ] Patrol zones have waypoints assigned
- [ ] Roam zones have center and radius set
- [ ] NavMesh is baked
- [ ] Bot prefab name is correct
- [ ] Movement percentages add up reasonably (< 1.0)
- [ ] Debug spawning enabled (first test)
- [ ] Console shows bot assignments
- [ ] Bots actually move to assigned behavior

## Territory Defense Integration

**Automatically Enabled!**

AdvancedBotSpawner **automatically configures territory defense** for each bot:

```
Territory Radius = Zone Roam Radius × 1.5
Max Chase Distance = Zone Roam Radius × 2.0
```

**What This Means:**

Bots spawned in a zone will:
- Defend that zone instead of chasing players across the map
- Use cover within their territory
- Fall back to their spawn zone when overwhelmed
- Return to zone if pulled too far away

**Example:**

```
Zone "Building 1st Floor"
Roam Radius: 20m

Auto-configured:
Territory Radius: 30m (defends entire floor)
Max Chase: 40m (can pursue into stairwell briefly)

Result:
- Guards defend 1st floor
- Won't chase player to 2nd floor
- Fall back to 1st floor defensive positions
- Each floor has independent defense
```

**Benefits:**

- **Realistic Defense:** Guards stay at their posts
- **Tactical Gameplay:** Players can retreat and regroup
- **Zone-Based Combat:** Fights stay localized to areas
- **No Cross-Contamination:** Floor 1 guards don't rush to Floor 3

See `TERRITORY_DEFENSE_GUIDE.md` for complete details on how territory defense works.

## Next Steps

Once you have basic spawning working:

1. Fine-tune movement percentages for your level
2. Adjust zone priorities for desired distribution
3. Add more zones for complex areas
4. Test with different bot counts
5. Combine with morale/fallback systems for realistic AI
6. Tweak roam radius to control territory size

See `BOT_SPAWNER_GUIDE.md` for personality system details.
See `AI_TROUBLESHOOTING_GUIDE.md` if bots don't behave correctly.
See `TERRITORY_DEFENSE_GUIDE.md` for territory defense system.

Happy advanced spawning!
