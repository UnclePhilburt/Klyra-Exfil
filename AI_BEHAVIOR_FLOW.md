# AI Behavior Flow - How It Works

## Combat Sequence

### Phase 1: Detection & Initial Engagement
1. **AI detects player** (via sight/hearing)
2. **Enters Combat state**
3. **Opens fire IMMEDIATELY** (no cover yet)
4. **Fires 3 shots in the open** (configurable via `shotsBeforeSeekingCover`)

### Phase 2: Cover Seeking
5. **After 3 shots fired** → AI seeks cover
6. **Calls `ForceSeekCover()`** on TacticalAI
7. **Finds best cover** using CoverPoint system
8. **Runs to cover position**

### Phase 3: Cover Combat
9. **Reaches cover** (within 1.5m)
10. **Continues shooting FROM cover** (waist-high allows shooting)
11. **Stays for 5-15 seconds** (random, based on combat style)
12. **Fires 8+ shots** from this position

### Phase 4: Relocation Decision
13. **Checks if should relocate:**
    - Time at cover exceeded?
    - Fired too many shots (8+)?
    - Aggressive AI? (random chance)
    - Taking fire?
    - Enemy too close (<4m)?

14. **If relocating:**
    - **25% chance** → Try flanking position (if aggressive)
    - **75% chance** → Find best tactical cover
    - **Move to new cover**
    - **Repeat from Phase 3**

15. **If staying:**
    - **No other cover available** → Stay put (OK!)
    - **Current cover is best** → Stay put (OK!)
    - **Continue fighting from same position**

---

## Combat Styles (Randomized)

### Defensive (25% chance)
- Aggression: 0.2 - 0.4
- Stays in cover: 10-15 seconds
- Prefers: Crouch cover
- Relocates: Less often
- Flanking: Rare

### Balanced (50% chance)
- Aggression: 0.4 - 0.7
- Stays in cover: 5-15 seconds
- Prefers: Any cover
- Relocates: Normally
- Flanking: Sometimes

### Aggressive (25% chance)
- Aggression: 0.7 - 1.0
- Stays in cover: 3-8 seconds
- Prefers: Stand cover
- Relocates: Frequently
- Flanking: Often

---

## Cover Types (from CoverPoint component)

### Stand Cover
- Full height (walls, tall obstacles)
- AI can shoot while standing
- Preferred by aggressive AI

### Crouch Cover
- Medium height (crates, low walls)
- AI can shoot while crouching
- Preferred by defensive AI

### Prone Cover
- Very low (requires prone position)
- AI can shoot while prone
- Used when no other options

---

## Debug Tips

### Enable Debug Mode
Set `debugTactics = true` in AdvancedAICombatTactics to see:
- "Initial engagement shot 1/3" - Counting shots before seeking cover
- "Initial engagement complete - seeking cover now" - Triggered cover seek
- "Force seeking cover at [name]" - Found and moving to cover
- "Now at cover: [name] (Stand/Crouch/Prone)" - Reached cover
- "Relocating to: [name]" - Moving to new cover
- "No other cover available - staying put" - No relocation

### Console Messages
Watch for these key messages:
1. "Player detected! Engaging!" - Combat started
2. "Force seeking cover at X" - Moving to cover
3. "Now at cover: X" - At cover position
4. "Relocating to: Y" - Changing positions

### Scene View Gizmos
When AI selected:
- **Blue sphere** = Defensive style
- **Yellow sphere** = Balanced style
- **Red sphere** = Aggressive style
- **Green line** = Current cover
- **Cyan line** = Target cover (relocating)

---

## Common Issues

### "AI not seeking cover"
✅ Check you have CoverPoint components in scene
✅ Check cover is within `coverSearchRange` (default 15m)
✅ Check `useCover = true` on TacticalAI
✅ Enable debug mode to see if cover seeking is triggered

### "AI seeks cover immediately"
✅ Check `shotsBeforeSeekingCover` (should be 3+)
✅ Make sure AdvancedAICombatTactics is attached

### "AI doesn't relocate"
✅ Check `relocateChance` > 0
✅ Check there are multiple CoverPoints available
✅ AI stays at best cover if no better options (this is correct!)

### "AI relocates too much"
✅ Increase `minTimeAtCover` and `maxTimeAtCover`
✅ Decrease `relocateChance`
✅ Increase `shotsBeforeRelocate`

---

## Settings Reference

### AdvancedAICombatTactics

**Combat Behavior:**
- `shotsBeforeSeekingCover` = 3 (shots fired before seeking cover)
- `minTimeAtCover` = 5 (min seconds at cover)
- `maxTimeAtCover` = 15 (max seconds at cover)
- `relocateChance` = 0.5 (chance to relocate)
- `shotsBeforeRelocate` = 8 (shots before considering relocation)

**Flanking:**
- `flankingChance` = 0.25 (chance to try flanking)
- `flankingSearchRange` = 15m
- `minFlankAngle` = 60° (minimum angle for flanking)

**Engagement:**
- `preferredEngagementRange` = 12m (ideal distance)
- `fallbackDistance` = 4m (fall back if closer)

**Debug:**
- `debugTactics` = false (enable for debug logs)

---

## Summary

The AI now:
1. ✅ **Shoots FIRST** when seeing player (3 shots)
2. ✅ **Seeks cover SECOND** (after initial engagement)
3. ✅ **Shoots FROM cover** (doesn't hide)
4. ✅ **Stays at cover** if it's the best/only option
5. ✅ **Relocates dynamically** when better options exist
6. ✅ **Varies behavior** (defensive/balanced/aggressive)
7. ✅ **Attempts flanking** when aggressive

This creates realistic, unpredictable AI that feels tactical!
