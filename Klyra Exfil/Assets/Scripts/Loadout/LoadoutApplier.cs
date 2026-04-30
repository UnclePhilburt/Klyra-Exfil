using System.Collections;
using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;

namespace Klyra.Loadout
{
    /// <summary>
    /// Put this on the player prefab. On Start, reads the saved loadout from
    /// LoadoutManager and adds those items to the UCC inventory. Only applies
    /// for the local client (Photon) so teammates don't overwrite each other.
    /// </summary>
    public class LoadoutApplier : MonoBehaviour
    {
        [Tooltip("If true, wipe any DefaultLoadout entries before applying the saved loadout.")]
        public bool clearExistingLoadout = true;

        private bool isInitialSpawn = true;

        private IEnumerator Start()
        {
            // Only the owning client applies its own loadout.
            var pv = GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && !pv.IsMine) yield break;

            // CRITICAL: Let UCC's LoadDefaultLoadout run FIRST.
            // Wait for UCC to FULLY initialize and load its default loadout.
            // This ensures all character references, object pools, and systems are ready.
            Debug.Log("[LoadoutApplier] Waiting for UCC to load default loadout...");
            yield return new WaitForSeconds(2f);

            Debug.Log("[LoadoutApplier] Starting loadout application after UCC initialization");
            Apply();
            isInitialSpawn = false;
        }

        /// <summary>
        /// Wipes the current inventory and applies the saved loadout from
        /// LoadoutManager. Safe to call at any time — e.g., from the Save
        /// button so changes take effect without needing a respawn.
        /// </summary>
        public void Apply()
        {
            StartCoroutine(ApplyCoroutine());
        }

        private IEnumerator ApplyCoroutine()
        {
            var manager = LoadoutManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("LoadoutApplier: no LoadoutManager found.", this);
                yield break;
            }

            var inventory = ResolveInventory();
            if (inventory == null)
            {
                Debug.LogError("LoadoutApplier: player has no UCC InventoryBase component.", this);
                yield break;
            }

            // Don't clear on initial spawn - just add items on top of default loadout.
            // UCC's prefab should have an empty default loadout to avoid conflicts.
            if (!isInitialSpawn)
            {
                Debug.Log("[LoadoutApplier] Not initial spawn - clearing before applying new loadout");
                if (clearExistingLoadout)
                {
                    Debug.Log("[LoadoutApplier] Clearing existing inventory...");
                    FullyClearInventory(inventory);

                    // Wait for UCC to fully process the item removal
                    yield return null;
                    yield return null;
                    Debug.Log("[LoadoutApplier] Inventory cleared, ready to add new items");
                }
            }
            else
            {
                Debug.Log("[LoadoutApplier] Initial spawn - adding items to default loadout without clearing");

                // Check if there are existing items from UCC's default loadout
                var existingItems = inventory.GetAllItemIdentifiers();
                if (existingItems != null && existingItems.Count > 0)
                {
                    Debug.LogWarning($"[LoadoutApplier] Player prefab has {existingItems.Count} items in default loadout. These will persist alongside LoadoutManager items. Consider clearing the Inventory component's Default Loadout in the inspector.");
                }
            }

            // Prefer the snapshot (carry-over from last play session) if we have
            // one. Otherwise apply fresh loadout defaults.
            if (manager.HasSnapshot)
            {
                Debug.Log("LoadoutApplier: restoring from inventory snapshot.");
                ApplySnapshot(inventory, manager);
            }
            else
            {
                Debug.Log("LoadoutApplier: applying fresh loadout defaults.");
                ApplyLoadoutDefaults(inventory, manager);
            }
        }

        private void ApplySnapshot(InventoryBase inventory, LoadoutManager manager)
        {
            var snap = manager.Snapshot;
            bool firstWeapon = true;
            for (int i = 0; i < snap.itemNames.Count; i++)
            {
                var entry = manager.FindEntry(snap.itemNames[i]);
                if (entry == null) continue;
                int amt = snap.amounts[i];
                if (amt <= 0) continue;

                // Only force-equip the first actual WEAPON (Primary/Secondary category)
                bool shouldForceEquip = firstWeapon &&
                    (entry.category == LoadoutCategory.Primary ||
                     entry.category == LoadoutCategory.Secondary);

                AddToInventory(inventory, entry, amt, shouldForceEquip);

                if (shouldForceEquip) firstWeapon = false;
            }
        }

        private void ApplyLoadoutDefaults(InventoryBase inventory, LoadoutManager manager)
        {
            Debug.Log("[LoadoutApplier] ===== APPLYING LOADOUT DEFAULTS =====");
            bool firstWeapon = true;
            foreach (var slot in manager.AllSlots())
            {
                if (string.IsNullOrEmpty(slot.itemName) || slot.amount <= 0)
                {
                    Debug.Log($"[LoadoutApplier] Skipping empty slot: {slot.itemName}");
                    continue;
                }

                var entry = manager.FindEntry(slot.itemName);
                if (entry == null)
                {
                    Debug.LogWarning($"LoadoutApplier: item '{slot.itemName}' not found in LoadoutManager.availableItems.");
                    continue;
                }

                // Only force-equip the first actual WEAPON (Primary/Secondary category)
                // Never force-equip throwables, ammo, or tactical items
                bool shouldForceEquip = firstWeapon &&
                    (entry.category == LoadoutCategory.Primary ||
                     entry.category == LoadoutCategory.Secondary);

                Debug.Log($"[LoadoutApplier] Processing slot: {slot.itemName} | FirstWeapon: {firstWeapon} | ShouldForceEquip: {shouldForceEquip}");

                AddToInventory(inventory, entry, slot.amount, shouldForceEquip);

                if (shouldForceEquip)
                {
                    firstWeapon = false;
                    Debug.Log($"[LoadoutApplier] First weapon equipped: {slot.itemName}. Setting firstWeapon=false");
                }
            }
            Debug.Log("[LoadoutApplier] ===== LOADOUT APPLICATION COMPLETE =====");
        }

        private void AddToInventory(InventoryBase inventory, LoadoutItemEntry entry, int amount, bool forceEquip)
        {
            var id = entry.item.CreateItemIdentifier();

            Debug.Log($"[LoadoutApplier] Adding item: {entry.item.name} | Category: {entry.category} | Amount: {amount} | SpawnCharacterItem: {entry.spawnCharacterItem} | ForceEquip requested: {forceEquip}");

            if (entry.spawnCharacterItem)
            {
                // For throwables and tactical items, NEVER auto-equip them
                // Only weapons (Primary/Secondary) should be force-equipped
                bool shouldEquip = forceEquip &&
                    (entry.category == LoadoutCategory.Primary ||
                     entry.category == LoadoutCategory.Secondary);

                Debug.Log($"[LoadoutApplier] {entry.item.name}: SpawnCharacterItem=true, ShouldEquip={shouldEquip} (forceEquip={forceEquip} && category={entry.category})");

                try
                {
                    // Allow tactical items (flashbangs, grenades) to spawn as CharacterItems if spawnCharacterItem is true
                    bool shouldSpawnCharacterItem = entry.spawnCharacterItem;

                    if (shouldSpawnCharacterItem)
                    {
                        // Use PickupItem for items that need CharacterItem spawn (weapons, throwables)
                        Debug.Log($"[LoadoutApplier] Adding {entry.item.name} with PickupItem (spawnCharacterItem=true)");
                        bool immediatePickup = true;
                        inventory.PickupItem(id, -1, amount, immediatePickup, shouldEquip);
                    }
                    else
                    {
                        // Count-only items (ammo, consumables, breach charges)
                        Debug.Log($"[LoadoutApplier] Adding {entry.item.name} as count-only (spawnCharacterItem=false)");
                        inventory.AddItemIdentifierAmount(id, amount, false, -1);
                    }

                    Debug.Log($"[LoadoutApplier] {entry.item.name}: AddItemIdentifierAmount completed");

                    // If this is a weapon that should be equipped, try to equip it
                    if (shouldEquip)
                    {
                        Debug.Log($"[LoadoutApplier] Attempting to equip {entry.item.name} via event system");
                        // Use UCC's event system to equip item set 0 (default first weapon set)
                        Opsive.Shared.Events.EventHandler.ExecuteEvent(inventory.gameObject, "OnItemSetManagerTryEquipItemSet", 0, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LoadoutApplier] {entry.item.name}: FAILED! Error: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                // Pure count — no CharacterItem spawn. Used for ammo and for
                // items like breach charges that aren't held in hand.
                Debug.Log($"[LoadoutApplier] {entry.item.name}: Adding as pure count (no CharacterItem spawn)");
                inventory.AddItemIdentifierAmount(id, amount, false);
            }
        }

        /// <summary>
        /// UCC's RemoveAllItems only removes items with a CharacterItem prefab
        /// backing them (weapons, held throwables). Pure-count items (ammo,
        /// breach charges) are left in place. This wipes the held items and
        /// then zeroes every remaining identifier so the next Apply starts
        /// from a truly empty inventory.
        /// </summary>
        private void FullyClearInventory(InventoryBase inventory)
        {
            // First, remove pure-count items (ammo, consumables)
            var ids = inventory.GetAllItemIdentifiers();
            if (ids != null)
            {
                // Copy to array because RemoveItemIdentifierAmount mutates the list.
                var snapshot = ids.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    int amount = inventory.GetItemIdentifierAmount(snapshot[i]);
                    if (amount > 0)
                    {
                        inventory.RemoveItemIdentifierAmount(snapshot[i], amount);
                    }
                }
            }

            // Then remove all CharacterItems (weapons, throwables)
            // Use destroyImmediately=true to prevent event system issues
            inventory.RemoveAllItems(true);
        }

        private InventoryBase ResolveInventory()
        {
            var inv = GetComponent<InventoryBase>();
            if (inv == null) inv = GetComponentInChildren<InventoryBase>();
            if (inv == null) inv = GetComponentInParent<InventoryBase>();
            return inv;
        }

        /// <summary>
        /// Delayed spawn for tactical items (throwables) to avoid initialization issues.
        /// Waits for character and item systems to be fully initialized before spawning.
        /// </summary>
        private IEnumerator DelayedSpawnTacticalCharacterItem(InventoryBase inventory, Opsive.Shared.Inventory.IItemIdentifier id, LoadoutItemEntry entry)
        {
            Debug.Log($"[LoadoutApplier] Starting delayed CharacterItem spawn for {entry.item.name}");

            // Wait significantly longer for all systems to initialize
            yield return new WaitForSeconds(3f);

            try
            {
                Debug.Log($"[LoadoutApplier] Attempting to spawn CharacterItem for {entry.item.name} after 3 second delay");

                // Try to spawn the CharacterItem now that everything should be initialized
                inventory.AddItemIdentifierAmount(id, 0, true, -1);

                Debug.Log($"[LoadoutApplier] SUCCESS! CharacterItem for {entry.item.name} spawned after delay");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadoutApplier] Delayed spawn FAILED for {entry.item.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// OLD VERSION - Delayed spawn for tactical items (throwables) to avoid initialization issues.
        /// Waits for character and item systems to be fully initialized before spawning.
        /// </summary>
        public IEnumerator DelayedSpawnTacticalItem(InventoryBase inventory, Opsive.Shared.Inventory.IItemIdentifier id, int amount)
        {
            // Wait for character to be fully initialized
            Debug.Log($"[LoadoutApplier] Delaying tactical item spawn for {id.GetItemDefinition().name}...");

            // Wait several frames for UCC to fully initialize
            yield return new WaitForSeconds(2f);

            try
            {
                // Now spawn the CharacterItem with throwable
                Debug.Log($"[LoadoutApplier] Spawning throwable CharacterItem for {id.GetItemDefinition().name} after delay");

                // DIFFERENT APPROACH: Try adding with spawnCharacterItems parameter explicitly
                // This might bypass some initialization issues
                inventory.AddItemIdentifierAmount(id, amount, true, -1);

                Debug.Log($"[LoadoutApplier] SUCCESS! {id.GetItemDefinition().name} spawned and ready to throw!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadoutApplier] Delayed spawn FAILED for {id.GetItemDefinition().name}: {e.Message}\n{e.StackTrace}");
                Debug.LogError($"[LoadoutApplier] Falling back to non-spawnable mode (like breach charges)");

                // Fallback: add as pure count if CharacterItem spawn fails
                try
                {
                    inventory.AddItemIdentifierAmount(id, amount, false);
                    Debug.LogWarning($"[LoadoutApplier] {id.GetItemDefinition().name} added as consumable count (won't be visible on character)");
                }
                catch
                {
                    Debug.LogError($"[LoadoutApplier] Complete failure adding {id.GetItemDefinition().name}");
                }
            }
        }

        private void OnDestroy()
        {
            // Snapshot the player's current inventory so their ammo/gear
            // carries into the next scene. Save is destructive — next scene's
            // LoadoutApplier will restore from this.
            var manager = LoadoutManager.Instance;
            if (manager == null) return;
            var pv = GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && !pv.IsMine) return;
            var inventory = ResolveInventory();
            if (inventory == null) return;

            var snap = new InventorySnapshot();
            var ids = inventory.GetAllItemIdentifiers();
            if (ids == null) return;
            foreach (var id in ids)
            {
                var def = id.GetItemDefinition();
                if (def == null) continue;
                int amount = inventory.GetItemIdentifierAmount(id);
                if (amount <= 0) continue;
                snap.itemNames.Add(def.name);
                snap.amounts.Add(amount);
            }
            manager.SaveSnapshot(snap);
            Debug.Log($"LoadoutApplier: snapshot saved ({snap.itemNames.Count} items).");
        }

        /// <summary>
        /// Resupply: wipes current inventory, applies loadout defaults, clears
        /// snapshot. Called from the loadout UI Save button.
        /// </summary>
        public void Resupply()
        {
            StartCoroutine(ResupplyCoroutine());
        }

        private IEnumerator ResupplyCoroutine()
        {
            var manager = LoadoutManager.Instance;
            if (manager == null) yield break;
            var inventory = ResolveInventory();
            if (inventory == null) yield break;

            manager.ClearSnapshot();

            if (clearExistingLoadout)
            {
                FullyClearInventory(inventory);

                // CRITICAL: Wait for UCC to process item removal before adding new items
                yield return null;
                yield return null;
            }

            ApplyLoadoutDefaults(inventory, manager);
            Debug.Log("LoadoutApplier: resupplied to loadout defaults.");
        }

        /// <summary>
        /// Finds the local player (the one owning this client) and applies
        /// the loadout to them. Used by the Save button in LoadoutUI.
        /// </summary>
        public static void ApplyToLocalPlayer()
        {
            var appliers = Object.FindObjectsOfType<LoadoutApplier>();
            for (int i = 0; i < appliers.Length; i++)
            {
                var pv = appliers[i].GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && !pv.IsMine) continue;
                appliers[i].Apply();
                return;
            }
            Debug.LogWarning("LoadoutApplier.ApplyToLocalPlayer: no local player with a LoadoutApplier was found in the scene.");
        }

        public static void ResupplyLocalPlayer()
        {
            var appliers = Object.FindObjectsOfType<LoadoutApplier>();
            for (int i = 0; i < appliers.Length; i++)
            {
                var pv = appliers[i].GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && !pv.IsMine) continue;
                appliers[i].Resupply();
                return;
            }
            Debug.LogWarning("LoadoutApplier.ResupplyLocalPlayer: no local player found.");
        }
    }
}
