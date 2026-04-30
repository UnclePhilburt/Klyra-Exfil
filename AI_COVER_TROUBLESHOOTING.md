# AI Cover System - Troubleshooting Guide

## Quick Checklist

Before testing, verify:

### 1. Components Are Attached
- [ ] AI has **TacticalAI** component
- [ ] AI has **AdvancedAICombatTactics** component
- [ ] Both components are **enabled**

### 2. Cover Points Exist
- [ ] Scene has **CoverPoint** components
- [ ] CoverPoints are **within 15m** of AI spawn
- [ ] CoverPoints have `isAvailable = true`

### 3. Settings Are Configured
On **TacticalAI**:
- [ ] `useCover = true`
- [ ] `coverSearchRange = 15` (or higher)

On **AdvancedAICombatTactics**:
- [ ] `shotsBeforeSeekingCover = 3` (or desired value)
- [ ] `debugTactics = true` (for testing)

---

## Expected Console Output

When AI sees player, you should see:

```
[AI_Name] Player detected! Engaging!
[AI_Name] *** SHOT FIRED 1/3 - Still in initial engagement ***
[AI_Name] *** SHOT FIRED 2/3 - Still in initial engagement ***
[AI_Name] *** SHOT FIRED 3/3 - Still in initial engagement ***
[AI_Name] *** INITIAL ENGAGEMENT COMPLETE (3 shots) - SEEKING COVER NOW ***
[AI_Name] *** SEEKING INITIAL COVER - Calling ForceSeekCover() ***
[AI_Name] Force seeking cover at CoverPoint_01
[AI_Name] SUCCESS! Cover assigned: CoverPoint_01
[AI_Name] Now at cover: CoverPoint_01 (Stand)
```

---

## Common Issues & Solutions

### Issue: No shot messages appear

**Symptom:** AI detects player but no "SHOT FIRED" messages

**Possible Causes:**
1. AI can't see player (line of sight blocked)
2. AI has no weapon equipped
3. Weapon is out of ammo
4. Use ability not configured in Opsive

**Solution:**
- Check AI has weapon in Opsive inventory
- Check weapon has ammo
- Enable `debugDetection = true` on TacticalAI
- Look for "Firing at target!" messages

---

### Issue: Shots counted but no cover seeking

**Symptom:** See "SHOT FIRED 1/3, 2/3, 3/3" but no "SEEKING COVER"

**Possible Causes:**
1. AdvancedAICombatTactics not entering combat state
2. Component disabled
3. `Update()` not being called

**Solution:**
- Check component is enabled in Inspector
- Check AI currentState = Combat
- Add debug log at start of `UpdateCombatTactics()`

---

### Issue: Cover seeking triggered but no cover assigned

**Symptom:** See "SEEKING INITIAL COVER" but "WARNING! No cover was assigned"

**Possible Causes:**
1. No CoverPoints in scene
2. CoverPoints too far away (>15m)
3. All CoverPoints occupied by other AI
4. CoverPoints have `isAvailable = false`

**Solution:**
- Add CoverPoint objects to scene
- Move CoverPoints closer to AI
- Increase `coverSearchRange` on TacticalAI
- Check CoverPoint settings in Inspector

---

### Issue: Cover assigned but AI doesn't move

**Symptom:** "SUCCESS! Cover assigned: X" but AI stays still

**Possible Causes:**
1. NavMesh not baked
2. AI not on NavMesh
3. Cover point not on NavMesh
4. PathfindingMovement ability not configured

**Solution:**
- Bake NavMesh (Window → AI → Navigation → Bake)
- Check AI is on NavMesh (blue area in Scene view)
- Check CoverPoint is on NavMesh
- Verify Opsive PathfindingMovement ability exists

---

### Issue: AI moves but doesn't reach cover

**Symptom:** AI starts moving but stops before reaching cover

**Possible Causes:**
1. NavMesh path blocked/incomplete
2. Cover too close to obstacles
3. NavMesh agent stopping distance too large

**Solution:**
- Check NavMesh path is complete (white path gizmo)
- Move CoverPoint away from walls
- Reduce NavMeshAgent `stoppingDistance` (try 0.5)

---

### Issue: AI reaches cover but "relocates" immediately

**Symptom:** AI reaches cover then immediately seeks new cover

**Possible Causes:**
1. `minTimeAtCover` too low
2. Current cover being released prematurely
3. Relocation logic triggering too soon

**Solution:**
- Increase `minTimeAtCover` to 10+
- Check for duplicate AdvancedAICombatTactics components
- Disable relocation temporarily (`relocateChance = 0`)

---

## Debug Mode Checklist

Enable these for maximum debugging:

On **TacticalAI**:
- `debugDetection = true`
- `debugVision = true`

On **AdvancedAICombatTactics**:
- `debugTactics = true`

You'll see:
- AI vision cones and rays (Scene view)
- Detection messages
- Shot counting
- Cover seeking triggers
- Cover assignment confirmation
- Movement updates

---

## Manual Testing Steps

### Test 1: Basic Detection
1. Place AI in scene
2. Play game
3. Walk in front of AI
4. **Expected:** "Player detected! Engaging!"

### Test 2: Initial Shots
1. Continue from Test 1
2. Watch console
3. **Expected:** 3x "SHOT FIRED" messages

### Test 3: Cover Seeking
1. Continue from Test 2
2. Wait for 3 shots
3. **Expected:** "SEEKING COVER NOW" message

### Test 4: Cover Assignment
1. Continue from Test 3
2. Check console
3. **Expected:** "SUCCESS! Cover assigned: X"

### Test 5: Movement
1. Continue from Test 4
2. Watch AI in Game/Scene view
3. **Expected:** AI runs toward cover

### Test 6: Arrival
1. Continue from Test 5
2. Wait for AI to reach cover
3. **Expected:** "Now at cover: X (Stand/Crouch)"

### Test 7: Combat From Cover
1. Continue from Test 6
2. AI should keep shooting
3. **Expected:** AI stays and fights from cover

---

## Performance Check

If AI seems slow or laggy:

1. **Reduce Update Frequency:**
   - Increase `threatCheckInterval` (TacticalAI)
   - Increase `aggressionChangeInterval` (AdvancedAICombatTactics)

2. **Simplify Cover Search:**
   - Reduce `coverSearchRange`
   - Use fewer CoverPoints in scene

3. **Disable Debug Mode:**
   - Set `debugTactics = false`
   - Set `debugDetection = false`
   - Set `debugVision = false`

---

## Still Not Working?

If AI still won't use cover after all checks:

1. **Remove AdvancedAICombatTactics** temporarily
2. **Test base TacticalAI cover** (triggered by taking damage)
3. **Shoot the AI** - do they seek cover?

**If base cover works:**
- Problem is in AdvancedAICombatTactics integration
- Check component initialization
- Verify `tacticalAI` reference is not null

**If base cover doesn't work:**
- Problem is in base TacticalAI or scene setup
- Check CoverPoints exist and are configured
- Check NavMesh is baked
- Check Opsive configuration

---

## Quick Fix: Force Cover Immediately

For testing, you can skip the "shoot first" behavior:

Set `shotsBeforeSeekingCover = 0` on AdvancedAICombatTactics

AI will seek cover immediately when detecting player (no initial shots).

---

## Getting Help

When asking for help, provide:

1. **Console output** (copy all AI messages)
2. **AI Inspector screenshot** (both components)
3. **Scene screenshot** (showing AI and CoverPoints)
4. **NavMesh screenshot** (Scene view with NavMesh visible)

This helps diagnose the issue quickly!
