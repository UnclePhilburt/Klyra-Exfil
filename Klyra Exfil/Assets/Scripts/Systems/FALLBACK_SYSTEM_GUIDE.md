# AI Tactical Fallback & Regroup System

## What This Does

AI now **tactically retreat and regroup** instead of just surrendering when overwhelmed!

When morale is **low but not critical**, AI will:
✅ **Fall back to rally points** (cover near allies)
✅ **Call for backup** (voice lines: "Falling back!", "Need support!")
✅ **Provide suppressive fire** while retreating
✅ **Regroup with allies** (+20 morale bonus when successful)
✅ **Counter-attack** once regrouped and morale restored

## Morale Levels & Behavior

**100-65 Morale:** Full combat - aggressive
**65-50 Morale:** Cautious combat - normal tactics
**50-30 Morale:** **FALLBACK** - retreat to regroup
**30-10 Morale:** Consider surrender (check conditions)
**Below 10 Morale:** Panic surrender

## How Fallback Works

### 1. Trigger Conditions
AI falls back when:
- Morale drops to 50% (worried, not panicking)
- Taking 3+ hits in 5 seconds (heavy fire!)
- Outnumbered but allies exist elsewhere

### 2. Find Rally Point
AI looks for:
- **Cover near allies** (prefer 2+ allies nearby)
- **Behind current position** (away from threat)
- **Defensive positions** (crouch cover preferred)
- **Within 15-30m** from current location

### 3. Tactical Retreat
- Move to rally point via NavMesh
- **Suppressive fire** every 1.5 seconds while moving
- Play voice line: "Falling back!", "Retreating!", etc.

### 4. Regroup
- Check for nearby allies (within 8m)
- When regrouped: **+20 morale bonus**
- Play voice line: "Regrouping!", "Got backup now!"

### 5. Re-engage
- Once morale reaches 65%, stop fallback
- Resume normal combat with renewed confidence
- Now fighting alongside allies = stronger position

## Example Scenarios

### Scenario 1: Isolated Guard Under Fire
**Setup:** 1 AI getting shot, 2 allies around the corner
**Behavior:**
1. Takes 3 hits → morale drops to 45
2. Initiates fallback
3. Finds rally point near allies (around corner)
4. Retreats while shooting back
5. Regroups with allies (+20 morale → 65)
6. All 3 AI counter-attack together!

### Scenario 2: Squad Getting Pushed
**Setup:** 3 AI vs 4 players, losing firefight
**Behavior:**
1. All 3 AI morale drops to 40-50 range
2. All fallback simultaneously
3. Find rally point at defensive choke point
4. Regroup, form defensive line
5. Morale restored, hold position together

### Scenario 3: Last Stand
**Setup:** Last 2 AI survivors, heavily outnumbered
**Behavior:**
1. Morale at 35% (below fallback threshold)
2. Regroup together
3. Too outnumbered to counter-attack
4. Morale continues to drop
5. Eventually surrender together

### Scenario 4: Leapfrog Retreat
**Setup:** 4 AI retreating from player push
**Behavior:**
1. Front 2 AI fallback first
2. Rear 2 AI provide cover fire
3. Front 2 reach rally point
4. Rear 2 then fallback
5. All 4 regroup and establish new defensive position

## Setup

### 1. Add Component
- Select AI character
- Add Component → **AI Fallback System**

### 2. Configure (All Optional - Defaults Work Great!)

**Fallback Triggers:**
- **Fallback Morale Threshold**: 50 (when to retreat)
- **Reengage Morale Threshold**: 65 (when to fight again)
- **Hits Before Fallback**: 3 (heavy fire trigger)
- **Hit Count Window**: 5 seconds

**Fallback Behavior:**
- **Fallback Distance**: 15m (minimum retreat distance)
- **Max Rally Point Search Distance**: 30m
- **Preferred Ally Count**: 2 (look for positions with 2+ allies)
- **Suppressive Fire While Falling**: ✓ Checked
- **Suppressive Fire Interval**: 1.5 seconds

**Regrouping:**
- **Regroup Distance**: 8m (how close to count as regrouped)
- **Regroup Morale Bonus**: 20 (morale gain when regrouped)
- **Fallback Cooldown**: 15 seconds (can't fallback again too soon)

### 3. Voice Lines (Optional)

**Fallback Voice Clips:**
- "Falling back!"
- "Retreating!"
- "Need support!"
- "Taking heavy fire!"
- "Pull back!"

**Regrouped Voice Clips:**
- "Regrouping!"
- "Got backup now!"
- "We're together!"
- "Ready to engage!"

## How It Integrates

**With Morale System:**
- Morale 50-30% = Fallback
- Morale 30-10% = Consider surrender
- Morale <10% = Panic surrender

**With Surrender System:**
- Fallback happens BEFORE surrender
- If fallback successful → morale restored → keep fighting
- If fallback fails (no allies) → morale continues dropping → eventual surrender

**With Cover System:**
- Uses existing CoverPoint system
- Finds rally points at cover positions
- Prefers crouch cover for defense

## Testing

### Quick Test (Force Fallback):
1. Select AI in scene
2. Set **AI Morale System → Current Morale** to **48**
3. Play mode - AI should immediately fallback
4. Watch it find rally point and retreat

### Realistic Test:
1. Setup: 3 AI in a room, 2 more AI in the next room
2. Attack the 3 AI with heavy fire
3. Watch them fallback to the next room
4. See them regroup with the other 2 AI
5. All 5 AI now defend together!

## Debug Visualization

When **Debug Fallback** is enabled (select AI in scene view):

**Yellow wireframe sphere** = Rally point destination
**Yellow line** = Path to rally point
**Cyan wireframe sphere** = Regroup distance (8m)
**Yellow translucent sphere** = Minimum fallback distance (15m)

**Console Logs:**
- "INITIATING FALLBACK! Morale: 45.0, Hits: 3"
- "Falling back to rally point: (123, 0, 456)"
- "Regrouped with 2 allies!"
- "RE-ENGAGING! Morale restored to 70.0"

## Voice Line Suggestions

**Fallback (Generate/Record These):**
- "Falling back!"
- "Tactical retreat!"
- "Pull back! Pull back!"
- "Need to regroup!"
- "Taking heavy fire - retreating!"
- "Moving to cover!"

**Regrouped:**
- "Regrouping!"
- "With you now!"
- "Ready to fight!"
- "Let's push back!"
- "Together now!"
- "We got this!"

## Advanced Tactics

### Make AI More Aggressive (Less Likely to Fallback):
- Decrease **Fallback Morale Threshold** to 35-40
- Increase **Hits Before Fallback** to 5-6
- Decrease **Regroup Morale Bonus** to 10-15

### Make AI More Cautious (Quick to Retreat):
- Increase **Fallback Morale Threshold** to 60-65
- Decrease **Hits Before Fallback** to 2
- Increase **Regroup Morale Bonus** to 30

### "Ready or Not" Realistic Style:
- **Fallback Threshold**: 50
- **Reengage Threshold**: 65
- **Hits Before Fallback**: 3
- **Regroup Bonus**: 20
- **Fallback Cooldown**: 15 seconds

## Troubleshooting

**AI never fallback:**
- Check that morale is between 30-50 range
- Enable **Debug Fallback** to see triggers
- Make sure **AI Morale System** is also attached

**AI fallback but don't regroup:**
- Check that other AI are within **Regroup Distance** (8m)
- Make sure other AI also have AIFallbackSystem
- Enable debug to see ally detection

**AI fallback into corners:**
- This can happen if no good rally points exist
- Add more CoverPoint objects in tactical positions
- AI will use basic retreat if no cover available

**Suppressive fire not working:**
- This is a placeholder in the current implementation
- You can integrate with UCC's weapon system for actual firing
- See `FireSuppressiveShot()` method

## Complete AI Behavior Chain

1. **100-65% Morale:** Normal aggressive combat
2. **65-50% Morale:** Cautious, using cover more
3. **50% Morale:** **FALLBACK INITIATED**
   - Find rally point near allies
   - Retreat while shooting
   - Call for backup
4. **Regrouped:** +20 morale bonus (→ 70%)
5. **70% Morale:** **RE-ENGAGE**
   - Counter-attack with allies
   - Back to normal combat
6. **If fallback fails (no allies):**
   - Morale continues dropping
   - Eventually surrender or panic

This creates dynamic, realistic firefights where AI don't just stand and die - they adapt!
