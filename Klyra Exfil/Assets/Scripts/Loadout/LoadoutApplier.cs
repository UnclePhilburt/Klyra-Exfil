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

        private IEnumerator Start()
        {
            // Only the owning client applies its own loadout.
            var pv = GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && !pv.IsMine) yield break;

            // Wait a frame so UCC's inventory / item setup finishes before we
            // start adding items. Without this the CharacterItems may not be
            // registered yet and the equip event fires on nothing.
            yield return null;
            Apply();
        }

        /// <summary>
        /// Wipes the current inventory and applies the saved loadout from
        /// LoadoutManager. Safe to call at any time — e.g., from the Save
        /// button so changes take effect without needing a respawn.
        /// </summary>
        public void Apply()
        {
            var manager = LoadoutManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("LoadoutApplier: no LoadoutManager found.", this);
                return;
            }

            var inventory = ResolveInventory();
            if (inventory == null)
            {
                Debug.LogError("LoadoutApplier: player has no UCC InventoryBase component.", this);
                return;
            }

            if (clearExistingLoadout)
            {
                FullyClearInventory(inventory);
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
            bool first = true;
            for (int i = 0; i < snap.itemNames.Count; i++)
            {
                var entry = manager.FindEntry(snap.itemNames[i]);
                if (entry == null) continue;
                int amt = snap.amounts[i];
                if (amt <= 0) continue;
                AddToInventory(inventory, entry, amt, first);
                first = false;
            }
        }

        private void ApplyLoadoutDefaults(InventoryBase inventory, LoadoutManager manager)
        {
            bool first = true;
            foreach (var slot in manager.AllSlots())
            {
                if (string.IsNullOrEmpty(slot.itemName) || slot.amount <= 0) continue;
                var entry = manager.FindEntry(slot.itemName);
                if (entry == null)
                {
                    Debug.LogWarning($"LoadoutApplier: item '{slot.itemName}' not found in LoadoutManager.availableItems.");
                    continue;
                }
                AddToInventory(inventory, entry, slot.amount, first);
                first = false;
            }
        }

        private void AddToInventory(InventoryBase inventory, LoadoutItemEntry entry, int amount, bool forceEquip)
        {
            var id = entry.item.CreateItemIdentifier();
            if (entry.spawnCharacterItem)
            {
                // Full pickup chain — spawns the CharacterItem and fires the
                // equip event. Required for weapons and held throwables.
                inventory.PickupItem(id, -1, amount, true, forceEquip);
            }
            else
            {
                // Pure count — no CharacterItem spawn. Used for ammo and for
                // items like breach charges that aren't held in hand.
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
            inventory.RemoveAllItems(false);

            var ids = inventory.GetAllItemIdentifiers();
            if (ids == null) return;

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

        private InventoryBase ResolveInventory()
        {
            var inv = GetComponent<InventoryBase>();
            if (inv == null) inv = GetComponentInChildren<InventoryBase>();
            if (inv == null) inv = GetComponentInParent<InventoryBase>();
            return inv;
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
            var manager = LoadoutManager.Instance;
            if (manager == null) return;
            var inventory = ResolveInventory();
            if (inventory == null) return;

            manager.ClearSnapshot();
            if (clearExistingLoadout) FullyClearInventory(inventory);
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
