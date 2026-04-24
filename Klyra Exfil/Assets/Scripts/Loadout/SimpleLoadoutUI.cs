using UnityEngine;
using UnityEngine.UI;
using Opsive.Shared.Inventory;

namespace Klyra.Loadout
{
    /// <summary>
    /// DEAD SIMPLE loadout UI - just works.
    /// </summary>
    public class SimpleLoadoutUI : MonoBehaviour
    {
        [Header("UI References - assign from inspector")]
        public GameObject panel;
        public Text itemNameText;
        public Text amountText;
        public Slider amountSlider;
        public Button addButton;
        public Button subtractButton;
        public Button saveButton;
        public Button closeButton;

        private ItemDefinitionBase currentItem;
        private int currentAmount = 0;

        void Start()
        {
            // Wire up buttons
            if (addButton != null)
                addButton.onClick.AddListener(() => ChangeAmount(1));

            if (subtractButton != null)
                subtractButton.onClick.AddListener(() => ChangeAmount(-1));

            if (amountSlider != null)
                amountSlider.onValueChanged.AddListener(OnSliderChanged);

            if (saveButton != null)
                saveButton.onClick.AddListener(Save);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (panel != null)
                panel.SetActive(false);
        }

        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void ChangeAmount(int delta)
        {
            currentAmount = Mathf.Max(0, currentAmount + delta);
            UpdateDisplay();
        }

        void OnSliderChanged(float value)
        {
            currentAmount = (int)value;
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (amountText != null)
                amountText.text = currentAmount.ToString();

            if (amountSlider != null)
                amountSlider.SetValueWithoutNotify(currentAmount);
        }

        void Save()
        {
            Debug.Log($"Saved amount: {currentAmount}");
            // Add your save logic here
        }
    }
}
