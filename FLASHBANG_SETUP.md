# Flashbang Setup Guide

This guide will help you set up flashbangs in your tactical shooter game.

## What Was Created

Two new scripts have been added to your `Assets/Scripts/` folder:

1. **FlashbangGrenade.cs** - The flashbang grenade projectile
2. **FlashbangEffect.cs** - The screen flash effect for players

## Available Assets

You already have these flashbang assets from Synty:

**Models:**
- `Assets/Synty/PolygonMilitary/Models/SM_Wep_Flashbang_01.fbx`
- `Assets/Synty/PolygonHeist/Model/SM_Wep_Flashbang_Base_01.fbx`
- `Assets/Synty/PolygonPoliceStation/Models/SM_Wep_Grenade_Flash_01.fbx`

**Prefabs:**
- `Assets/Synty/PolygonMilitary/Prefabs/Weapons/SM_Wep_Flashbang_01.prefab`
- `Assets/Synty/PolygonHeist/Prefab/Weapons/SM_Wep_Flashbang_Base_01.prefab`
- `Assets/Synty/PolygonPoliceStation/Prefabs/Weapons/SM_Wep_Grenade_Flash_01.prefab`

**Effects:**
- `Assets/Synty/PolygonHeist/Prefab/FX/FX_FlashBang_01.prefab` (visual effect)

---

## Setup Instructions

### Part 1: Create the Flashbang Projectile Prefab

1. **Create a new GameObject** in your scene
   - Right-click in Hierarchy → Create Empty
   - Name it: `FlashbangProjectile`

2. **Add the flashbang model**
   - Drag one of the flashbang models into `FlashbangProjectile` as a child
   - Recommended: `SM_Wep_Flashbang_01` from PolygonMilitary

3. **Add required components:**
   - Add `Rigidbody` component (for physics)
   - Add `Sphere Collider` or `Capsule Collider` (adjust to fit the model)
   - Add `PhotonView` component (for networking)
   - Add `FlashbangGrenade` script (the one we just created)

4. **Configure FlashbangGrenade script:**
   - **Fuse Time**: 1.5 seconds (adjust to preference)
   - **Effect Radius**: 10 meters
   - **Max Flash Duration**: 5 seconds
   - **Min Flash Duration**: 1 second
   - **Max Deafen Duration**: 4 seconds
   - **Obstacle Mask**: Set to layers that block line of sight (walls, doors, etc.)

5. **Add Light for flash effect:**
   - Add a child GameObject → Add `Light` component
   - Set Type to `Point Light`
   - Set Color to bright white
   - Set Range to `20`
   - Set Intensity to `10`
   - **Disable it by default** (FlashbangGrenade will enable it on detonation)
   - Drag this Light into the `Flash Light` field on FlashbangGrenade script

6. **Add visual effects:**
   - Drag `FX_FlashBang_01.prefab` into the `Flash Effect` field
   - (Or use your own explosion/flash effect)

7. **Add Audio:**
   - Add `Audio Source` component
   - Find or import a loud bang sound effect
   - Drag the bang sound into the `Bang Sound` field
   - Optionally add a grenade pin sound to `Pin Sound` field

8. **Save as Prefab:**
   - Drag `FlashbangProjectile` from Hierarchy into `Assets/Prefabs/` folder
   - Delete from scene

---

### Part 2: Add FlashbangEffect to Player

1. **Find your player prefab** (the character controller)

2. **Add FlashbangEffect script to the player:**
   - Option A: Add it to the player's Camera
   - Option B: Add it to the root player GameObject
   - The script will auto-create the UI canvas and flash overlay

3. **Configure FlashbangEffect (optional):**
   - **Flash Color**: White (default)
   - **Enable Ringing Sound**: Check this
   - **Ringing Sound**: Add a ringing/tinnitus audio clip if you have one
   - **Audio Muffle Amount**: 0.7 (adjust to preference)

4. **Save the player prefab**

---

### Part 3: Create Throwable Flashbang Item (Opsive Integration)

Since you're using Opsive's Ultimate Character Controller, you can integrate the flashbang with the existing grenade system:

#### Option A: Duplicate Existing Frag Grenade Setup

1. **Duplicate the Frag Grenade prefabs:**
   - Find: `Assets/Opsive/.../PhotonPUN/Demo/Prefabs/Items/FragGrenadeWeaponRightPun.prefab`
   - Duplicate it and rename to: `FlashbangWeaponRightPun.prefab`
   - Do the same for `FragGrenadeWeaponLeftPun.prefab`

2. **Replace the projectile:**
   - Open `FlashbangWeaponRightPun` prefab
   - Find the component that references `FragGrenadeProjectilePun`
   - Change it to reference your new `FlashbangProjectile` prefab

3. **Replace the model:**
   - Replace the frag grenade model with one of the flashbang models
   - Use: `SM_Wep_Flashbang_01` or similar

4. **Update the throwable properties:**
   - The grenade should already have throwing mechanics from Opsive
   - Adjust throw force, arc, etc. as needed

#### Option B: Make it a Standalone Pickup

1. **Create pickup prefab:**
   - Duplicate one of the flashbang model prefabs
   - Add `Item Pickup` component (from Opsive)
   - Configure it to add flashbangs to inventory

2. **Place in scene** where players can find flashbangs

---

### Part 4: Configure Photon Networking

1. **Register the prefab with Photon:**
   - Create folder: `Assets/Resources/` (if it doesn't exist)
   - Move or copy `FlashbangProjectile` prefab to `Assets/Resources/`
   - This allows PhotonNetwork to spawn it over the network

2. **Test in multiplayer:**
   - The FlashbangGrenade script already uses `photonView.RPC` for networking
   - The FlashbangEffect only runs on local players (auto-detects)

---

## How It Works

### Flashbang Mechanics:

1. **Player throws flashbang** (using Opsive's throw system)
2. **Fuse starts immediately** (default 1.5 seconds)
3. **On detonation:**
   - Bright light flash
   - Loud bang sound
   - Checks all players within radius (10m)
   - Line-of-sight check (walls block the effect)
   - Applies flash effect based on distance

### Flash Effect:

- **Close range**: 5 seconds of white screen + 4 seconds deafened
- **Far range**: 1 second of white screen + 2 seconds deafened
- **Behind wall**: No effect
- **Screen gradually fades** from white to normal (smooth transition)
- **Audio muffling** reduces game sounds while deafened

---

## Testing

1. **Solo test:**
   - Place player in scene
   - Spawn a flashbang projectile near the player
   - Run the scene
   - The flashbang should detonate and flash you

2. **Multiplayer test:**
   - Build and run two instances
   - Throw flashbang at another player
   - Should work over network

---

## Customization Tips

### Make flashbangs stronger/weaker:
- Adjust `Effect Radius` (bigger = affects more area)
- Adjust `Max Flash Duration` (longer = more blinding)
- Adjust `Fade Curve` for different fade effects

### Add directional awareness:
- Currently, flashbang affects equally regardless of which way player is looking
- You could modify `FlashbangGrenade.cs` to check if player is looking at the grenade
- Use `Vector3.Dot` to check camera forward vs direction to grenade

### Add more effects:
- Add screen shake (use the `CameraShake` class like BreachingCharge does)
- Add blur or distortion effects
- Add particle effects (sparks, smoke)

---

## Troubleshooting

**Flashbang doesn't explode:**
- Make sure `FlashbangGrenade` script is attached
- Check that `fuseTime` is set properly
- Check Console for debug logs

**Players not getting flashed:**
- Make sure `FlashbangEffect` is on the player prefab
- Check that `Effect Radius` is large enough
- Check that `Obstacle Mask` isn't blocking the effect
- Check Console for "Flashing [playername]" messages

**White screen doesn't appear:**
- Make sure `FlashbangEffect` is on the LOCAL player only
- Check that Canvas and Image were created (should auto-create)
- Check that `flashImage.color` alpha is being set

**Networking issues:**
- Make sure `FlashbangProjectile` is in `Assets/Resources/` folder
- Make sure it has a `PhotonView` component
- Check PhotonView is not null in logs

---

## Next Steps

Once flashbangs are working, you can:

1. **Add to AI enemies** - Make enemies react to flashbangs
2. **Add to inventory** - Limit number of flashbangs per player
3. **Add tactical deployment** - Coordinated breach with door kick + flashbang
4. **Add variations** - Different flashbang types (9-bang, etc.)

Enjoy your tactical gameplay!
