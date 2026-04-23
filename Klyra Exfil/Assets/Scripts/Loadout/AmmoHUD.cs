using UnityEngine;
using UnityEngine.UI;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items;
using Opsive.UltimateCharacterController.Items.Actions;

namespace Klyra.Loadout
{
    /// <summary>
    /// Bottom-right HUD that reads the currently-equipped weapon's clip and
    /// reserve ammo from UCC and prints e.g. "24 / 120". Hides when no
    /// shootable weapon is equipped.
    /// </summary>
    public class AmmoHUD : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;          // wrapper hidden when no weapon is equipped
        public Text clipText;            // "24"
        public Text reserveText;         // "/ 120"
        public Text weaponNameText;      // "M4A1"

        private InventoryBase cachedInventory;

        private void Update()
        {
            var inventory = ResolveInventory();
            if (inventory == null)
            {
                Hide();
                return;
            }

            // Walk every slot; show the first active shootable we find.
            int slotCount = inventory.SlotCount;
            for (int i = 0; i < slotCount; i++)
            {
                var item = inventory.GetActiveCharacterItem(i);
                if (item == null) continue;

                var shootable = FindShootable(item);
                if (shootable != null)
                {
                    ShowShootable(shootable, item);
                    return;
                }
            }

            Hide();
        }

        private ShootableAction FindShootable(CharacterItem item)
        {
            var actions = item.ItemActions;
            if (actions == null) return null;
            for (int i = 0; i < actions.Length; i++)
            {
                var s = actions[i] as ShootableAction;
                if (s != null) return s;
            }
            return null;
        }

        private void ShowShootable(ShootableAction shootable, CharacterItem item)
        {
            if (root != null && !root.activeSelf) root.SetActive(true);
            if (clipText != null) clipText.text = shootable.ClipRemainingCount.ToString();
            if (reserveText != null) reserveText.text = $"/ {shootable.AmmoRemainingCount}";
            if (weaponNameText != null) weaponNameText.text = item.ItemDefinition != null
                ? item.ItemDefinition.name.ToUpperInvariant()
                : item.name.ToUpperInvariant();
        }

        private void Hide()
        {
            if (root != null && root.activeSelf) root.SetActive(false);
        }

        private InventoryBase ResolveInventory()
        {
            if (cachedInventory != null) return cachedInventory;

            // Find the local player — one owning a LoadoutApplier (which tags the local player in this project).
            var appliers = Object.FindObjectsOfType<LoadoutApplier>();
            for (int i = 0; i < appliers.Length; i++)
            {
                var pv = appliers[i].GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && !pv.IsMine) continue;
                var inv = appliers[i].GetComponent<InventoryBase>()
                       ?? appliers[i].GetComponentInChildren<InventoryBase>()
                       ?? appliers[i].GetComponentInParent<InventoryBase>();
                if (inv != null)
                {
                    cachedInventory = inv;
                    return inv;
                }
            }
            return null;
        }
    }
}
