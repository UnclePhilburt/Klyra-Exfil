using System;
using System.Collections.Generic;
using UnityEngine;
using Opsive.Shared.Inventory;

namespace Klyra.Loadout
{
    [Serializable]
    public class InventorySnapshot
    {
        public List<string> itemNames = new List<string>();
        public List<int> amounts = new List<int>();
    }

    public enum LoadoutCategory
    {
        Any,        // shows up in every slot
        Primary,    // primary weapon slot only
        Secondary,  // secondary weapon slot only
        Ammo,       // primary/secondary ammo slots
        Tactical    // tactical 1/2 slots (throwables)
    }

    [Serializable]
    public class LoadoutItemEntry
    {
        public ItemDefinitionBase item;
        [Tooltip("Which loadout slot(s) this item can be chosen for.")]
        public LoadoutCategory category = LoadoutCategory.Any;
        [Tooltip("Weight of a single unit of this item (kg). Multiplied by the picked amount.")]
        public float weightPerUnit = 1f;
        [Tooltip("Spawn a UCC CharacterItem prefab for this item (weapons / held throwables). Uncheck for pure-count items (ammo, breach charges) that aren't held in hand.")]
        public bool spawnCharacterItem = true;
    }

    /// <summary>
    /// Persistent, scene-spanning loadout store. Created once in the Station
    /// scene and survives into mission scenes via DontDestroyOnLoad.
    /// </summary>
    public class LoadoutManager : MonoBehaviour
    {
        private const string PrefsKey = "KlyraLoadout";
        private const string SnapshotPrefsKey = "KlyraLoadoutSnapshot";
        private const string CharacterPrefsKey = "KlyraCharacter";

        [Header("Characters")]
        [Tooltip("Names of character prefabs in Resources/ that the player can choose between. First entry is the default.")]
        public List<string> availableCharacters = new List<string> { "Swat" };

        [Header("Carry")]
        [Tooltip("Max total weight the player can carry (kg).")]
        public float maxWeight = 20f;

        [Tooltip("All items the player can select in the loadout UI, plus their per-unit weight.")]
        public List<LoadoutItemEntry> availableItems = new List<LoadoutItemEntry>();

        public LoadoutData Current { get; private set; } = new LoadoutData();

        /// <summary>
        /// Prefab name (in Resources/) of the character the player has chosen.
        /// Falls back to the first entry in availableCharacters when unset.
        /// </summary>
        public string CurrentCharacter
        {
            get
            {
                string saved = PlayerPrefs.GetString(CharacterPrefsKey, string.Empty);
                if (!string.IsNullOrEmpty(saved)) return saved;
                return availableCharacters != null && availableCharacters.Count > 0
                    ? availableCharacters[0]
                    : null;
            }
            set
            {
                PlayerPrefs.SetString(CharacterPrefsKey, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }
        public InventorySnapshot Snapshot { get; private set; }

        public bool HasSnapshot => Snapshot != null && Snapshot.itemNames.Count > 0;

        public static LoadoutManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            Current = string.IsNullOrEmpty(json)
                ? new LoadoutData()
                : JsonUtility.FromJson<LoadoutData>(json);

            string snapJson = PlayerPrefs.GetString(SnapshotPrefsKey, string.Empty);
            Snapshot = string.IsNullOrEmpty(snapJson)
                ? null
                : JsonUtility.FromJson<InventorySnapshot>(snapJson);
        }

        public void SaveSnapshot(InventorySnapshot snap)
        {
            Snapshot = snap;
            PlayerPrefs.SetString(SnapshotPrefsKey, JsonUtility.ToJson(snap));

            // Reflect the actual inventory state back into the loadout data so
            // the loadout screen shows what the player CURRENTLY has (post-raid
            // depleted counts, not the original picks).
            SyncLoadoutFromSnapshot();

            PlayerPrefs.Save();
        }

        private void SyncLoadoutFromSnapshot()
        {
            if (Snapshot == null) return;
            foreach (var slot in AllSlots())
            {
                if (slot == null || string.IsNullOrEmpty(slot.itemName)) continue;
                int idx = Snapshot.itemNames.IndexOf(slot.itemName);
                slot.amount = idx >= 0 ? Snapshot.amounts[idx] : 0;
            }
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
        }

        public void ClearSnapshot()
        {
            Snapshot = null;
            PlayerPrefs.DeleteKey(SnapshotPrefsKey);
            PlayerPrefs.Save();
        }

        public ItemDefinitionBase FindItem(string itemName)
        {
            var entry = FindEntry(itemName);
            return entry != null ? entry.item : null;
        }

        public LoadoutItemEntry FindEntry(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            for (int i = 0; i < availableItems.Count; i++)
            {
                var e = availableItems[i];
                if (e != null && e.item != null && e.item.name == itemName) return e;
            }
            return null;
        }

        public float WeightOf(string itemName, int amount)
        {
            if (amount <= 0) return 0f;
            var entry = FindEntry(itemName);
            return entry != null ? entry.weightPerUnit * amount : 0f;
        }

        public float CurrentWeight()
        {
            float total = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot == null || string.IsNullOrEmpty(slot.itemName)) continue;
                int amount = Mathf.Max(1, slot.amount); // weapons stored as 1
                total += WeightOf(slot.itemName, amount);
            }
            return total;
        }

        public float RemainingCapacity()
        {
            return Mathf.Max(0f, maxWeight - CurrentWeight());
        }

        public IEnumerable<LoadoutSlot> AllSlots()
        {
            yield return Current.primary;
            yield return Current.secondary;
            yield return Current.throwable1;
            yield return Current.throwable2;
            yield return Current.primaryAmmo;
            yield return Current.secondaryAmmo;
        }
    }
}
