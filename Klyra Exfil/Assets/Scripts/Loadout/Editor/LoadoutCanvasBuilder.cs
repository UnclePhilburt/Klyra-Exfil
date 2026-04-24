using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    public static class LoadoutCanvasBuilder
    {
        // Palette
        private static readonly Color Backdrop  = new Color(0.00f, 0.00f, 0.00f, 0.82f);
        private static readonly Color PanelBG   = new Color(0.07f, 0.08f, 0.10f, 1.00f);
        private static readonly Color HeaderBG  = new Color(0.09f, 0.10f, 0.12f, 1.00f);
        private static readonly Color ColumnBG  = new Color(0.10f, 0.11f, 0.13f, 1.00f);
        private static readonly Color CardBG    = new Color(0.13f, 0.14f, 0.16f, 1.00f);
        private static readonly Color Accent    = new Color(0.83f, 0.29f, 0.23f, 1.00f);
        private static readonly Color TextMain  = new Color(0.92f, 0.93f, 0.95f, 1.00f);
        private static readonly Color TextDim   = new Color(0.55f, 0.58f, 0.62f, 1.00f);
        private static readonly Color SaveBG    = new Color(0.22f, 0.55f, 0.28f, 1.00f);
        private static readonly Color CloseBG   = new Color(0.30f, 0.09f, 0.10f, 1.00f);
        private static readonly Color DividerCol = new Color(1f, 1f, 1f, 0.06f);

        [MenuItem("Tools/Klyra/Create Loadout Canvas")]
        public static void CreateLoadoutCanvas()
        {
            EnsureEventSystem();

            var canvasGO = new GameObject("LoadoutCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // HIGHER THAN SUBTITLES (200) SO NOTHING BLOCKS IT
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Loadout Canvas");

            // Full-screen dim backdrop.
            var dim = NewImage(canvasGO.transform, "Dim", Backdrop);
            Stretch(dim.rectTransform);

            // Root panel — nearly full-screen with a small inset.
            var panel = NewImage(canvasGO.transform, "Panel", PanelBG);
            var panelRT = panel.rectTransform;
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = new Vector2(80, 60);
            panelRT.offsetMax = new Vector2(-80, -60);

            // --- HEADER -------------------------------------------------
            var header = NewImage(panel.transform, "Header", HeaderBG);
            var headerRT = header.rectTransform;
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.sizeDelta = new Vector2(0, 90);
            headerRT.anchoredPosition = Vector2.zero;

            // Accent stripe under the header.
            var accentStripe = NewImage(header.transform, "AccentStripe", Accent);
            var asRT = accentStripe.rectTransform;
            asRT.anchorMin = new Vector2(0, 0);
            asRT.anchorMax = new Vector2(1, 0);
            asRT.pivot = new Vector2(0.5f, 0);
            asRT.sizeDelta = new Vector2(0, 3);
            asRT.anchoredPosition = Vector2.zero;

            // Title on the left.
            var title = NewText(header.transform, "Title", "LOADOUT", 44, FontStyle.Bold, TextAnchor.MiddleLeft, TextMain);
            var titleRT = title.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0);
            titleRT.anchorMax = new Vector2(0, 1);
            titleRT.pivot = new Vector2(0, 0.5f);
            titleRT.anchoredPosition = new Vector2(30, 0);
            titleRT.sizeDelta = new Vector2(500, 0);

            var subtitle = NewText(header.transform, "Subtitle", "SELECT GEAR FOR DEPLOYMENT", 16, FontStyle.Normal, TextAnchor.LowerLeft, TextDim);
            var subRT = subtitle.rectTransform;
            subRT.anchorMin = new Vector2(0, 0);
            subRT.anchorMax = new Vector2(0, 1);
            subRT.pivot = new Vector2(0, 0.5f);
            subRT.anchoredPosition = new Vector2(270, 4);
            subRT.sizeDelta = new Vector2(500, 40);

            // Save / Close on the right of the header.
            var saveBtn = BuildButton(header.transform, "SaveButton", "SAVE", SaveBG);
            var saveRT = ((RectTransform)saveBtn.transform);
            saveRT.anchorMin = new Vector2(1, 0.5f);
            saveRT.anchorMax = new Vector2(1, 0.5f);
            saveRT.pivot = new Vector2(1, 0.5f);
            saveRT.sizeDelta = new Vector2(180, 56);
            saveRT.anchoredPosition = new Vector2(-220, 0);

            var closeBtn = BuildButton(header.transform, "CloseButton", "CLOSE", CloseBG);
            var closeRT = ((RectTransform)closeBtn.transform);
            closeRT.anchorMin = new Vector2(1, 0.5f);
            closeRT.anchorMax = new Vector2(1, 0.5f);
            closeRT.pivot = new Vector2(1, 0.5f);
            closeRT.sizeDelta = new Vector2(180, 56);
            closeRT.anchoredPosition = new Vector2(-30, 0);

            // Character dropdown in the header (between title and buttons).
            var charDD = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            charDD.name = "CharacterDropdown";
            charDD.transform.SetParent(header.transform, false);
            var charRT = (RectTransform)charDD.transform;
            charRT.anchorMin = new Vector2(1, 0.5f);
            charRT.anchorMax = new Vector2(1, 0.5f);
            charRT.pivot = new Vector2(1, 0.5f);
            charRT.sizeDelta = new Vector2(240, 48);
            charRT.anchoredPosition = new Vector2(-450, 0);
            var charImg = charDD.GetComponent<Image>();
            if (charImg != null) charImg.color = new Color(0.13f, 0.15f, 0.18f, 1f);
            var charLabel = charDD.transform.Find("Label")?.GetComponent<Text>();
            if (charLabel != null) { charLabel.color = TextMain; charLabel.fontSize = 18; }
            var charItem = charDD.transform.Find("Template/Viewport/Content/Item/Item Label")?.GetComponent<Text>();
            if (charItem != null) { charItem.color = new Color(0.10f, 0.11f, 0.13f, 1f); charItem.fontSize = 18; charItem.fontStyle = FontStyle.Bold; }

            // --- LEFT COLUMN (slot list) --------------------------------
            var left = NewImage(panel.transform, "LeftColumn", ColumnBG);
            var leftRT = left.rectTransform;
            leftRT.anchorMin = new Vector2(0, 0);
            leftRT.anchorMax = new Vector2(0, 1);
            leftRT.pivot = new Vector2(0, 0.5f);
            leftRT.sizeDelta = new Vector2(540, -90);
            leftRT.anchoredPosition = new Vector2(0, -45);

            NewText(left.transform, "LeftHeader", "SLOTS", 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextDim)
                .rectTransform.anchoredPosition = new Vector2(30, -30);
            ((RectTransform)left.transform.Find("LeftHeader")).anchorMin = new Vector2(0, 1);
            ((RectTransform)left.transform.Find("LeftHeader")).anchorMax = new Vector2(1, 1);
            ((RectTransform)left.transform.Find("LeftHeader")).pivot = new Vector2(0, 1);
            ((RectTransform)left.transform.Find("LeftHeader")).sizeDelta = new Vector2(-30, 30);

            // Slot buttons stacked vertically.
            var slotRefs = new LoadoutUI.SlotButtonRef[6];
            slotRefs[0] = CreateSlotButton(left.transform, LoadoutUI.SlotId.Primary,        "PRIMARY",        -80);
            slotRefs[1] = CreateSlotButton(left.transform, LoadoutUI.SlotId.Secondary,      "SECONDARY",     -170);
            slotRefs[2] = CreateSlotButton(left.transform, LoadoutUI.SlotId.PrimaryAmmo,    "PRIMARY AMMO",  -260);
            slotRefs[3] = CreateSlotButton(left.transform, LoadoutUI.SlotId.SecondaryAmmo,  "SECONDARY AMMO",-350);
            slotRefs[4] = CreateSlotButton(left.transform, LoadoutUI.SlotId.Throwable1,     "TACTICAL 1",    -440);
            slotRefs[5] = CreateSlotButton(left.transform, LoadoutUI.SlotId.Throwable2,     "TACTICAL 2",    -530);

            // --- RIGHT COLUMN (item cards) ------------------------------
            var right = NewImage(panel.transform, "RightColumn", ColumnBG);
            var rightRT = right.rectTransform;
            rightRT.anchorMin = new Vector2(0, 0);
            rightRT.anchorMax = new Vector2(1, 1);
            rightRT.pivot = new Vector2(0, 0.5f);
            rightRT.sizeDelta = new Vector2(-540, -90);
            rightRT.anchoredPosition = new Vector2(540, -45);

            NewText(right.transform, "RightHeader", "AVAILABLE ITEMS", 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextDim)
                .rectTransform.anchoredPosition = new Vector2(30, -30);
            var rightHeaderRT = (RectTransform)right.transform.Find("RightHeader");
            rightHeaderRT.anchorMin = new Vector2(0, 1);
            rightHeaderRT.anchorMax = new Vector2(1, 1);
            rightHeaderRT.pivot = new Vector2(0, 1);
            rightHeaderRT.sizeDelta = new Vector2(-30, 30);

            // Scroll view for item cards - DON'T OVERLAP AMOUNT PANEL
            var scroll = CreateScrollView(right.transform, out RectTransform gridContent);
            scroll.anchorMin = new Vector2(0, 0);
            scroll.anchorMax = new Vector2(1, 1);
            scroll.pivot = new Vector2(0.5f, 0.5f);
            scroll.offsetMin = new Vector2(20, 140); // Above amount panel (which is at Y=20 with height 100 = goes to 120)
            scroll.offsetMax = new Vector2(-20, -70);

            // Item card template — disabled, cloned at runtime.
            var cardTemplate = CreateItemCardTemplate(gridContent);

            // Weight bar — pinned above the amount panel.
            var weightBar = NewImage(right.transform, "WeightBar", CardBG);
            var wbRT = weightBar.rectTransform;
            wbRT.anchorMin = new Vector2(0, 0);
            wbRT.anchorMax = new Vector2(1, 0);
            wbRT.pivot = new Vector2(0.5f, 0);
            wbRT.sizeDelta = new Vector2(-40, 44);
            wbRT.anchoredPosition = new Vector2(0, 110);

            var fillBG = NewImage(weightBar.transform, "FillBG", new Color(0f, 0f, 0f, 0.35f));
            var fillBGRT = fillBG.rectTransform;
            fillBGRT.anchorMin = fillBGRT.anchorMax = new Vector2(0, 0.5f);
            fillBGRT.pivot = new Vector2(0, 0.5f);
            fillBGRT.sizeDelta = new Vector2(400, 18);
            fillBGRT.anchoredPosition = new Vector2(200, 0);

            var fill = NewImage(fillBG.transform, "Fill", new Color(0.38f, 0.70f, 0.35f, 1f));
            var fillRT = fill.rectTransform;
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            var weightText = NewText(weightBar.transform, "Readout", "WEIGHT  0.0 / 20.0 KG", 16, FontStyle.Bold, TextAnchor.MiddleLeft, TextMain);
            var wtRT = weightText.rectTransform;
            wtRT.anchorMin = new Vector2(1, 0); wtRT.anchorMax = new Vector2(1, 1);
            wtRT.pivot = new Vector2(1, 0.5f);
            wtRT.sizeDelta = new Vector2(440, 0);
            wtRT.anchoredPosition = new Vector2(-20, 0);

            // Amount panel at the bottom of the right column - COMPLETELY REBUILT
            var amountPanel = NewImage(right.transform, "AmountPanel", CardBG);
            var apRT = amountPanel.rectTransform;
            apRT.anchorMin = new Vector2(0, 0);
            apRT.anchorMax = new Vector2(1, 0);
            apRT.pivot = new Vector2(0.5f, 0);
            apRT.sizeDelta = new Vector2(-40, 100);
            apRT.anchoredPosition = new Vector2(0, 20);

            // Subtract button - CREATED FIRST SO NOTHING BLOCKS IT
            var minusGO = new GameObject("Minus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            minusGO.transform.SetParent(amountPanel.transform, false);
            var minusImg = minusGO.GetComponent<Image>();
            minusImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            minusImg.raycastTarget = true;
            var minus1Btn = minusGO.GetComponent<Button>();
            minus1Btn.targetGraphic = minusImg;
            var m1RT = (RectTransform)minusGO.transform;
            m1RT.anchorMin = new Vector2(0.05f, 0.05f);
            m1RT.anchorMax = new Vector2(0.48f, 0.55f);
            m1RT.offsetMin = Vector2.zero;
            m1RT.offsetMax = Vector2.zero;

            var minusText = NewText(minusGO.transform, "Text", "−", 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            minusText.raycastTarget = false;
            var mtRT = minusText.rectTransform;
            mtRT.anchorMin = Vector2.zero;
            mtRT.anchorMax = Vector2.one;
            mtRT.offsetMin = Vector2.zero;
            mtRT.offsetMax = Vector2.zero;

            // Add button - bottom right
            var plusGO = new GameObject("Plus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            plusGO.transform.SetParent(amountPanel.transform, false);
            var plusImg = plusGO.GetComponent<Image>();
            plusImg.color = new Color(0.2f, 0.7f, 0.2f, 1f);
            plusImg.raycastTarget = true;
            var plus1Btn = plusGO.GetComponent<Button>();
            plus1Btn.targetGraphic = plusImg;
            var p1RT = (RectTransform)plusGO.transform;
            p1RT.anchorMin = new Vector2(0.52f, 0.05f);
            p1RT.anchorMax = new Vector2(0.95f, 0.55f);
            p1RT.offsetMin = Vector2.zero;
            p1RT.offsetMax = Vector2.zero;

            var plusText = NewText(plusGO.transform, "Text", "+", 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            plusText.raycastTarget = false;
            var ptRT = plusText.rectTransform;
            ptRT.anchorMin = Vector2.zero;
            ptRT.anchorMax = Vector2.one;
            ptRT.offsetMin = Vector2.zero;
            ptRT.offsetMax = Vector2.zero;

            // Amount display - ABOVE buttons, no overlap AT ALL
            var amountValue = NewText(amountPanel.transform, "AmountValue", "0", 48, FontStyle.Bold, TextAnchor.MiddleCenter, TextMain);
            var avRT = amountValue.rectTransform;
            avRT.anchorMin = new Vector2(0, 0.6f);
            avRT.anchorMax = new Vector2(1, 0.95f);
            avRT.offsetMin = Vector2.zero;
            avRT.offsetMax = Vector2.zero;
            amountValue.raycastTarget = false;

            // DESTROY the text's game object raycast - nuclear option
            var avCanvas = amountValue.gameObject.AddComponent<CanvasGroup>();
            avCanvas.blocksRaycasts = false;
            avCanvas.interactable = false;

            // --- WIRE LoadoutUI ----------------------------------------
            var ui = canvasGO.AddComponent<LoadoutUI>();
            ui.gridContent = gridContent;
            ui.itemCardTemplate = cardTemplate;
            ui.amountPanel = amountPanel.gameObject;
            ui.amountMinus = minus1Btn;
            ui.amountPlus = plus1Btn;
            ui.amountValue = amountValue;
            ui.weightReadout = weightText;
            ui.weightFill = fill;
            ui.characterDropdown = charDD.GetComponent<Dropdown>();
            ui.saveButton = saveBtn;
            ui.closeButton = closeBtn;
            ui.slotButtons.Clear();
            ui.slotButtons.AddRange(slotRefs);

            // ADD CLICK LOGGER AND RAYCAST DEBUGGER
            canvasGO.AddComponent<ClickLogger>();
            canvasGO.AddComponent<RaycastDebugger>();

            canvasGO.SetActive(false);
            Selection.activeGameObject = canvasGO;
            Debug.Log("Loadout canvas (Ready-or-Not style) created WITH CLICK LOGGING. Drag into LoadoutStation's Loadout UI field.");
        }

        [MenuItem("Tools/Klyra/Create Loadout Manager")]
        public static void CreateLoadoutManager()
        {
            if (Object.FindObjectOfType<LoadoutManager>() != null)
            {
                Debug.Log("LoadoutManager already exists in the scene.");
                return;
            }
            var go = new GameObject("LoadoutManager", typeof(LoadoutManager));
            Undo.RegisterCreatedObjectUndo(go, "Create LoadoutManager");
            Selection.activeGameObject = go;
            Debug.Log("LoadoutManager created. Populate the Available Items list in the inspector.");
        }

        // --- helpers ------------------------------------------------------

        private static LoadoutUI.SlotButtonRef CreateSlotButton(Transform parent, LoadoutUI.SlotId id, string label, float y)
        {
            var go = NewImage(parent, $"Slot_{id}", CardBG);
            var rt = go.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-40, 76);
            rt.anchoredPosition = new Vector2(20, y);

            // Accent bar on the left edge (hidden when unselected, filled when selected).
            var accent = NewImage(go.transform, "Accent", new Color(0,0,0,0));
            var accentRT = accent.rectTransform;
            accentRT.anchorMin = new Vector2(0, 0);
            accentRT.anchorMax = new Vector2(0, 1);
            accentRT.pivot = new Vector2(0, 0.5f);
            accentRT.sizeDelta = new Vector2(6, 0);
            accentRT.anchoredPosition = Vector2.zero;

            var slotLabel = NewText(go.transform, "SlotLabel", label, 14, FontStyle.Bold, TextAnchor.UpperLeft, TextDim);
            var slRT = slotLabel.rectTransform;
            slRT.anchorMin = new Vector2(0, 0); slRT.anchorMax = new Vector2(1, 1);
            slRT.pivot = new Vector2(0, 1);
            slRT.offsetMin = new Vector2(24, 0);
            slRT.offsetMax = new Vector2(-16, -10);

            var itemLabel = NewText(go.transform, "ItemLabel", "— empty —", 22, FontStyle.Bold, TextAnchor.LowerLeft, TextMain);
            var ilRT = itemLabel.rectTransform;
            ilRT.anchorMin = new Vector2(0, 0); ilRT.anchorMax = new Vector2(1, 1);
            ilRT.pivot = new Vector2(0, 0);
            ilRT.offsetMin = new Vector2(24, 8);
            ilRT.offsetMax = new Vector2(-16, -30);

            go.raycastTarget = true; // SLOT BUTTONS NEED RAYCASTS
            var btn = go.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;
            btn.targetGraphic = go;

            return new LoadoutUI.SlotButtonRef
            {
                id = id, button = btn,
                slotLabel = slotLabel, itemLabel = itemLabel,
                accentBar = accent
            };
        }

        private static RectTransform CreateScrollView(Transform parent, out RectTransform content)
        {
            var scrollGO = new GameObject("ItemGrid", typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var img = scrollGO.GetComponent<Image>();
            img.color = new Color(0,0,0,0);
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport: use RectMask2D so we don't need a mask sprite. A stock
            // Mask component with a spriteless Image stencils everything to
            // invisible — cards stay clickable but never render.
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewportGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            scroll.viewport = vpRT;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            content = contentGO.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var grid = contentGO.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.cellSize = new Vector2(260, 110);
            grid.spacing = new Vector2(12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;

            return (RectTransform)scrollGO.transform;
        }

        private static GameObject CreateItemCardTemplate(RectTransform gridContent)
        {
            var card = NewImage(gridContent, "CardTemplate", CardBG);
            var rt = card.rectTransform;
            rt.sizeDelta = new Vector2(260, 110);
            card.raycastTarget = true; // ITEM CARDS NEED RAYCASTS
            card.gameObject.AddComponent<Button>().targetGraphic = card;

            var title = NewText(card.transform, "Title", "ITEM NAME", 20, FontStyle.Bold, TextAnchor.UpperLeft, TextMain);
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0, 1);
            tRT.offsetMin = new Vector2(16, 0);
            tRT.offsetMax = new Vector2(-16, -12);

            var sub = NewText(card.transform, "Subtitle", "category", 14, FontStyle.Normal, TextAnchor.LowerLeft, TextDim);
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0, 0);
            sRT.offsetMin = new Vector2(16, 14);
            sRT.offsetMax = new Vector2(-16, -50);

            card.gameObject.SetActive(false);
            return card.gameObject;
        }

        private static Button BuildButton(Transform parent, string name, string label, Color bg)
        {
            var img = NewImage(parent, name, bg);
            img.raycastTarget = true; // BUTTONS NEED RAYCASTS
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            var txt = NewText(img.transform, "Label", label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var txRT = txt.rectTransform;
            txRT.anchorMin = Vector2.zero; txRT.anchorMax = Vector2.one;
            txRT.offsetMin = Vector2.zero; txRT.offsetMax = Vector2.zero;
            // Disable raycast on text so it doesn't block button clicks
            txt.raycastTarget = false;
            return btn;
        }

        private static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false; // DON'T BLOCK CLICKS BY DEFAULT
            return img;
        }

        private static Text NewText(Transform parent, string name, string content, int size,
            FontStyle style, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        internal static void EnsureEventSystem()
        {
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing == null)
            {
                var go = new GameObject("EventSystem",
                    typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            }
            else if (existing.GetComponent<StandaloneInputModule>() != null
                     && existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                // Replace the old module so the scene works with the new Input System.
                Undo.DestroyObjectImmediate(existing.GetComponent<StandaloneInputModule>());
                Undo.AddComponent<InputSystemUIInputModule>(existing.gameObject);
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
