using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Opsive.UltimateCharacterController.Inventory;

namespace Klyra.Loadout
{
    /// <summary>
    /// Press Tab to peek at the local player's current inventory. Works in
    /// any scene — the Canvas survives scene loads via DontDestroyOnLoad.
    /// Populated from the UCC InventoryBase on the local player each time
    /// it opens, so counts are always live.
    /// </summary>
    public class InventoryScreen : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public RectTransform content;
        public GameObject rowTemplate;

        [Header("Input")]
        public Key toggleKey = Key.Tab;

        public Color rowBG = new Color(0.13f, 0.14f, 0.16f, 1f);
        public Color textColor = new Color(0.92f, 0.93f, 0.95f, 1f);
        public Color amountColor = new Color(0.98f, 0.78f, 0.30f, 1f);

        private static InventoryScreen Instance;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (panel != null) panel.SetActive(false);
            if (rowTemplate != null) rowTemplate.SetActive(false);
            Debug.Log($"[InventoryScreen] Awake on GameObject='{gameObject.name}' in scene='{gameObject.scene.name}' — toggle key = {toggleKey}. Panel assigned? {panel != null}", this);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb[toggleKey].wasPressedThisFrame)
            {
                Debug.Log($"[InventoryScreen] {toggleKey} pressed — toggling.");
                Toggle();
            }
        }

        public void Toggle()
        {
            if (panel == null)
            {
                Debug.LogError($"[InventoryScreen] 'panel' null on GameObject='{gameObject.name}' in scene='{gameObject.scene.name}'. Click this log to highlight it.", this);
                return;
            }
            bool open = !panel.activeSelf;
            panel.SetActive(open);
            Debug.Log($"[InventoryScreen] panel='{panel.name}' → activeSelf={panel.activeSelf} activeInHierarchy={panel.activeInHierarchy}");
            if (open) Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < spawnedRows.Count; i++) Destroy(spawnedRows[i]);
            spawnedRows.Clear();

            var inventory = FindLocalInventory();
            if (inventory == null || content == null || rowTemplate == null) return;

            var ids = inventory.GetAllItemIdentifiers();
            if (ids == null) return;

            foreach (var id in ids)
            {
                var def = id.GetItemDefinition();
                if (def == null) continue;
                int amount = inventory.GetItemIdentifierAmount(id);
                if (amount <= 0) continue;

                var row = Instantiate(rowTemplate, content);
                row.name = $"Row_{def.name}";
                row.SetActive(true);
                spawnedRows.Add(row);

                var bg = row.GetComponent<Image>();
                if (bg != null) bg.color = rowBG;

                var nameText = row.transform.Find("Name")?.GetComponent<Text>();
                if (nameText != null)
                {
                    nameText.text = def.name.ToUpperInvariant();
                    nameText.color = textColor;
                }

                var countText = row.transform.Find("Count")?.GetComponent<Text>();
                if (countText != null)
                {
                    countText.text = amount.ToString();
                    countText.color = amountColor;
                }
            }
        }

        private InventoryBase FindLocalInventory()
        {
            var appliers = Object.FindObjectsOfType<LoadoutApplier>();
            for (int i = 0; i < appliers.Length; i++)
            {
                var pv = appliers[i].GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && !pv.IsMine) continue;
                var inv = appliers[i].GetComponent<InventoryBase>()
                       ?? appliers[i].GetComponentInChildren<InventoryBase>()
                       ?? appliers[i].GetComponentInParent<InventoryBase>();
                if (inv != null) return inv;
            }
            return null;
        }
    }
}
