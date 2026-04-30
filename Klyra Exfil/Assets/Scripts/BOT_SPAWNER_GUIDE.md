# Updated Bot Spawner - With AI Personality System

## What's New?

The BotSpawner now **automatically adds all AI systems** to spawned bots and **randomizes their personalities** for variety!

## Features

### ✅ Auto-Add AI Systems
Spawned bots automatically get:
- `AI Morale System`
- `AI Fallback System`
- `AI Surrender Animation`

No need to manually add these to your bot prefab!

### ✅ AI Personality Types
Each bot gets a random personality that affects their behavior:

**🐔 COWARD (20% chance)**
- Surrenders early (40-50% morale)
- Panics easily (15-25% morale)
- Falls back quickly (55-65% morale)
- Takes only 2-3 hits before retreating
- **Behavior:** Runs away fast, surrenders often

**😐 NORMAL (30% chance)**
- Balanced surrender threshold (25-35% morale)
- Normal panic threshold (8-12% morale)
- Standard fallback (45-55% morale)
- Takes 3 hits before retreating
- **Behavior:** Realistic, Ready or Not style

**💪 BRAVE (30% chance)**
- Tough fighter (surrenders at 15-25% morale)
- Rarely panics (5-10% morale)
- Fights longer before fallback (35-45% morale)
- Takes 4-5 hits before retreating
- **Behavior:** Aggressive, holds ground

**🔥 FEARLESS (20% chance)**
- Will fight to near-death (10-20% morale to surrender)
- Almost never panics (2-8% morale)
- Barely retreats (25-35% morale)
- Takes 5-7 hits before falling back
- **Behavior:** Extremely dangerous, fights until critical

## Setup

### Option 1: Keep Prefab Clean (Recommended)

**Your bot prefab only needs:**
- TacticalAI
- AdvancedAICombatTactics (optional but recommended)
- NavMeshAgent
- Health component
- UCC components

**BotSpawner will add:**
- AI Morale System
- AI Fallback System
- AI Surrender Animation

**Advantage:** Clean prefab, easy to update

### Option 2: Pre-Configure Prefab

**Add all systems to prefab manually:**
- Disable `Auto Add AI Systems` in BotSpawner
- Manually add the 3 AI systems to your prefab
- Set custom default values

**Advantage:** Full control over default settings

## BotSpawner Settings

### Bot Settings
- **Bot Prefab Name**: Name of prefab in Resources folder (e.g., "BikerCriminal")

### Spawn Amount Randomization
- **Min Bots**: Minimum to spawn (0-10)
- **Max Bots**: Maximum to spawn (1-20)

### Spawn Points
- **Spawn Points**: Array of Transform spawn locations
- **Reuse Spawn Points**: Allow multiple bots per spawn point

### AI Systems (NEW!)
- **Auto Add AI Systems**: ✓ Enable to auto-add morale/fallback/surrender
- **Randomize Morale Settings**: ✓ Enable for personality variety

## Examples

### Example 1: Mixed Squad
**Setup:**
- Min Bots: 3
- Max Bots: 5
- Auto Add: ✓
- Randomize: ✓

**Result:**
- Spawns 3-5 bots
- Each has random personality
- Might get: 1 Coward, 2 Normal, 1 Brave, 1 Fearless
- Creates varied combat (some retreat, some fight hard)

### Example 2: Elite Squad (All Brave)
**Setup:**
- Auto Add: ✓
- Randomize: ✗ (uncheck)
- Manually set morale in prefab:
  - Surrender Threshold: 15
  - Panic Threshold: 5
  - Fallback Threshold: 35

**Result:**
- All bots are tough fighters
- Hard to make them retreat
- Will fight to the death

### Example 3: Scared Guards (All Cowards)
**Setup:**
- Auto Add: ✓
- Randomize: ✗
- Manually set in prefab:
  - Surrender Threshold: 45
  - Panic Threshold: 20
  - Fallback Threshold: 60

**Result:**
- Guards easily scared
- Quick to surrender
- More stealth-friendly gameplay

## Combat Scenarios

### Scenario: 5 Random Bots vs 2 Players

**Spawned Personalities:**
1. Bot 1: FEARLESS
2. Bot 2: NORMAL
3. Bot 3: COWARD
4. Bot 4: BRAVE
5. Bot 5: NORMAL

**Combat Flow:**

**[Start]** All 5 bots engage

**[Player kills Bot 1 (Fearless)]**
- Bot 3 (Coward) sees death, morale drops to 50
- Bot 3 immediately falls back!

**[Players push harder]**
- Bot 3 (Coward) morale hits 40 → surrenders!
- Bots 2, 4, 5 still fighting

**[Player kills Bot 4 (Brave)]**
- Bot 2 (Normal) sees death, morale at 35
- Bot 5 (Normal) morale at 40
- Both fall back to regroup

**[Bots regroup]**
- Bot 2 & 5 get +20 morale
- Counter-attack together!

**[Player keeps pressure]**
- Bot 2 morale drops to 25 → surrenders
- Bot 5 (last survivor) morale critical
- Bot 5 surrenders too

**Result:** Dynamic, realistic fight with varied AI responses!

## Debug Info

When **Debug Morale** is enabled on spawned bots, you'll see:

```
[BikerCriminal_1]: Personality = COWARD (easily scared)
[BikerCriminal_2]: Personality = BRAVE (tough fighter)
[BikerCriminal_3]: Personality = NORMAL (balanced)
[BikerCriminal_4]: Personality = FEARLESS (will fight to the death)
[BikerCriminal_5]: Personality = NORMAL (balanced)
```

## Personality Distribution

With 10 bots:
- ~2 Cowards
- ~3 Normal
- ~3 Brave
- ~2 Fearless

Perfect balance for varied gameplay!

## Testing

### Test 1: Spawn and Check Personalities

1. Play mode
2. Check Console for spawn messages
3. Look for "Personality = X" messages
4. You should see a mix of personalities

### Test 2: Fight Different Personalities

1. Find the COWARD bot
2. Shoot it a few times
3. Watch it retreat/surrender quickly

4. Find the FEARLESS bot
5. Shoot it a lot
6. Watch it keep fighting stubbornly

### Test 3: Verify Systems Added

1. Play mode
2. Select a spawned bot in Hierarchy
3. Check Inspector
4. Verify all 3 systems are present:
   - AI Morale System ✓
   - AI Fallback System ✓
   - AI Surrender Animation ✓

## Troubleshooting

**Issue: Bots spawn but don't have AI systems**
- Check `Auto Add AI Systems` is enabled
- Check Console for "Added AIMoraleSystem" messages
- Make sure bot prefab has TacticalAI component

**Issue: All bots behave the same**
- Enable `Randomize Morale Settings`
- Check Console for personality messages
- Different personalities should be assigned

**Issue: Bots spawn but don't move/fight**
- This is a separate issue (not spawner-related)
- Check the AI_TROUBLESHOOTING_GUIDE.md
- Make sure NavMesh is baked
- Make sure cover points exist

## Performance Note

Adding components at runtime is fine for reasonable bot counts (up to 20-30 bots). If spawning 100+ bots, consider pre-adding components to the prefab instead.

## Future Customization

You can easily add more personality types or tweak existing ones in the `RandomizeBotMorale()` method:

```csharp
// Example: Add "BERSERKER" personality
else if (personalityRoll < 0.9f) // 10% chance
{
    morale.surrenderMoraleThreshold = 5f; // Never surrenders
    morale.panicMoraleThreshold = 0f; // Never panics
    fallback.fallbackMoraleThreshold = 10f; // Rarely retreats
    fallback.hitsBeforeFallback = 10; // Takes tons of damage
}
```

Happy bot spawning! 🤖
