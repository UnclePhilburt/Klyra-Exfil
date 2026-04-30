# AI Morale & Smart Surrender System

## What This Does

AI now have a **morale system** that makes them surrender intelligently based on:

✅ **Group Strength** - Won't surrender if they outnumber you
✅ **Witness Death** - Lose morale when seeing allies die nearby
✅ **Isolation** - Surrender faster when alone
✅ **Health** - Wounded AI more likely to give up
✅ **Flanking** - Lose morale if shot from behind
✅ **Panic** - Auto-surrender if morale collapses completely

## How It Works

Each AI has a **morale value (0-100)**:
- **100** = Full confidence
- **30 or below** = Considers surrender (but checks conditions first)
- **10 or below** = PANIC - instant surrender

### Morale Goes Down When:
- Taking damage (-5 per hit)
- Witnessing ally deaths (-15 per death seen)
- Being isolated (-2 per second)
- Being flanked/shot from behind (-10)
- Low health (wounded = less morale)

### Morale Goes Up When:
- Nearby allies (+10 per ally, max +40)

## Setup (Quick - 2 steps!)

### 1. Add Components to AI

Select your AI enemy and add **two components**:

1. **AI Morale System** (the smart surrender logic)
2. **AI Surrender Animation** (handles voice lines and weapon drop)

### 2. Configure Settings (Optional)

**AI Morale System:**
- **Surrender Morale Threshold**: 30 (default - when they start considering surrender)
- **Panic Morale Threshold**: 10 (default - instant surrender)
- **Ally Detection Range**: 20m (how far they detect friendly AI)
- **Enemy Detection Range**: 30m (how far they detect players - MULTIPLAYER AWARE!)
- **Minimum Numbers Ratio**: 1.5 (won't surrender if 3v2 in their favor)
- **Morale Penalty Per Nearby Enemy**: 8 (morale lost per second per nearby player)
- **Debug Morale**: ✓ Check this to see morale changes in console

**AI Surrender Animation:**
- **Surrender Voice Clips**: Add 3-5 audio files ("I surrender!", etc.)
- **Drop Weapon On Surrender**: ✓ Checked
- **Debug Surrender**: ✓ Check for logs

## Example Scenarios

### Scenario 1: Outnumbered Squad
**Setup:** 5 AI enemies vs 1 player
**Result:** They WON'T surrender easily - they outnumber you!
**But:** If you kill 3-4 of them, the survivors will panic and surrender

### Scenario 2: Multiplayer 2v3
**Setup:** 2 players vs 3 AI enemies
**Result:** AI check nearby enemies, see 2 players close, decide to fight (3v2 advantage)
**But:** If 1 player flanks behind while the other attacks from front = surrounded AI surrenders!

### Scenario 3: Multiplayer 4v2 (The Last Stand)
**Setup:** 4 players push a room with 2 AI defenders
**Result:** AI see 4 enemies nearby, lose morale rapidly (-8 per enemy per second)
**Result:** Both AI panic and surrender almost immediately (completely outnumbered)

### Scenario 4: Kill Ally in Front of Them
**Setup:** 2 AI enemies, you headshot one
**Result:** The survivor sees it happen (-15 morale), gets scared, checks if isolated, and surrenders!

### Scenario 5: Isolated Guard
**Setup:** 1 AI alone, you shoot them a few times
**Result:** Taking damage + isolated + no backup = quick surrender

### Scenario 6: Flanking Attack
**Setup:** Sneak behind AI and shoot them in the back
**Result:** Flanked penalty (-10) + damage (-5) = rapid morale loss, likely surrender

### Scenario 7: Wounded Survivor
**Setup:** AI at 25% health, low morale
**Result:** Checks health, realizes wounded + demoralized = auto-surrender

### Scenario 8: Multiplayer Pincer Attack
**Setup:** 2 players flank AI from both sides
**Result:** AI detects 2 enemies nearby from different directions = SURROUNDED
**Result:** Instant panic surrender (2+ enemies, no allies)

## Testing the System

### Quick Test (100% Surrender):
1. Select your AI
2. Find **AI Morale System**
3. Set **Current Morale** to **5** (below panic threshold)
4. Play - AI will instantly surrender

### Realistic Test:
1. Keep morale at default (100)
2. Spawn 3 AI enemies
3. Kill one in front of the others
4. Watch the survivors lose morale and consider surrender
5. Kill another - last one should panic surrender

## Debug Visualization

When **Debug Morale** is enabled:

**In Scene View (select AI):**
- Green/Yellow bar above AI head = morale level
- Green wireframe sphere = ally detection range
- Cyan lines = connections to nearby allies

**In Console:**
- "Witnessed ally death! Morale: 65/100"
- "Isolated! Losing morale (45/100)"
- "FLANKED! Major morale loss! (30/100)"
- "Outnumbered and demoralized - surrendering!"

## Advanced Tweaking

### Make AI Braver (Harder to Scare):
- Increase **Surrender Morale Threshold** to 15-20
- Increase **Panic Morale Threshold** to 5
- Decrease **Morale Loss On Ally Death** to 10

### Make AI Cowardly (Easy to Scare):
- Increase **Surrender Morale Threshold** to 40-50
- Increase **Panic Morale Threshold** to 20
- Increase **Morale Loss On Ally Death** to 25

### Realistic "Ready or Not" Style:
- **Surrender Threshold**: 30
- **Panic Threshold**: 10
- **Ally Death Penalty**: 15
- **Minimum Numbers Ratio**: 1.5
- **Health Influence**: 0.5

## Troubleshooting

**AI never surrender:**
- Check that **AI Morale System** is attached
- Enable **Debug Morale** to see morale values
- Try killing allies right in front of them
- Make sure they can "see" ally deaths (within sight range)

**AI surrender too easily:**
- Decrease morale loss values
- Increase surrender thresholds
- Increase minimum numbers ratio

**AI surrender too slowly:**
- Increase morale loss values
- Increase **Morale Loss When Isolated**
- Decrease surrender thresholds

## Voice Lines (Optional)

Add these surrender phrases to **AI Surrender Animation**:
- "I surrender!"
- "Don't shoot!"
- "Okay, okay! I give up!"
- "Please! I'm unarmed!"
- "I don't want to die!"

Generate them for free at:
- https://elevenlabs.io
- https://ttsmaker.com
- https://play.ht

## How Different From Normal Surrender?

**Old System (Voice Command):**
- Random 30% chance to comply
- Triggered by player voice command
- No context awareness

**New System (Morale):**
- Intelligent decision making
- Considers group strength, deaths, health
- Automatic when conditions met
- Still works with voice commands (but smarter)

Both systems work together! Voice commands lower morale, making surrender more likely.
