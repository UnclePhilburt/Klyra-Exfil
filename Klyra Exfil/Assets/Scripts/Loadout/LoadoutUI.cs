using System;
using System.Collections.Generic;
using Opsive.Shared.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Klyra.Loadout
{
    /// <summary>
    /// Ready-or-Not style loadout screen: select a slot on the left, pick an
    /// item from the card grid on the right, adjust count for ammo/throwables.
    /// Carry weight is capped — the + button and item picks stop at the cap.
    /// </summary>
    public class LoadoutUI : MonoBehaviour
    {
        public enum SlotId
        {
            Primary,
            Secondary,
            PrimaryAmmo,
            SecondaryAmmo,
            Throwable1,
            Throwable2
        }

        [Serializable]
        public class SlotButtonRef
        {
            public SlotId id;
            public Button button;
            public Text slotLabel;
            public Text itemLabel;
            public Image accentBar;
        }

        [Header("Slot buttons (left column)")]
        public List<SlotButtonRef> slotButtons = new List<SlotButtonRef>();

        [Header("Item card grid (right column)")]
        public RectTransform gridContent;
        public GameObject itemCardTemplate;

        [Header("Amount controls (ammo / throwables only)")]
        public GameObject amountPanel;
        public Button amountMinus;
        public Button amountPlus;
        public Text amountValue;

        [Header("Weight bar")]
        public Text weightReadout;
        public Image weightFill;

        [Header("Character select")]
        public Dropdown characterDropdown;

        [Header("Buttons")]
        public Button saveButton;
        public Button closeButton;

        [Header("Colors")]
        public Color accentSelected = new Color(0.83f, 0.29f, 0.23f, 1f);
        public Color cardBG = new Color(0.09f, 0.10f, 0.12f, 1f);
        public Color cardBGSelected = new Color(0.17f, 0.19f, 0.22f, 1f);
        public Color cardTextColor = new Color(0.92f, 0.93f, 0.95f, 1f);
        public Color cardTextSubtle = new Color(0.55f, 0.58f, 0.62f, 1f);
        public Color weightOK = new Color(0.38f, 0.70f, 0.35f, 1f);
        public Color weightFull = new Color(0.83f, 0.29f, 0.23f, 1f);

        private SlotId selectedSlot = SlotId.Primary;
        private readonly List<GameObject> spawnedCards = new List<GameObject>();

        private void OnEnable()
        {
            if (LoadoutManager.Instance == null)
            {
                Debug.LogError("LoadoutUI: no LoadoutManager in scene.");
                return;
            }

            for (int i = 0; i < slotButtons.Count; i++)
            {
                var captured = slotButtons[i];
                captured.button.onClick.RemoveAllListeners();
                captured.button.onClick.AddListener(() => SelectSlot(captured.id));
            }

            // Use HoldRepeatButton so clicking OR holding works. We remove the
            // regular onClick listener because the hold component drives taps
            // via its own OnPointerDown — otherwise we'd double-fire on tap.
            if (amountMinus != null)
            {
                amountMinus.onClick.RemoveAllListeners();
                var repeat = amountMinus.GetComponent<HoldRepeatButton>()
                    ?? amountMinus.gameObject.AddComponent<HoldRepeatButton>();
                repeat.onRepeat = () => AdjustAmount(-1);
            }
            if (amountPlus != null)
            {
                amountPlus.onClick.RemoveAllListeners();
                var repeat = amountPlus.GetComponent<HoldRepeatButton>()
                    ?? amountPlus.gameObject.AddComponent<HoldRepeatButton>();
                repeat.onRepeat = () => AdjustAmount(+1);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(OnSave);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (itemCardTemplate != null) itemCardTemplate.SetActive(false);

            SetupCharacterDropdown();

            SelectSlot(selectedSlot);
            RefreshAllSlotLabels();
            RefreshWeightBar();
        }

        private void SetupCharacterDropdown()
        {
            if (characterDropdown == null) return;
            var mgr = LoadoutManager.Instance;

            characterDropdown.ClearOptions();
            if (mgr.availableCharacters != null && mgr.availableCharacters.Count > 0)
            {
                characterDropdown.AddOptions(mgr.availableCharacters);
                int idx = mgr.availableCharacters.IndexOf(mgr.CurrentCharacter);
                characterDropdown.value = idx >= 0 ? idx : 0;
                characterDropdown.RefreshShownValue();
            }

            characterDropdown.onValueChanged.RemoveAllListeners();
            characterDropdown.onValueChanged.AddListener(OnCharacterChanged);
        }

        private void OnCharacterChanged(int index)
        {
            var mgr = LoadoutManager.Instance;
            if (mgr.availableCharacters == null || index < 0 || index >= mgr.availableCharacters.Count) return;
            mgr.CurrentCharacter = mgr.availableCharacters[index];
            Debug.Log($"Character set to '{mgr.CurrentCharacter}'. Respawning now.");
            PlayerSpawner.RespawnLocalPlayer();
        }

        private void OnDisable() { ClearSpawnedCards(); }

        private bool SlotNeedsAmount(SlotId id)
        {
            return id == SlotId.PrimaryAmmo || id == SlotId.SecondaryAmmo
                || id == SlotId.Throwable1 || id == SlotId.Throwable2;
        }

        private LoadoutSlot GetSlot(SlotId id)
        {
            var d = LoadoutManager.Instance.Current;
            switch (id)
            {
                case SlotId.Primary: return d.primary;
                case SlotId.Secondary: return d.secondary;
                case SlotId.PrimaryAmmo: return d.primaryAmmo;
                case SlotId.SecondaryAmmo: return d.secondaryAmmo;
                case SlotId.Throwable1: return d.throwable1;
                case SlotId.Throwable2: return d.throwable2;
            }
            return null;
        }

        private void SelectSlot(SlotId id)
        {
            selectedSlot = id;
            for (int i = 0; i < slotButtons.Count; i++)
            {
                bool isSelected = slotButtons[i].id == id;
                if (slotButtons[i].accentBar != null)
                {
                    slotButtons[i].accentBar.color = isSelected
                        ? accentSelected
                        : new Color(accentSelected.r, accentSelected.g, accentSelected.b, 0f);
                }
            }
            if (amountPanel != null) amountPanel.SetActive(SlotNeedsAmount(id));
            RefreshAmountLabel();
            RebuildGrid();
        }

        private void AdjustAmount(int delta)
        {
            var slot = GetSlot(selectedSlot);
            if (slot == null || string.IsNullOrEmpty(slot.itemName))
            {
                Debug.Log("[LoadoutUI] AdjustAmount: no item picked for this slot.");
                return;
            }

            if (delta > 0)
            {
                // Only add if the weight would still fit under the cap.
                var entry = LoadoutManager.Instance.FindEntry(slot.itemName);
                float unit = entry != null ? entry.weightPerUnit : 0f;
                if (unit > 0f && LoadoutManager.Instance.RemainingCapacity() < unit)
                {
                    Debug.Log("[LoadoutUI] AdjustAmount: over weight cap, not adding.");
                    return;
                }
            }

            slot.amount = Mathf.Max(0, slot.amount + delta);
            RefreshAmountLabel();
            RefreshSlotLabel(selectedSlot);
            RefreshWeightBar();
        }

        private void RefreshAmountLabel()
        {
            var slot = GetSlot(selectedSlot);
            if (amountValue != null && slot != null) amountValue.text = slot.amount.ToString();
        }

        private void RefreshWeightBar()
        {
            var mgr = LoadoutManager.Instance;
            float cur = mgr.CurrentWeight();
            float max = mgr.maxWeight;
            if (weightReadout != null)
            {
                weightReadout.text = $"WEIGHT  {cur:F1} / {max:F1} KG";
            }
            if (weightFill != null)
            {
                float t = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
                weightFill.fillAmount = t;
                weightFill.color = t >= 1f ? weightFull : Color.Lerp(weightOK, weightFull, t * 0.8f);
            }
        }

        private void RebuildGrid()
        {
            ClearSpawnedCards();
            if (gridContent == null || itemCardTemplate == null) return;

            var currentItemName = GetSlot(selectedSlot)?.itemName ?? string.Empty;

            SpawnCard("(none)", "", string.IsNullOrEmpty(currentItemName), null, 0f);

            foreach (var entry in LoadoutManager.Instance.availableItems)
            {
                if (entry == null || entry.item == null) continue;
                if (!CategoryMatchesSlot(entry.category, selectedSlot)) continue;
                string sub = SafeCategoryName(entry.item);
                bool selected = entry.item.name == currentItemName;
                SpawnCard(entry.item.name, sub, selected, entry.item, entry.weightPerUnit);
            }
        }

        private void SpawnCard(string title, string subtitle, bool selected, ItemDefinitionBase item, float weight)
        {
            var card = Instantiate(itemCardTemplate, gridContent);
            card.name = $"Card_{title}";
            card.SetActive(true);
            spawnedCards.Add(card);

            var bg = card.GetComponent<Image>();
            if (bg != null) bg.color = selected ? cardBGSelected : cardBG;

            var titleText = card.transform.Find("Title")?.GetComponent<Text>();
            if (titleText != null) { titleText.text = title.ToUpperInvariant(); titleText.color = cardTextColor; }

            var subText = card.transform.Find("Subtitle")?.GetComponent<Text>();
            if (subText != null)
            {
                subText.color = cardTextSubtle;
                if (item == null) subText.text = "clear this slot";
                else subText.text = weight > 0f ? $"{subtitle}   •   {weight:F1} KG" : subtitle;
            }

            var btn = card.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string capturedName = item != null ? item.name : string.Empty;
                btn.onClick.AddListener(() => OnPickItem(capturedName));
            }
        }

        private void OnPickItem(string itemName)
        {
            var slot = GetSlot(selectedSlot);
            if (slot == null) return;

            // Swap the item; adjust amount so we don't exceed weight cap.
            var prevItem = slot.itemName;
            var prevAmount = slot.amount;
            slot.itemName = itemName;

            if (string.IsNullOrEmpty(itemName))
            {
                slot.amount = 0;
            }
            else
            {
                int desired = SlotNeedsAmount(selectedSlot) ? (prevAmount > 0 ? prevAmount : 30) : 1;
                slot.amount = ClampAmountToCapacity(itemName, desired);
                if (slot.amount == 0 && desired > 0)
                {
                    // Not enough room even for 1 unit — revert to previous.
                    Debug.Log($"[LoadoutUI] Not enough carry weight for {itemName}.");
                    slot.itemName = prevItem;
                    slot.amount = prevAmount;
                }
            }

            RefreshAmountLabel();
            RefreshAllSlotLabels();
            RefreshWeightBar();
            RebuildGrid();
        }

        private int ClampAmountToCapacity(string itemName, int desired)
        {
            var entry = LoadoutManager.Instance.FindEntry(itemName);
            if (entry == null || entry.weightPerUnit <= 0f) return desired;
            float remaining = LoadoutManager.Instance.RemainingCapacity();
            int maxByCap = Mathf.FloorToInt(remaining / entry.weightPerUnit);
            return Mathf.Max(0, Mathf.Min(desired, maxByCap));
        }

        private void ClearSpawnedCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++) Destroy(spawnedCards[i]);
            spawnedCards.Clear();
        }

        private void RefreshAllSlotLabels()
        {
            for (int i = 0; i < slotButtons.Count; i++) RefreshSlotLabel(slotButtons[i].id);
        }

        private void RefreshSlotLabel(SlotId id)
        {
            var entry = slotButtons.Find(s => s.id == id);
            if (entry == null) return;
            var slot = GetSlot(id);
            if (entry.slotLabel != null) entry.slotLabel.text = PrettySlotName(id).ToUpperInvariant();
            if (entry.itemLabel != null)
            {
                if (slot == null || string.IsNullOrEmpty(slot.itemName))
                {
                    entry.itemLabel.text = "— empty —";
                    entry.itemLabel.color = cardTextSubtle;
                }
                else
                {
                    string txt = slot.itemName.ToUpperInvariant();
                    if (SlotNeedsAmount(id)) txt += $"  ×{slot.amount}";
                    entry.itemLabel.text = txt;
                    entry.itemLabel.color = cardTextColor;
                }
            }
        }

        private static string PrettySlotName(SlotId id)
        {
            switch (id)
            {
                case SlotId.Primary: return "Primary";
                case SlotId.Secondary: return "Secondary";
                case SlotId.PrimaryAmmo: return "Primary Ammo";
                case SlotId.SecondaryAmmo: return "Secondary Ammo";
                case SlotId.Throwable1: return "Tactical 1";
                case SlotId.Throwable2: return "Tactical 2";
            }
            return id.ToString();
        }

        private static bool CategoryMatchesSlot(LoadoutCategory cat, SlotId slot)
        {
            if (cat == LoadoutCategory.Any) return true;
            switch (slot)
            {
                case SlotId.Primary:        return cat == LoadoutCategory.Primary;
                case SlotId.Secondary:      return cat == LoadoutCategory.Secondary;
                case SlotId.PrimaryAmmo:
                case SlotId.SecondaryAmmo:  return cat == LoadoutCategory.Ammo;
                case SlotId.Throwable1:
                case SlotId.Throwable2:     return cat == LoadoutCategory.Tactical;
            }
            return false;
        }

        private static string SafeCategoryName(ItemDefinitionBase item)
        {
            try
            {
                var cat = item.GetItemCategory();
                return cat != null ? cat.name.ToUpperInvariant() : string.Empty;
            }
            catch { return string.Empty; }
        }

        private void OnSave()
        {
            LoadoutManager.Instance.Save();
            // Save now also RESUPPLIES — clears the carry-over snapshot and
            // wipes + refills the player's inventory to the loadout defaults.
            LoadoutApplier.ResupplyLocalPlayer();
            Debug.Log("Loadout saved and resupplied.");
        }
    }
}
