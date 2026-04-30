# AI Not Using Cover / Not Retreating - Troubleshooting Guide

## Problem: AI Just Stand and Shoot

If your AI are just standing in place and shooting without using cover or retreating, here's how to fix it:

## Checklist

### 1. Do you have CoverPoint objects in your scene?

**Check:**
- Open your scene
- Search for "CoverPoint" objects
- You should have several cover points placed around the map

**If you have ZERO cover points:**
- The AI can't use cover because there's none to use!
- Create empty GameObjects
- Add the `CoverPoint` component
- Position them behind walls, crates, corners, etc.

**Quick Test:**
- Select an AI in scene
- In TacticalAI component, check `Cover Search Range` (default: 15m)
- Make sure there's at least one CoverPoint within 15m

### 2. Are the morale/fallback components attached?

**Check:**
- Select your AI prefab/character
- Look in Inspector for these components:
  - `AI Morale System` ✓
  - `AI Surrender Animation` ✓
  - `AI Fallback System` ✓

**If missing:**
- The AI won't retreat because the systems don't exist!
- Add all three components to your AI prefab

### 3. Is TacticalAI's useCover enabled?

**Check:**
- Select AI
- Find `TacticalAI` component
- Check `Use Cover` checkbox

**If unchecked:**
- AI won't seek cover at all
- Enable it

### 4. Are there any errors in console?

**Check:**
- Open Unity Console (Ctrl+Shift+C)
- Look for red error messages
- Common errors:
  - "No NavMeshAgent" - Add NavMeshAgent component
  - "No cover found" - Add more CoverPoint objects
  - "Reflection error" - Health component issue (should be fixed now)

### 5. Is the NavMesh baked?

**Check:**
- Window → AI → Navigation
- Select your level geometry
- Click "Bake" tab
- Make sure NavMesh is baked (blue overlay in scene view)

**If not baked:**
- AI can't pathfind to cover!
- Click "Bake" button
- Make sure `NavMesh Agent` component is on AI

## Testing Steps

### Test 1: Force Cover Seeking

1. Play mode
2. Select AI in Hierarchy
3. Find `TacticalAI` component
4. Click the three dots menu → "Force Seek Cover"
5. Watch console - does it say "Moving to cover point X"?

**If YES:** Cover system works, but AI aren't triggering it naturally
**If NO:** Cover points might not exist or aren't in range

### Test 2: Force Fallback

1. Play mode
2. Select AI in Hierarchy
3. Find `AI Morale System` component
4. Set `Current Morale` to **48** (just below fallback threshold of 50)
5. Watch AI behavior

**Expected:** AI should find rally point and retreat
**If nothing happens:** Check console for errors, make sure AIFallbackSystem is attached

### Test 3: Surrender Test

1. Play mode
2. Select AI
3. Set `AI Morale System → Current Morale` to **8** (panic level)
4. Watch AI

**Expected:** AI drops weapon and says "I surrender!"
**If nothing happens:** Check AISurrenderAnimation component exists

## Common Issues & Fixes

### Issue: "AI moves to cover but then stands up and ignores it"

**Cause:** Advanced AI Combat Tactics might be overriding
**Fix:** Make sure AdvancedAICombatTactics `debugTactics` is enabled to see what it's doing

### Issue: "AI never fallback, morale just keeps dropping"

**Cause:** No AIFallbackSystem component
**Fix:** Add the AIFallbackSystem component to your AI

### Issue: "AI fallback but run into walls"

**Cause:** NavMesh not baked or cover points off NavMesh
**Fix:**
- Rebake NavMesh
- Move cover points onto walkable NavMesh (blue areas)

### Issue: "Compilation errors about Health.MaxHealth"

**Status:** Should be fixed now with reflection-based approach
**If still happening:** Let me know, there might be a UCC version mismatch

### Issue: "AI use cover initially but then never move again"

**Cause:** They're stuck at cover because AdvancedAICombatTactics isn't triggering relocation
**Fix:**
- Check that AdvancedAICombatTactics is attached
- Enable `debugTactics` on it
- Watch console for relocation messages

## Debug Mode Setup

**Enable ALL debug modes:**

1. **TacticalAI:**
   - `Debug Detection` ✓
   - (Any other debug options)

2. **AdvancedAICombatTactics:**
   - `Debug Tactics` ✓

3. **AIMoraleSystem:**
   - `Debug Morale` ✓

4. **AIFallbackSystem:**
   - `Debug Fallback` ✓

5. **AISurrenderAnimation:**
   - `Debug Surrender` ✓

**Play the game and watch the Console - you should see LOTS of debug messages telling you exactly what the AI are thinking!**

## Quick Setup Guide (From Scratch)

If you want to start fresh:

### 1. Create Cover Points

```
1. Create empty GameObject
2. Name it "Cover_01"
3. Add Component → CoverPoint
4. Set Cover Type (Stand/Crouch/Prone)
5. Position behind wall/obstacle
6. Duplicate 10-20 times around map
```

### 2. Setup AI

```
1. Select AI prefab
2. Add Component → AI Morale System
3. Add Component → AI Surrender Animation
4. Add Component → AI Fallback System
5. Make sure TacticalAI.useCover = true
6. Make sure NavMeshAgent exists
```

### 3. Bake NavMesh

```
1. Window → AI → Navigation
2. Select all floors/walkable geometry
3. Mark as "Navigation Static"
4. Bake tab → Bake
```

### 4. Test

```
1. Play mode
2. Shoot AI a few times
3. Watch them:
   - Seek cover (TacticalAI)
   - Lose morale (AIMoraleSystem)
   - Fallback to regroup (AIFallbackSystem)
   - Eventually surrender (AISurrenderAnimation)
```

## Expected Behavior Flow

**Full Combat Flow:**

1. **Player spotted** → AI enters Combat state
2. **Takes damage** → Seeks nearest cover
3. **More damage** → Morale drops to 70%
4. **Ally dies nearby** → Morale drops to 45%
5. **Morale < 50%** → FALLBACK triggered!
6. AI finds rally point near allies
7. AI retreats while shooting (suppressive fire)
8. AI reaches rally point
9. AI regroups with allies (+20 morale → 65%)
10. **Morale > 65%** → RE-ENGAGE!
11. AI counter-attacks from new position

**If player keeps pushing:**

12. More damage → morale drops again
13. Morale < 30% → considers surrender
14. Checks: outnumbered? isolated? wounded?
15. If YES → **SURRENDERS**
16. Drops weapon, voice line, hands up

## Still Not Working?

If you've tried all of this and it still doesn't work:

1. **Share these details:**
   - Are there CoverPoint objects? (count)
   - Are components attached? (list them)
   - Any console errors? (copy/paste)
   - What does the AI do? (describe exact behavior)

2. **Send a screenshot of:**
   - AI Inspector showing all components
   - Scene view showing cover points
   - Console with debug enabled

3. **Test with a simple scene:**
   - 1 AI
   - 3-4 cover points
   - Simple room
   - Does it work there?
