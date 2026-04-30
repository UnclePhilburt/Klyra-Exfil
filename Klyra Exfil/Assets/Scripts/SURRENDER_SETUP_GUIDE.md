# AI Surrender Setup Guide

## What This Does
When AI complies with voice commands, they will:
- Drop their weapon
- Say a random surrender voice line ("I surrender!", "Don't shoot!", etc.)
- Stand idle for the compliance duration

## Setup Steps

### 1. Get Voice Lines (Optional - works without them too)

**Option A: Record your own**
- Record short phrases like "I surrender!", "Don't shoot!", "Okay okay!"
- Save as `.wav` or `.mp3` files
- Name them clearly (e.g., `ISurrender.wav`, `DontShoot.wav`, `Okay.wav`)

**Option B: Use AI voice generation (FREE)**
- Go to sites like:
  - https://elevenlabs.io (free tier available)
  - https://ttsmaker.com (free)
  - https://play.ht (free trial)
- Generate short surrender phrases
- Download as audio files

**Option C: Use text-to-speech**
- Windows: Use built-in TTS to generate audio
- Or skip voice lines entirely - weapon drop alone is effective!

### 2. Add Script to AI

1. Select your AI character in the scene/prefab
2. **Add Component** → Search for **AI Surrender Animation**
3. In the Inspector:
   - **Surrender Voice Clips**:
     - Set Size to 3 (or however many you want)
     - Drag your audio clips into the slots
   - **Voice Volume**: 0.8 (adjust as needed)
   - **Drop Weapon On Surrender**: ✓ Check this
   - **Debug Surrender**: ✓ Check this to see logs

### 3. Import Audio (if using voice lines)

1. Drag your audio files into Unity (put in `Assets/Media/Sounds/Voices/`)
2. Select each audio clip in Unity
3. In Inspector:
   - **Load Type**: Decompress On Load (for short clips)
   - **Preload Audio Data**: ✓ Checked
   - Click **Apply**

### 4. Test It

1. Start Play mode
2. Get near an AI enemy
3. Use a voice command (your VoiceLineSystem)
4. If the AI complies (30% chance by default), they should:
   - Drop their weapon
   - Say "I surrender!" (or random variant)
   - Stand idle for 5 seconds

## Example Voice Lines

Good surrender phrases to record/generate:
- "I surrender!"
- "Don't shoot!"
- "Okay, okay!"
- "I give up!"
- "Please don't kill me!"
- "I'm unarmed!"
- "Hands up, hands up!"

## How It Works

- Watches TacticalAI's state
- When state changes to **Compliant**:
  - Drops weapon via UCC inventory system
  - Plays random voice line from your array
  - AI stands idle (normal behavior)
- Automatically resets when leaving Compliant state

## Troubleshooting

**Voice lines don't play:**
- Check that clips are assigned in Inspector
- Make sure AudioSource exists (script creates one automatically)
- Enable Debug Surrender to see logs

**AI doesn't surrender:**
- Default compliance chance is 30% (random)
- Increase `complianceChance` in TacticalAI to 1.0 (100%) for testing
- Make sure you're using voice commands within `voiceCommandRange` (10m default)

**Weapon doesn't drop:**
- Check that "Drop Weapon On Surrender" is enabled
- Make sure AI has UCC Inventory system setup
- Check Unity console for errors

## Advanced: Change Compliance Duration

To make AI surrender for longer/shorter:

1. Select AI character
2. Find **TacticalAI** component
3. Change **Compliance Duration** (default: 5 seconds)

## Advanced: Always Surrender (For Testing)

1. Select AI character
2. Find **TacticalAI** component
3. Change **Compliance Chance** to **1.0** (100% surrender rate)

## Works Without Voice Lines!

If you don't add any audio clips, the system will still:
- Drop the weapon
- Make AI stand idle
- Log warnings but continue working

The weapon drop alone is a clear visual indicator of surrender!
