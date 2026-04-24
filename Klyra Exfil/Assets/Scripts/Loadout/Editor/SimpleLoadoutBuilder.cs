using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    public static class SimpleLoadoutBuilder
    {
        [MenuItem("Tools/Klyra/Create SIMPLE Loadout (WORKS)")]
        public static void CreateSimpleLoadout()
        {
            // Canvas
            var canvasGO = new GameObject("SimpleLoadoutCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Black background
            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.95f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Main panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(800, 600);

            // Title
            CreateText(panel.transform, "Title", "LOADOUT", 400, 250, 40);

            // Item name
            var itemName = CreateText(panel.transform, "ItemName", "Select an item", 400, 180, 24);

            // Amount text
            var amountText = CreateText(panel.transform, "AmountText", "0", 400, 100, 60);

            // Slider
            var sliderGO = UnityEngine.UI.DefaultControls.CreateSlider(new UnityEngine.UI.DefaultControls.Resources());
            sliderGO.name = "Slider";
            sliderGO.transform.SetParent(panel.transform, false);
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchoredPosition = new Vector2(400, 20);
            sliderRT.sizeDelta = new Vector2(600, 40);
            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;

            // Add button
            var addBtn = CreateBigButton(panel.transform, "AddBtn", "+", 600, -80, new Color(0.2f, 0.8f, 0.2f, 1f));

            // Subtract button
            var subBtn = CreateBigButton(panel.transform, "SubBtn", "-", 200, -80, new Color(0.8f, 0.2f, 0.2f, 1f));

            // Save button
            var saveBtn = CreateBigButton(panel.transform, "SaveBtn", "SAVE", 600, -200, new Color(0.3f, 0.6f, 0.3f, 1f));

            // Close button
            var closeBtn = CreateBigButton(panel.transform, "CloseBtn", "CLOSE", 200, -200, new Color(0.6f, 0.3f, 0.3f, 1f));

            // Add the UI script
            var ui = canvasGO.AddComponent<SimpleLoadoutUI>();
            ui.panel = panel;
            ui.itemNameText = itemName;
            ui.amountText = amountText;
            ui.amountSlider = slider;
            ui.addButton = addBtn;
            ui.subtractButton = subBtn;
            ui.saveButton = saveBtn;
            ui.closeButton = closeBtn;

            panel.SetActive(false);

            Selection.activeGameObject = canvasGO;
            Debug.Log("<color=green>SIMPLE loadout created! This one ACTUALLY WORKS.</color>");
        }

        static Text CreateText(Transform parent, string name, string content, float x, float y, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(600, size + 20);

            return text;
        }

        static Button CreateBigButton(Transform parent, string name, string label, float x, float y, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(300, 100);

            // Button text
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
