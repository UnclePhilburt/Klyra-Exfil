# Territory Defense System - Complete Guide

## What Is Territory Defense?

The **Territory Defense System** makes AI defend their assigned areas instead of aggressively chasing players across the entire map. This creates more realistic, tactical gameplay where:

- **Guards stay in their zones** (buildings, floors, rooms)
- **AI won't chase you forever** - they defend their territory
- **Players can tactically retreat** and regroup
- **Firefights stay localized** instead of turning into map-wide chases
- **AI use defensive tactics** - falling back to their spawn area when overwhelmed

## How It Works

### Core Concept

Each AI has a **spawn position** (where they start) and a **territory radius** (how far they'll defend from that spawn).

**Example:**
```
Guard spawns at Building 1st Floor entrance
Territory Radius: 25 meters

Player enters building → Guard engages
Player backs up 20m → Guard pursues (still in territory)
Player backs up 40m → Guard stops pursuing, returns to defend entrance
```

### Territory States

**1. Inside Territory (< Territory Radius)**
- AI behaves normally
- Will engage enemies
- Uses cover within territory
- Fights aggressively

**2. Outside Territory (> Territory Radius, < Max Chase Distance)**
- AI becomes defensive
- Prefers to back towards spawn
- Still shoots but won't push forward
- Tries to pull enemies into territory

**3. Too Far From Territory (> Max Chase Distance)**
- AI retreats back to territory
- Provides defensive fire while backing up
- Prioritizes returning to defensive position
- Breaks contact if they lose sight of player

## Settings

### TacticalAI Settings

**Defend Territory**
- **Enable**: AI defends territory instead of chasing
- **Disable**: AI will chase players across entire map (old behavior)
- **Default**: Enabled (automatically set by spawners)

**Territory Radius**
- How far from spawn point AI will actively defend
- **Small (10-15m)**: Tight defense, room-level
- **Medium (20-30m)**: Building floor defense
- **Large (40-60m)**: Entire building or compound
- **Default**: 25m (good for most scenarios)

**Max Chase Distance**
- How far AI will go before forcefully returning
- Should be larger than territory radius (gives chase buffer)
- **Default**: 35m (10m chase buffer beyond territory)

**Return When Outside Territory**
- When true, AI will return to spawn if pulled too far
- When false, AI stays at edge of territory and shoots
- **Default**: True (more defensive)

### Automatic Configuration

**AdvancedBotSpawner** automatically sets:
```csharp
Territory Radius = Zone Roam Radius × 1.5
Max Chase Distance = Zone Roam Radius × 2.0
```

**Example Zone:**
```
Zone "Building 1st Floor"
Roam Radius: 20m

Auto-calculated:
Territory Radius: 30m (defends entire floor)
Max Chase: 40m (can chase into stairwell a bit)
```

**BotSpawner** uses defaults:
```
Territory Radius: 25m
Max Chase Distance: 35m
```

## Tactical Behaviors

### Cover Selection

AI **prioritizes cover within their territory**:

**Inside Territory:**
- Cover gets +50 score bonus
- AI strongly prefers staying in territory

**Outside Territory:**
- Cover gets -30 score penalty
- AI avoids moving further from spawn

**Result:** AI picks defensive cover positions within their zone.

### Combat Movement

**Normal Situation (Inside Territory):**
```
Player 15m away → AI maintains 10m combat distance
Player 25m away → AI moves closer (still in territory)
Player 5m away → AI backs up but stays in territory
```

**Outside Territory:**
```
Player nearby → AI backs towards spawn while shooting
AI reaches territory edge → Holds position, defensive fire
Player advances → AI falls back to defensive cover
```

**Too Far From Territory:**
```
AI automatically returns to spawn area
Shoots at player while retreating (suppressive fire)
Slower fire rate while backing up (defensive behavior)
If player pursuit stops → AI resumes patrol in territory
```

### Fallback System Integration

When AI morale drops and they fall back:

**Territory Defense ON:**
- Rally points **strongly prioritized** within territory
- +100 score bonus for rally points inside territory
- +50 additional bonus for rally points near spawn
- Falls back towards spawn position (defensive retreat)

**Without Territory Defense:**
- Rally points chosen based on ally positions only
- May retreat away from spawn point

**Example Flow:**
```
1. AI takes damage, morale drops to 45%
2. AIFallbackSystem triggers
3. Searches for rally point near allies
4. Finds two options:
   - Cover near allies, 40m from spawn (outside territory)
   - Cover near allies, 20m from spawn (inside territory)
5. Picks the one inside territory (+100 score)
6. Falls back to defensive position
7. Regroups with allies at spawn area
8. Counter-attacks from fortified position
```

## Examples

### Example 1: Office Building Defense

**Setup:**
```
Zone: "Office 1st Floor"
Spawn Points: Near elevator and front desk
Patrol Waypoints: Through hallways
Roam Radius: 15m

Auto-calculated:
Territory Radius: 22.5m (entire floor)
Max Chase: 30m
```

**Player Approach:**
1. Player enters front door
2. AI at desk engages (player in territory)
3. Player backs into lobby (still in territory)
4. AI pursues, using desk cover
5. Player exits building, backs to parking lot (40m from spawn)
6. AI stops at doorway, shoots defensively
7. AI won't chase into parking lot - holds building

**Result:** Guards defend the building, don't chase into open

### Example 2: Multi-Floor Building

**Setup:**
```
Zone 1: "Floor 1" - Territory 25m
Zone 2: "Floor 2" - Territory 25m
Zone 3: "Floor 3" - Territory 25m
```

**Player Movement:**
1. Player clears Floor 1
2. Moves to Floor 2
3. Floor 2 guards engage
4. Floor 1 guards **don't follow** upstairs (outside their territory)
5. Each floor defends independently

**Result:** Realistic floor-by-floor clearing

### Example 3: Warehouse Compound

**Setup:**
```
Zone A: "Main Warehouse" - Territory 40m (large open space)
Zone B: "Loading Dock" - Territory 20m
Zone C: "Offices" - Territory 15m (tight interior)
```

**Behaviors:**
- Warehouse guards patrol big area, defend entire warehouse
- Dock guards stay near loading bay
- Office guards stay in office wing
- No cross-contamination between zones

### Example 4: Defensive Fallback

**Scenario:**
```
3 Guards defending warehouse entrance
Player attacks from 30m away
```

**Flow:**
1. Guards engage from entrance (inside territory)
2. Player wounds 2 guards
3. Morale drops → Fallback triggered
4. Guards search for rally points
5. Find cover 10m behind entrance (still in territory)
6. All 3 fall back to defensive position near spawn
7. Regroup (+20 morale)
8. Counter-attack from fortified position

**Without Territory Defense:**
- Guards might rally at cover 50m away
- Split from their defensive position
- Lose territorial advantage

## Comparison: Territory ON vs OFF

### Territory Defense: **ON** (Defensive AI)

**Behavior:**
- Stays in assigned zone
- Uses cover within territory
- Falls back to spawn when overwhelmed
- Won't chase across map
- Defends key positions

**Best For:**
- Tactical games (Ready or Not, SWAT 4 style)
- Building clearing
- Zone defense scenarios
- Realistic guard behavior

**Feels Like:**
- Guards defending their post
- Organized defense
- Predictable patrol patterns
- Strategic clearing

### Territory Defense: **OFF** (Aggressive AI)

**Behavior:**
- Chases players everywhere
- Uses any cover available
- Can pursue across entire map
- Hunts players down
- Mobile combat

**Best For:**
- Action games
- Arena shooters
- Hunt-or-be-hunted gameplay
- High-intensity combat

**Feels Like:**
- Enemies hunting you
- Nowhere to hide
- Constant pressure
- Fast-paced combat

## Debug Information

**Enable Debug Detection** on TacticalAI to see:

```
[BikerCriminal_1]: Too far from territory (42.3m), returning to defend spawn area
[BikerCriminal_2]: Outside territory, taking defensive position closer to spawn
[BikerCriminal_3]: Moving to cover point Cover_Floor1_A (INSIDE territory)
[BikerCriminal_4]: Target outside territory, holding defensive position
```

**Enable Debug Fallback** on AIFallbackSystem to see:

```
[BikerCriminal_1] Found rally point at Cover_Entrance, score: 287.5 (inside territory)
[BikerCriminal_2] Rally point Cover_Parking rejected - outside territory
```

## Common Scenarios

### Scenario 1: "Guards won't leave their posts"

**Issue:** Player at edge of territory, guards won't chase

**This is working correctly!** Guards are defending their assigned area.

**Solutions:**
- Increase `Territory Radius` to extend defense zone
- Increase `Max Chase Distance` to allow more pursuit
- Or disable `Defend Territory` for more aggressive AI

### Scenario 2: "AI retreats too easily"

**Cause:** Territory radius too small, AI backs up frequently

**Fix:**
- Increase `Territory Radius` (e.g., 25m → 40m)
- This gives AI more space to maneuver

### Scenario 3: "Multi-floor building, guards from other floors respond"

**Issue:** Want floor-by-floor defense, but all guards rush to one floor

**Fix:**
- Use **AdvancedBotSpawner** with separate zones per floor
- Each zone has its own territory
- Guards defend their assigned floor only

### Scenario 4: "AI won't defend, just patrols even when player nearby"

**Issue:** Player outside territory, AI doesn't see them as threat to territory

**Fix:**
- Ensure player is within `Sight Range` (default 20m)
- Check `Territory Radius` is large enough
- Verify AI can see player (line of sight)

## Performance Impact

**Minimal** - Territory checks are simple distance calculations:
```csharp
float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
bool outsideTerritory = distanceFromSpawn > territoryRadius;
```

No pathfinding, no expensive raycasts, just math.

## Integration with Other Systems

### ✓ Works With:
- **AIMoraleSystem** - Morale loss still applies
- **AIFallbackSystem** - Rally points prioritize territory
- **AISurrenderAnimation** - Surrender still happens at low morale
- **AdvancedAICombatTactics** - Tactics apply within territory
- **AdvancedBotSpawner** - Auto-configures territory per zone
- **BotSpawner** - Uses default territory settings

### ⚠ Note:
- Cover system respects territory (prioritizes cover in zone)
- Patrol/Roam respects territory (stays in assigned area)
- Flashbangs/voice commands work regardless of territory

## Quick Setup

### For New Levels:

**1. Simple Setup (BotSpawner):**
```
- Territory automatically enabled
- 25m default radius
- Works out of the box
```

**2. Advanced Setup (AdvancedBotSpawner):**
```
- Create spawn zones
- Set roam radius per zone
- Territory auto-calculated from roam radius
- Each zone defends independently
```

### Manual Override:

```csharp
// In Unity Inspector or script:
TacticalAI ai = GetComponent<TacticalAI>();
ai.defendTerritory = true;
ai.territoryRadius = 30f; // 30 meter territory
ai.maxChaseDistance = 45f; // Chase up to 45m before returning
```

## Best Practices

### Territory Sizing

**Tight Interior (10-15m):**
- Single rooms
- Small offices
- Corridors

**Medium Interior (20-30m):**
- Building floors
- Large rooms
- Warehouse sections

**Large Exterior (40-60m):**
- Parking lots
- Courtyards
- Compounds

**Rule of Thumb:** Territory should cover the area the AI patrols/roams

### Zone Design

**Good Zone:**
```
Zone has:
- Clear boundaries (walls, doors)
- Cover points throughout
- Patrol waypoints
- Logical defensive positions
```

**Bad Zone:**
```
Zone issues:
- Territory too small (AI constantly "outside")
- Territory too large (AI chases forever)
- No cover within territory
- Overlapping with other zones
```

### Testing Your Setup

**1. Spawn AI**
- Check console for territory size
- Verify territory radius seems reasonable

**2. Approach AI**
- AI should engage when you enter territory
- Check they use cover within zone

**3. Retreat Far**
- AI should stop chasing at max distance
- AI should return to spawn area

**4. Check Fallback**
- Damage AI until morale < 50%
- Verify they fall back to spawn area
- Check they regroup at defensive position

## Summary

**Territory Defense** transforms AI from relentless hunters into realistic guards who:
- Defend assigned positions
- Use tactical fallbacks
- Prioritize zone security
- Create predictable, clearable encounters

Perfect for tactical shooters where you want:
- Room clearing
- Floor-by-floor progression
- Strategic gameplay
- Realistic guard behavior

Enable it via spawners (automatic) or manually set `defendTerritory = true` on TacticalAI.
