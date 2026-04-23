# Mission Selection Table Setup

This guide shows you how to set up the mission selection table in your police station lobby.

---

## Quick Setup

### Step 1: Add the Table Object

1. **In your police station scene**, find or place a table GameObject where you want the mission table
   - You can use any table model from your Synty assets

2. **Select the table** in the Hierarchy

3. **Add Component** → Search for **`MissionTable`**

4. The table is now interactive!

### Step 2: Configure the Mission Table

With the table selected, you'll see the MissionTable script settings:

**Interaction Settings:**
- **Interact Key**: `E` (default - press E to interact)
- **Interaction Distance**: `3` (how close player must be)
- **Show Prompt**: ✅ Checked (shows "Press E" message)

**Missions:**
- **Size**: `1` (we'll add one mission for now)

Click the arrow to expand Mission 0:
- **Mission Name**: `Door Breach Training`
- **Scene Name**: Type the exact name of your door scene (find it in Build Settings)
- **Description**: `Practice breaching techniques`
- **Thumbnail**: Leave empty for now (optional mission image)

### Step 3: Add Your Scene to Build Settings

**IMPORTANT:** Unity can only load scenes that are in Build Settings!

1. **File** → **Build Settings**

2. **Click "Add Open Scenes"** if your door scene is currently open
   - OR drag your door scene from the Project window into the "Scenes in Build" list

3. **Note the scene name** - use this EXACT name in the "Scene Name" field

4. Click **Close**

### Step 4: Test It

1. **Press Play** ▶️

2. **Walk up to the table** (within 3 meters)

3. You should see: **"Press [E] to Select Mission"**

4. **Press E** - Mission selection menu appears!

5. **Click on the mission** - It loads the scene!

6. **Press ESC** to close the menu

---

## Adding More Missions

To add more missions:

1. Select the table

2. In the MissionTable script, change **Missions → Size** to however many missions you want

3. Fill out each mission:
   - Mission Name
   - Scene Name (must match Build Settings!)
   - Description

4. Make sure all scenes are added to Build Settings!

---

## Customization

### Change the Interaction Key

- Change **Interact Key** dropdown to any key you want

### Change Interaction Distance

- Increase/decrease **Interaction Distance** (in meters)

### Disable the Prompt

- Uncheck **Show Prompt** if you don't want the "Press E" text

### Add Audio (Optional)

1. Import sound effects into your project

2. Select the table

3. In MissionTable script:
   - **Open Menu Sound**: Drag a sound here
   - **Select Mission Sound**: Drag a sound here

### Custom UI (Advanced)

The script auto-creates a simple UI. If you want to customize it:

1. Create your own Canvas in the scene

2. Design your mission panel UI

3. Assign your custom UI elements to the MissionTable script fields:
   - **Mission Canvas**
   - **Mission Panel**
   - **Prompt Text**

---

## Multiplayer Notes

- In multiplayer, **only the Master Client** (host) can start missions
- The mission menu will show for everyone, but only the host can actually load the scene
- When the host selects a mission, everyone will load into it together

---

## Troubleshooting

**"Press E" doesn't show:**
- Make sure you're within interaction distance (check the cyan sphere in Scene view when table is selected)
- Make sure your player has the "Player" tag

**Menu doesn't open:**
- Check Console for errors
- Make sure the player object is found (has "Player" tag or PhotonView)

**Mission doesn't load:**
- Make sure the scene name EXACTLY matches the name in Build Settings
- Make sure the scene is added to Build Settings (File → Build Settings)
- Check Console for "Scene not found" errors

**Cursor stays locked:**
- The script should unlock the cursor automatically
- If not, make sure Cursor.lockState is being set in your player controller

---

## Example Setup

```
Police Station Scene
├── Environment
├── Lighting
├── Player
└── MissionTable (with MissionTable script)
    ├── Table Model (visual)
    └── Collider (for interaction detection)

Missions Array:
[0] Door Breach Training - "SampleScene"
[1] Hostage Rescue - "HostageScene" (when you make it)
[2] Drug Raid - "DrugRaidScene" (when you make it)
```

That's it! You now have a Ready or Not style mission selection table!
