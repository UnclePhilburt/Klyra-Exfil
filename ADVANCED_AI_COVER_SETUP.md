# Advanced AI Cover System - Setup Guide

This guide shows you how to set up the new dynamic AI cover system that makes enemies use cover more intelligently.

---

## What This System Does

The AI will now:
- ✅ **Run to cover when seeing player**
- ✅ **Shoot FROM cover** (waist-high cover allows shooting)
- ✅ **Dynamically relocate** to different cover positions
- ✅ **Vary behavior each time** (sometimes defensive, sometimes aggressive)
- ✅ **Use cover type intelligently** (Stand/Crouch/Prone from CoverPoint component)
- ✅ **Attempt flanking maneuvers** when aggressive
- ✅ **Fall back** when enemy gets too close

---

## Setup Instructions

### Step 1: Add the Component to Your AI

1. **Select your AI enemy** in the scene
2. **Add Component** → Search for **`AdvancedAICombatTactics`**
3. The component will automatically find the TacticalAI component

That's it! The AI will now use the advanced tactics system.

---

## How It Works

### Cover Detection
The system reads the **CoverPoint** component on each cover object:
- **Stand cover** (waist-high) - AI can shoot from it
- **Crouch cover** (lower) - AI crouches to use it
- **Prone cover** (very low) - AI goes prone

The AI automatically detects which type of cover it's using and adjusts behavior accordingly.

### Dynamic Behavior
Each AI randomly picks a **combat style** when entering combat:

**Defensive (25% chance):**
- Stays in cover longer (10-15 seconds)
- Prefers Crouch cover
- Relocates less often
- More cautious

**Balanced (50% chance):**
- Normal tactical behavior
- 5-15 seconds at each cover
- Balanced cover preference
- Standard relocation

**Aggressive (25% chance):**
- Stays in cover less (3-8 seconds)
- Prefers Stand cover
- Relocates frequently
- Attempts flanking

The AI also **changes style** every 20 seconds, so it's unpredictable!

---

## Configuration Options

### Combat Behavior
- **Min/Max Time At Cover** - How long AI stays before considering moving
- **Relocate Chance** - Probability of relocating (0-1)
- **Shots Before Relocate** - After firing this many shots, AI considers moving

### Flanking
- **Flanking Chance** - How often AI tries to flank (0-1)
- **Flanking Search Range** - Distance to search for flanking positions
- **Min Flank Angle** - Minimum angle for flanking (default 60°)

### Engagement
- **Preferred Engagement Range** - Distance AI prefers to fight from (default 12m)
- **Fallback Distance** - If enemy gets closer, AI falls back (default 4m)

### Debug
- **Debug Tactics** - Enable to see debug logs and gizmos

---

## Visual Debugging

When you **select the AI in Scene view**, you'll see:

- **Colored sphere above AI** - Shows combat style:
  - **Blue** = Defensive
  - **Yellow** = Balanced
  - **Red** = Aggressive

- **Cyan line to cover** - Shows where AI is relocating to

- **Green line** - Current cover position

- **Yellow wireframe sphere** - Preferred engagement range

- **Red wireframe sphere** - Fallback distance

---

## Example Scenarios

### Scenario 1: Defensive AI
1. Player enters room
2. AI detects player
3. AI runs to nearest Crouch cover
4. AI shoots from cover for 10-15 seconds
5. AI relocates to different cover
6. Repeats

### Scenario 2: Aggressive AI
1. Player enters room
2. AI detects player
3. AI runs to Stand cover
4. AI shoots from cover for 3-5 seconds
5. AI relocates to **flanking** position
6. AI pushes forward from new angle
7. Repeats

### Scenario 3: Player Rushes AI
1. AI is in cover, shooting
2. Player rushes close (<4m)
3. AI **falls back** to different cover
4. AI continues fighting from safer distance

---

## Tips for Best Results

### 1. Place Lots of Cover Points
- The more cover you have, the more dynamic AI movement will be
- Place them at different heights (Stand, Crouch, Prone)
- Spread them around the room for flanking opportunities

### 2. Vary Cover Types
- Mix Stand and Crouch cover
- Aggressive AI will prefer Stand
- Defensive AI will prefer Crouch

### 3. Create Flanking Routes
- Place cover at 60-90° angles from each other
- AI will naturally use these for flanking

### 4. Adjust Per AI
- Make some AI more aggressive (increase relocate chance)
- Make some AI defensive (increase time at cover)
- This creates variety in encounters

### 5. Enable Debug Mode
- Turn on "Debug Tactics" to see what AI is thinking
- Watch the console for relocation reasons
- Use Scene view gizmos to visualize behavior

---

## Advanced: Combining with Existing Features

This system works seamlessly with:
- **Flashbangs** - AI resets when flashbanged
- **Voice commands** - AI can still comply
- **Damage system** - Taking damage increases relocation chance
- **Multiple AI** - Each AI has independent behavior

---

## Troubleshooting

### AI doesn't relocate:
- Check "Relocate Chance" is > 0
- Check there are multiple CoverPoints in range
- Enable "Debug Tactics" to see decisions

### AI relocates too often:
- Increase "Min Time At Cover"
- Decrease "Relocate Chance"
- Increase "Shots Before Relocate"

### AI doesn't flank:
- Check "Flanking Chance" is > 0
- Make sure cover is placed at angles (60-90° from current position)
- AI needs aggression > 0.5 to flank

### AI shoots even when not at cover:
- This is intentional! AI shoots FROM cover (waist-high)
- AI always shoots when in combat, regardless of cover state
- If you want peek/hide behavior, modify `ShouldAllowShooting()`

---

## Next Steps

1. **Add the component** to all your AI enemies
2. **Place lots of CoverPoints** around your maps
3. **Test different combat styles** by watching AI behavior
4. **Adjust settings** to your preference
5. **Enjoy dynamic tactical AI combat!**

The AI will now feel much more realistic and unpredictable!
