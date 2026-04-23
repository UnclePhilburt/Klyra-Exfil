using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    public static class InventoryScreenBuilder
    {
        private static readonly Color Backdrop  = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelBG   = new Color(0.07f, 0.08f, 0.10f, 0.98f);
        private static readonly Color HeaderBG  = new Color(0.09f, 0.10f, 0.12f, 1f);
        private static readonly Color Accent    = new Color(0.83f, 0.29f, 0.23f, 1f);
        private static readonly Color TextMain  = new Color(0.92f, 0.93f, 0.95f, 1f);
        private static readonly Color TextDim   = new Color(0.55f, 0.58f, 0.62f, 1f);
        private static readonly Color RowBG     = new Color(0.13f, 0.14f, 0.16f, 1f);

        [MenuItem("Tools/Klyra/Create Inventory Screen")]
        public static void CreateInventoryScreen()
        {
            LoadoutCanvasBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("InventoryCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Inventory Canvas");

            // Root container. When hidden, it takes everything (dim + panel)
            // with it, so the inventory screen never blocks other canvases.
            var root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

            // Dim backdrop inside the root so it disables with the panel.
            var dim = Img(root.transform, "Dim", Backdrop);
            var dimRT = dim.rectTransform;
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            dim.raycastTarget = false; // belt-and-suspenders: don't block clicks

            // Panel — narrow centered column.
            var panel = Img(root.transform, "Panel", PanelBG);
            var panelRT = panel.rectTransform;
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(640, 820);

            // Header bar with accent stripe.
            var header = Img(panel.transform, "Header", HeaderBG);
            var hRT = header.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.sizeDelta = new Vector2(0, 80);
            hRT.anchoredPosition = Vector2.zero;

            var stripe = Img(header.transform, "AccentStripe", Accent);
            var stRT = stripe.rectTransform;
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(1, 0);
            stRT.pivot = new Vector2(0.5f, 0); stRT.sizeDelta = new Vector2(0, 3);

            var title = Txt(header.transform, "Title", "INVENTORY", 36, FontStyle.Bold, TextAnchor.MiddleLeft, TextMain);
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0, 0.5f);
            tRT.offsetMin = new Vector2(30, 0);
            tRT.offsetMax = new Vector2(-30, 0);

            var hint = Txt(header.transform, "Hint", "TAB TO CLOSE", 14, FontStyle.Normal, TextAnchor.MiddleRight, TextDim);
            var hintRT = hint.rectTransform;
            hintRT.anchorMin = new Vector2(0, 0); hintRT.anchorMax = new Vector2(1, 1);
            hintRT.pivot = new Vector2(1, 0.5f);
            hintRT.offsetMin = new Vector2(30, 0);
            hintRT.offsetMax = new Vector2(-30, 0);

            // Scroll content.
            var scrollGO = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0);
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            var scRT = (RectTransform)scrollGO.transform;
            scRT.anchorMin = new Vector2(0, 0); scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(20, 20); scRT.offsetMax = new Vector2(-20, -100);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewportGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            scroll.viewport = vpRT;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = Vector2.zero;

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRT;

            // Row template.
            var row = Img(contentRT, "RowTemplate", RowBG);
            row.rectTransform.sizeDelta = new Vector2(0, 64);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 64;

            var rowName = Txt(row.transform, "Name", "ITEM NAME", 22, FontStyle.Bold, TextAnchor.MiddleLeft, TextMain);
            var rnRT = rowName.rectTransform;
            rnRT.anchorMin = new Vector2(0, 0); rnRT.anchorMax = new Vector2(1, 1);
            rnRT.pivot = new Vector2(0, 0.5f);
            rnRT.offsetMin = new Vector2(20, 0); rnRT.offsetMax = new Vector2(-120, 0);

            var rowCount = Txt(row.transform, "Count", "0", 26, FontStyle.Bold, TextAnchor.MiddleRight, Accent);
            var rcRT = rowCount.rectTransform;
            rcRT.anchorMin = new Vector2(1, 0); rcRT.anchorMax = new Vector2(1, 1);
            rcRT.pivot = new Vector2(1, 0.5f);
            rcRT.sizeDelta = new Vector2(120, 0);
            rcRT.anchoredPosition = new Vector2(-20, 0);

            row.gameObject.SetActive(false);

            // Component wire-up.
            var screen = canvasGO.AddComponent<InventoryScreen>();
            // Panel is what we toggle — it contains both the dim and the panel visual now.
            screen.panel = root;
            screen.content = contentRT;
            screen.rowTemplate = row.gameObject;

            root.SetActive(false);
            Selection.activeGameObject = canvasGO;
            Debug.Log("InventoryCanvas created. Place it in the Station scene; it persists across scenes via DontDestroyOnLoad.");
        }

        // --- helpers ------------------------------------------------------

        private static Image Img(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text Txt(Transform parent, string name, string content, int size, FontStyle style, TextAnchor anchor, Color color)
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
    }
}
