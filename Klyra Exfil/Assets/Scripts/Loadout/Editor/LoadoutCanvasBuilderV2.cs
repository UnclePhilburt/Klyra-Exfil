using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    /// <summary>
    /// Completely rebuilt loadout canvas - clean, simple, actually works.
    /// </summary>
    public static class LoadoutCanvasBuilderV2
    {
        // Clean color palette
        private static readonly Color DarkBG = new Color(0.1f, 0.1f, 0.12f, 1f);
        private static readonly Color CardBG = new Color(0.15f, 0.15f, 0.17f, 1f);
        private static readonly Color AccentRed = new Color(0.85f, 0.3f, 0.25f, 1f);
        private static readonly Color AccentGreen = new Color(0.3f, 0.7f, 0.3f, 1f);
        private static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.6f, 1f);

        [MenuItem("Tools/Klyra/Create Loadout Canvas V2 (New)")]
        public static void CreateLoadoutCanvas()
        {
            LoadoutCanvasBuilder.EnsureEventSystem();

            // Main canvas
            var canvasGO = new GameObject("LoadoutCanvas_V2",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Loadout Canvas V2");

            // Dark background overlay
            var bg = CreateImage(canvasGO.transform, "Background", new Color(0, 0, 0, 0.9f));
            Stretch(bg);

            // Main panel container - full screen with small margin
            var panel = CreateImage(canvasGO.transform, "MainPanel", DarkBG);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = new Vector2(50, 50);
            panelRT.offsetMax = new Vector2(-50, -50);

            // === HEADER ===
            var header = CreateHeader(panel.transform, out Button saveBtn, out Button closeBtn, out Dropdown charDropdown);

            // === LEFT SIDE - SLOT LIST ===
            var leftPanel = CreateImage(panel.transform, "LeftPanel", CardBG);
            var leftRT = leftPanel.GetComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0, 0);
            leftRT.anchorMax = new Vector2(0, 1);
            leftRT.pivot = new Vector2(0, 0.5f);
            leftRT.anchoredPosition = new Vector2(20, -50);
            leftRT.sizeDelta = new Vector2(450, -120);

            var slotRefs = CreateSlotList(leftPanel.transform);

            // === RIGHT SIDE - ITEM GRID & AMOUNT ===
            var rightPanel = CreateImage(panel.transform, "RightPanel", CardBG);
            var rightRT = rightPanel.GetComponent<RectTransform>();
            rightRT.anchorMin = new Vector2(0, 0);
            rightRT.anchorMax = new Vector2(1, 1);
            rightRT.pivot = new Vector2(0, 0.5f);
            rightRT.anchoredPosition = new Vector2(490, -50);
            rightRT.sizeDelta = new Vector2(-510, -120);

            // Item grid at top
            var gridContent = CreateItemGrid(rightPanel.transform);
            var cardTemplate = CreateItemCard(gridContent);

            // Amount controls at bottom
            var amountPanel = CreateAmountControls(rightPanel.transform,
                out Slider slider, out InputField input, out Text amountText,
                out Button maxBtn, out Button clearBtn, out Button plus30, out Button plus60);

            // Weight bar
            var weightBar = CreateWeightBar(rightPanel.transform, out Image weightFill, out Text weightText);

            // === WIRE UP LoadoutUI ===
            var ui = canvasGO.AddComponent<LoadoutUI>();
            ui.gridContent = gridContent;
            ui.itemCardTemplate = cardTemplate;
            ui.amountPanel = amountPanel;
            ui.amountValue = amountText;
            ui.amountSlider = slider;
            ui.amountInput = input;
            ui.amountMax = maxBtn;
            ui.amountClear = clearBtn;
            ui.amountPlus30 = plus30;
            ui.amountPlus60 = plus60;
            ui.weightReadout = weightText;
            ui.weightFill = weightFill;
            ui.characterDropdown = charDropdown;
            ui.saveButton = saveBtn;
            ui.closeButton = closeBtn;
            ui.slotButtons.Clear();
            ui.slotButtons.AddRange(slotRefs);

            canvasGO.SetActive(false);
            Selection.activeGameObject = canvasGO;
            Debug.Log("<color=green>NEW Loadout Canvas V2 created! Clean, simple, actually works.</color>");
        }

        private static GameObject CreateHeader(Transform parent, out Button saveBtn, out Button closeBtn, out Dropdown dropdown)
        {
            var header = CreateImage(parent, "Header", new Color(0.08f, 0.08f, 0.1f, 1f));
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.sizeDelta = new Vector2(0, 80);
            hRT.anchoredPosition = Vector2.zero;

            // Title
            CreateText(header.transform, "Title", "LOADOUT", 36, FontStyle.Bold, TextAnchor.MiddleLeft, TextWhite,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(400, 60));

            // Close button
            closeBtn = CreateSimpleButton(header.transform, "CloseBtn", "X", AccentRed,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(60, 60));

            // Save button
            saveBtn = CreateSimpleButton(header.transform, "SaveBtn", "SAVE", AccentGreen,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-100, 0), new Vector2(140, 60));

            // Character dropdown
            var ddGO = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            ddGO.name = "CharacterDropdown";
            ddGO.transform.SetParent(header.transform, false);
            var ddRT = (RectTransform)ddGO.transform;
            ddRT.anchorMin = new Vector2(0.5f, 0.5f);
            ddRT.anchorMax = new Vector2(0.5f, 0.5f);
            ddRT.pivot = new Vector2(0.5f, 0.5f);
            ddRT.anchoredPosition = Vector2.zero;
            ddRT.sizeDelta = new Vector2(300, 50);
            dropdown = ddGO.GetComponent<Dropdown>();

            return header;
        }

        private static LoadoutUI.SlotButtonRef[] CreateSlotList(Transform parent)
        {
            var title = CreateText(parent, "Title", "GEAR SLOTS", 20, FontStyle.Bold, TextAnchor.UpperLeft, TextGray,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -20), new Vector2(-20, 40));

            var slots = new LoadoutUI.SlotButtonRef[6];
            slots[0] = CreateSlotButton(parent, LoadoutUI.SlotId.Primary, "PRIMARY WEAPON", 70);
            slots[1] = CreateSlotButton(parent, LoadoutUI.SlotId.Secondary, "SECONDARY WEAPON", 160);
            slots[2] = CreateSlotButton(parent, LoadoutUI.SlotId.PrimaryAmmo, "PRIMARY AMMO", 250);
            slots[3] = CreateSlotButton(parent, LoadoutUI.SlotId.SecondaryAmmo, "SECONDARY AMMO", 340);
            slots[4] = CreateSlotButton(parent, LoadoutUI.SlotId.Throwable1, "TACTICAL 1", 430);
            slots[5] = CreateSlotButton(parent, LoadoutUI.SlotId.Throwable2, "TACTICAL 2", 520);

            return slots;
        }

        private static LoadoutUI.SlotButtonRef CreateSlotButton(Transform parent, LoadoutUI.SlotId id, string label, float yOffset)
        {
            var go = CreateImage(parent, $"Slot_{id}", new Color(0.12f, 0.12f, 0.14f, 1f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -yOffset);
            rt.sizeDelta = new Vector2(-40, 70);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true; // MUST be true for buttons
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Accent bar
            var accent = CreateImage(go.transform, "Accent", new Color(0, 0, 0, 0));
            var acRT = accent.GetComponent<RectTransform>();
            acRT.anchorMin = new Vector2(0, 0);
            acRT.anchorMax = new Vector2(0, 1);
            acRT.pivot = new Vector2(0, 0.5f);
            acRT.sizeDelta = new Vector2(4, 0);

            // Slot label
            var slotLabel = CreateText(go.transform, "SlotLabel", label, 14, FontStyle.Bold, TextAnchor.UpperLeft, TextGray,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 10), new Vector2(-20, -10));

            // Item label
            var itemLabel = CreateText(go.transform, "ItemLabel", "— empty —", 18, FontStyle.Bold, TextAnchor.LowerLeft, TextWhite,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 10), new Vector2(-20, -30));

            return new LoadoutUI.SlotButtonRef
            {
                id = id,
                button = btn,
                slotLabel = slotLabel,
                itemLabel = itemLabel,
                accentBar = accent.GetComponent<Image>()
            };
        }

        private static RectTransform CreateItemGrid(Transform parent)
        {
            var title = CreateText(parent, "Title", "AVAILABLE ITEMS", 20, FontStyle.Bold, TextAnchor.UpperLeft, TextGray,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -20), new Vector2(-20, 40));

            // Scroll view - make sure it doesn't overlap amount panel
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0.35f);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = new Vector2(20, 10);
            scrollRT.offsetMax = new Vector2(-20, -70);

            var scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0);

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport with mask
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewportGO.GetComponent<RectTransform>();
            Stretch(vpRT);
            scroll.viewport = vpRT;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var content = contentGO.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);

            var grid = contentGO.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280, 100);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;

            return content;
        }

        private static GameObject CreateItemCard(RectTransform parent)
        {
            var card = CreateImage(parent, "CardTemplate", new Color(0.18f, 0.18f, 0.2f, 1f));
            var img = card.GetComponent<Image>();
            img.raycastTarget = true; // MUST be true for buttons
            card.AddComponent<Button>().targetGraphic = img;

            CreateText(card.transform, "Title", "ITEM NAME", 18, FontStyle.Bold, TextAnchor.UpperLeft, TextWhite,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(15, 0), new Vector2(-15, -15));

            CreateText(card.transform, "Subtitle", "Category • 1.5 KG", 14, FontStyle.Normal, TextAnchor.LowerLeft, TextGray,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(15, 15), new Vector2(-15, -45));

            card.SetActive(false);
            return card;
        }

        private static GameObject CreateAmountControls(Transform parent,
            out Slider slider, out InputField input, out Text amountText,
            out Button maxBtn, out Button clearBtn, out Button plus30, out Button plus60)
        {
            var panel = CreateImage(parent, "AmountPanel", new Color(0.12f, 0.12f, 0.14f, 1f));
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(1, 0);
            panelRT.pivot = new Vector2(0.5f, 0);
            panelRT.anchoredPosition = new Vector2(0, 60);
            panelRT.sizeDelta = new Vector2(-40, 240);

            // Title at top
            CreateText(panel.transform, "Title", "AMOUNT", 20, FontStyle.Bold, TextAnchor.MiddleLeft, TextGray,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -10), new Vector2(-20, 35));

            // Big amount display
            amountText = CreateText(panel.transform, "AmountDisplay", "0", 56, FontStyle.Bold, TextAnchor.MiddleCenter, TextWhite,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -45), new Vector2(0, 75));

            // Slider - fixed position
            var sliderGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGO.name = "AmountSlider";
            sliderGO.transform.SetParent(panel.transform, false);
            var sliderRT = (RectTransform)sliderGO.transform;
            sliderRT.anchorMin = new Vector2(0, 1);
            sliderRT.anchorMax = new Vector2(1, 1);
            sliderRT.pivot = new Vector2(0.5f, 1);
            sliderRT.anchoredPosition = new Vector2(0, -130);
            sliderRT.sizeDelta = new Vector2(-40, 30);
            slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;

            // Buttons row at bottom - FIXED SIZE
            float btnY = 20;
            float btnHeight = 50;
            float btnWidth = 110;
            float spacing = 10;

            clearBtn = CreateFixedButton(panel.transform, "ClearBtn", "CLEAR", AccentRed,
                20, btnY, btnWidth, btnHeight);

            plus30 = CreateFixedButton(panel.transform, "Plus30Btn", "+30", new Color(0.4f, 0.6f, 0.4f, 1f),
                20 + btnWidth + spacing, btnY, btnWidth, btnHeight);

            plus60 = CreateFixedButton(panel.transform, "Plus60Btn", "+60", new Color(0.4f, 0.6f, 0.4f, 1f),
                20 + (btnWidth + spacing) * 2, btnY, btnWidth, btnHeight);

            maxBtn = CreateFixedButton(panel.transform, "MaxBtn", "MAX", AccentGreen,
                20 + (btnWidth + spacing) * 3, btnY, btnWidth, btnHeight);

            // Set input to null - we don't need it
            input = null;

            panel.SetActive(false);
            return panel;
        }

        private static Button CreateFixedButton(Transform parent, string name, string label, Color color,
            float x, float y, float width, float height)
        {
            var go = CreateImage(parent, name, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            return btn;
        }

        private static GameObject CreateWeightBar(Transform parent, out Image fill, out Text text)
        {
            var bar = CreateImage(parent, "WeightBar", new Color(0.12f, 0.12f, 0.14f, 1f));
            var barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 0);
            barRT.anchorMax = new Vector2(1, 0);
            barRT.pivot = new Vector2(0.5f, 0);
            barRT.sizeDelta = new Vector2(-40, 40);
            barRT.anchoredPosition = new Vector2(0, 10);

            text = CreateText(bar.transform, "Text", "WEIGHT: 0.0 / 20.0 KG", 16, FontStyle.Bold, TextAnchor.MiddleLeft, TextWhite,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 0), Vector2.zero);

            var fillBG = CreateImage(bar.transform, "FillBG", new Color(0, 0, 0, 0.5f));
            var fillBGRT = fillBG.GetComponent<RectTransform>();
            fillBGRT.anchorMin = new Vector2(0.5f, 0.5f);
            fillBGRT.anchorMax = new Vector2(0.5f, 0.5f);
            fillBGRT.sizeDelta = new Vector2(300, 10);

            fill = CreateImage(fillBG.transform, "Fill", AccentGreen).GetComponent<Image>();
            var fillRT = fill.GetComponent<RectTransform>();
            Stretch(fillRT);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            return bar;
        }

        // === HELPER METHODS ===

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int size,
            FontStyle style, TextAnchor anchor, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            return text;
        }

        private static Button CreateSimpleButton(Transform parent, string name, string label, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = CreateImage(parent, name, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            CreateText(go.transform, "Label", label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) Stretch(rt);
        }
    }
}
