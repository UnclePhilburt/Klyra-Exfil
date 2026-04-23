# Opsive Flashbang Setup Guide (Step-by-Step)

This guide shows you exactly how to set up flashbangs in Opsive's Ultimate Character Controller with Photon PUN networking.

---

## Part 1: Create the Flashbang Projectile Prefab

### Step 1: Create the base projectile GameObject

1. Open Unity
2. In the **Hierarchy**, right-click → **Create Empty**
3. Name it: `FlashbangProjectilePun`
4. Position it at `(0, 0, 0)`

### Step 2: Add the flashbang 3D model

1. In **Project** window, navigate to:
   - `Assets/Synty/PolygonMilitary/Prefabs/Weapons/SM_Wep_Flashbang_01.prefab`

2. **Drag** `SM_Wep_Flashbang_01` into the **Hierarchy** as a **child** of `FlashbangProjectilePun`

3. The hierarchy should look like:
   ```
   FlashbangProjectilePun
   └── SM_Wep_Flashbang_01
   ```

4. Adjust the child model:
   - Position: `(0, 0, 0)`
   - Rotation: `(0, 0, 0)`
   - Scale: `(1, 1, 1)`

### Step 3: Add Physics Components

Select `FlashbangProjectilePun` (the parent), then add components:

1. **Add Component** → **Rigidbody**
   - Mass: `0.5`
   - Drag: `0`
   - Angular Drag: `0.05`
   - Use Gravity: ✅ **Checked**
   - Is Kinematic: ❌ **Unchecked**

2. **Add Component** → **Capsule Collider**
   - Radius: `0.03` (adjust to fit your model)
   - Height: `0.15` (adjust to fit your model)
   - Direction: `Y-Axis`
   - Center: `(0, 0, 0)`

### Step 4: Add Photon Networking

Select `FlashbangProjectilePun`, then:

1. **Add Component** → **Photon View**
   - Observe Options: `Reliable Delta Compressed`
   - Owner: `Takeover`

2. **Add Component** → **Photon Transform View**
   - Synchronize Position: ✅ **Checked**
   - Synchronize Rotation: ✅ **Checked**
   - Synchronize Scale: ❌ **Unchecked**

3. In the **Photon View** component:
   - Click the **+** button under "Observed Components"
   - Drag the **Photon Transform View** component into the slot

### Step 5: Add the Flashbang Script

Select `FlashbangProjectilePun`, then:

1. **Add Component** → Search for **"Flashbang Grenade"**
2. Click it to add the `FlashbangGrenade` script

3. **Configure the script:**
   - **Fuse Time**: `1.5`
   - **Effect Radius**: `10`
   - **Max Flash Duration**: `5`
   - **Min Flash Duration**: `1`
   - **Max Deafen Duration**: `4`
   - **Obstacle Mask**: Click dropdown → Select **Default**, **Wall**, **Door** (whatever blocks vision)
   - Leave other fields empty for now (we'll add effects next)

### Step 6: Add Flash Light Effect

1. Select `FlashbangProjectilePun` in Hierarchy
2. Right-click on it → **Create Empty** (creates a child)
3. Name the child: `FlashLight`
4. Select `FlashLight`, then **Add Component** → **Light**

5. Configure the Light:
   - Type: `Point`
   - Color: **Bright White** (255, 255, 255)
   - Intensity: `10`
   - Range: `20`
   - **Render Mode**: `Important`
   - **IMPORTANT**: **Uncheck** the Light component to disable it by default

6. Select `FlashbangProjectilePun` (parent)
7. In the **FlashbangGrenade** script:
   - Drag the **FlashLight** child object into the **Flash Light** field
   - Set **Flash Light Duration**: `0.3`
   - Set **Flash Light Intensity**: `10`

### Step 7: Add Visual Effects

1. Select `FlashbangProjectilePun`
2. In the **FlashbangGrenade** script:
   - **Flash Effect** field: Drag `Assets/Synty/PolygonHeist/Prefab/FX/FX_FlashBang_01.prefab` into it
   - (Or use any explosion particle effect you prefer)

### Step 8: Add Audio

1. Select `FlashbangProjectilePun`
2. **Add Component** → **Audio Source**

3. Configure Audio Source:
   - **Audio Clip**: Leave empty (controlled by script)
   - **Play On Awake**: ❌ **Unchecked**
   - **Loop**: ❌ **Unchecked**
   - **Spatial Blend**: `1` (fully 3D)
   - **Volume**: `1`
   - **Min Distance**: `1`
   - **Max Distance**: `50`

4. **Find or import audio:**
   - You need 2 sounds:
     - **Bang Sound**: A loud explosion/flashbang sound
     - **Pin Sound** (optional): A pin pulling sound

5. **Import your audio files** into `Assets/Audio/` (create folder if needed)

6. Select `FlashbangProjectilePun`
7. In the **FlashbangGrenade** script:
   - Drag your **bang sound** into the **Bang Sound** field
   - Drag your **pin sound** into the **Pin Sound** field (optional)

### Step 9: Save as Prefab (IMPORTANT!)

1. Create folder: `Assets/Resources/` (if it doesn't exist)
   - Right-click in Project → Create → Folder → Name it "Resources"

2. **Drag** `FlashbangProjectilePun` from **Hierarchy** into `Assets/Resources/`

3. This creates a prefab that Photon can spawn over the network

4. **Delete** `FlashbangProjectilePun` from the Hierarchy (the prefab is saved)

---

## Part 2: Add FlashbangEffect to Your Player

### Step 1: Find Your Player Prefab

1. Navigate to your player prefab location
   - Likely in: `Assets/Prefabs/` or similar
   - Look for your character controller prefab (the one players spawn as)

2. **Double-click** the player prefab to open it in Prefab Mode

### Step 2: Add the FlashbangEffect Script

1. In the Prefab Mode Hierarchy, find your **Camera**
   - Usually named: `Camera`, `First Person Camera`, or `Character Camera`

2. Select the Camera

3. **Add Component** → Search for **"Flashbang Effect"**

4. Click it to add the `FlashbangEffect` script

### Step 3: Configure FlashbangEffect

The script will auto-create the UI, but you can configure these settings:

- **Flash Color**: `White (255, 255, 255, 255)`
- **Fade Curve**: Leave as default (smooth fade)
- **Enable Ringing Sound**: ✅ **Checked** (if you want ear ringing)
- **Ringing Sound**: Drag an ear ringing audio clip here (optional)
- **Audio Muffle Amount**: `0.7` (how much to reduce game sounds)

### Step 4: Save the Player Prefab

1. At the top of the Hierarchy, click **"< Auto Save"** or **Save**
2. Exit Prefab Mode
3. The player is now ready to receive flashbang effects!

---

## Part 3: Create the Throwable Flashbang Item (Like Frag Grenade)

Now we need to make it so players can actually **throw** the flashbang using Opsive's throwable system.

### Method 1: Duplicate the Frag Grenade (Easiest)

#### Step 1: Duplicate Frag Grenade Weapon Prefabs

1. Navigate to:
   - `Assets/Opsive/.../PhotonPUN/Demo/Prefabs/Items/FragGrenadeWeaponRightPun.prefab`

2. **Right-click** on `FragGrenadeWeaponRightPun.prefab` → **Duplicate**

3. Rename the duplicate to: `FlashbangWeaponRightPun`

4. **Repeat** for `FragGrenadeWeaponLeftPun.prefab`:
   - Duplicate it
   - Rename to: `FlashbangWeaponLeftPun`

#### Step 2: Edit the Right Hand Flashbang

1. **Double-click** `FlashbangWeaponRightPun` to open in Prefab Mode

2. **Replace the visual model:**
   - In the Hierarchy, find the child that has the frag grenade model
   - Delete it or disable it
   - Drag `SM_Wep_Flashbang_01` from your Synty assets as a child
   - Position/rotate it so it looks right in the hand

3. **Find the ThrowableItem component** (should be on the root):
   - Look for a field called **"Throwable Prefab"** or **"Projectile"**
   - Drag your `FlashbangProjectilePun` prefab from `Assets/Resources/` into this field

4. **Configure throw settings** (optional):
   - **Throw Force**: `15` (adjust for throw strength)
   - **Throw Angle**: `0` (adjust for arc)

5. **Save** the prefab and exit Prefab Mode

#### Step 3: Edit the Left Hand Flashbang

1. Repeat Step 2 for `FlashbangWeaponLeftPun`
2. Make sure the model is positioned correctly for the left hand

#### Step 4: Create Item Definitions

Opsive uses "Item Definitions" to identify items in the inventory.

1. Navigate to: `Assets/Opsive/.../Demo/Inventory/`

2. **Right-click** in the folder → **Create** → **Ultimate Character Controller** → **Inventory** → **Item Definition**

3. Name it: `Flashbang`

4. Select the `Flashbang` Item Definition

5. In the Inspector:
   - **Name**: `Flashbang`
   - **Item ID**: `2001` (pick a unique number not used by other items)
   - **Description**: `Non-lethal stun grenade`
   - **Icon**: Drag a flashbang icon image if you have one

#### Step 5: Assign Item Definition to Weapon Prefabs

1. Open `FlashbangWeaponRightPun` in Prefab Mode

2. Select the root object

3. Find the **Item** component

4. In the **Item Definition** field:
   - Drag your newly created `Flashbang` Item Definition

5. **Save** and exit

6. **Repeat** for `FlashbangWeaponLeftPun`

#### Step 6: Add to Item Collection

1. Navigate to:
   - `Assets/Opsive/.../PhotonPUN/Demo/Inventory/PunItemCollection.asset`

2. **Select** `PunItemCollection`

3. In the Inspector, you'll see a list of items

4. **Increase the size** by 1

5. In the new slot:
   - Drag your `Flashbang` Item Definition

6. **Save** (Ctrl+S)

---

## Part 4: Give Players Flashbangs

### Option A: Start with Flashbangs in Inventory

1. Find your **player prefab**

2. Find the **Inventory** component on the player

3. Look for **Default Loadout** or **Starting Items**

4. Add an entry:
   - **Item Definition**: `Flashbang`
   - **Amount**: `3` (number of flashbangs)

5. Save the player prefab

### Option B: Create Flashbang Pickups

1. Navigate to: `Assets/Opsive/.../Demo/Prefabs/Items/Drops/`

2. Duplicate one of the existing drop prefabs (like `FragGrenadeDrop`)

3. Rename to: `FlashbangDropPun`

4. Open it in Prefab Mode

5. Replace the model with a flashbang model

6. Find the **Item Pickup** component:
   - **Item Definition**: Drag the `Flashbang` Item Definition
   - **Amount**: `1`

7. Save prefab

8. **Place in scene**: Drag `FlashbangDropPun` into your level where you want pickups

---

## Part 5: Testing

### Solo Test

1. **Play the scene**

2. Your character should spawn with flashbangs (if you set up default loadout)

3. **Switch to flashbang**: Press the grenade/throwable key (usually `4` or `G`)

4. **Throw**: Press Fire button (Left Mouse)

5. The flashbang should:
   - Be thrown forward
   - Detonate after 1.5 seconds
   - Flash your screen white
   - Muffle audio

### Multiplayer Test

1. **File** → **Build Settings**

2. Make sure your scene is added

3. **Build and Run** (creates a standalone build)

4. Run the build (Player 1)

5. Run Unity Editor Play mode (Player 2)

6. Both should connect to the same room

7. Throw flashbang at the other player

8. Should work over network!

---

## Troubleshooting

### Flashbang doesn't throw
- Make sure you assigned the `FlashbangProjectilePun` to the ThrowableItem component
- Check that you're equipping the flashbang weapon (press the correct key)
- Check Console for errors

### Flashbang doesn't spawn in multiplayer
- Make sure `FlashbangProjectilePun` is in `Assets/Resources/` folder
- Make sure it has a PhotonView component
- Check Photon is connected

### Screen doesn't flash
- Make sure `FlashbangEffect` is on your player's Camera
- Make sure the player is within 10 meters
- Check Console for "Flashing [name]" messages
- Make sure no walls are blocking (line of sight check)

### Can't equip flashbang
- Make sure you added it to the player's default loadout or picked up a drop
- Make sure Item Definition ID is unique
- Make sure Item Definition is in the PunItemCollection

---

## Advanced: Animation Setup (Optional)

If you want proper throwing animations:

1. Open `FlashbangWeaponRightPun` in Prefab Mode

2. Find the **Animator** component or **Item Perspective Properties**

3. Assign animations:
   - **Equip Animation**: Use grenade equip animation
   - **Throw Animation**: Use grenade throw animation
   - **Idle Animation**: Use grenade idle animation

4. Opsive has animations in:
   - `Assets/Opsive/.../Demo/Animations/Items/Throwable/`

---

## Tips

- **Adjust fuse time** to make it cook faster/slower
- **Adjust effect radius** to make it affect a bigger/smaller area
- **Adjust flash duration** to make it more/less punishing
- **Add more flashbangs** by increasing the amount in inventory

You're all set! Enjoy your tactical flashbangs! 🎮
