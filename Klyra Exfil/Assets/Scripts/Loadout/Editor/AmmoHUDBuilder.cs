using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    public static class AmmoHUDBuilder
    {
        private static readonly Color BG         = new Color(0.07f, 0.08f, 0.10f, 0.85f);
        private static readonly Color Accent     = new Color(0.98f, 0.78f, 0.30f, 1f);
        private static readonly Color TextMain   = new Color(0.98f, 0.98f, 0.98f, 1f);
        private static readonly Color TextDim    = new Color(0.55f, 0.58f, 0.62f, 1f);

        [MenuItem("Tools/Klyra/Create Ammo HUD")]
        public static void CreateAmmoHUD()
        {
            LoadoutCanvasBuilder.EnsureEventSystem();

            var canvasGO = new GameObject("AmmoHUDCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // below the loadout / inventory screens
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            // The HUD is non-interactive; disable the raycaster so it never
            // blocks clicks on the loadout UI behind it.
            canvasGO.GetComponent<GraphicRaycaster>().enabled = false;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Ammo HUD");

            // Root wrapper — lets AmmoHUD hide the whole thing.
            var root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(1, 0); rootRT.anchorMax = new Vector2(1, 0);
            rootRT.pivot = new Vector2(1, 0);
            rootRT.sizeDelta = new Vector2(360, 150);
            rootRT.anchoredPosition = new Vector2(-30, 30);

            // Background panel.
            var bg = Img(root.transform, "BG", BG);
            var bgRT = bg.rectTransform;
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            // Accent stripe at the top.
            var stripe = Img(root.transform, "AccentStripe", Accent);
            var stRT = stripe.rectTransform;
            stRT.anchorMin = new Vector2(0, 1); stRT.anchorMax = new Vector2(1, 1);
            stRT.pivot = new Vector2(0.5f, 1); stRT.sizeDelta = new Vector2(0, 3);

            // Weapon name (top row).
            var weaponName = Txt(root.transform, "WeaponName", "WEAPON", 18, FontStyle.Bold, TextAnchor.UpperLeft, TextDim);
            var wnRT = weaponName.rectTransform;
            wnRT.anchorMin = new Vector2(0, 1); wnRT.anchorMax = new Vector2(1, 1);
            wnRT.pivot = new Vector2(0, 1);
            wnRT.offsetMin = new Vector2(20, -40);
            wnRT.offsetMax = new Vector2(-20, -14);

            // Clip (big).
            var clip = Txt(root.transform, "Clip", "0", 64, FontStyle.Bold, TextAnchor.MiddleLeft, TextMain);
            var clRT = clip.rectTransform;
            clRT.anchorMin = new Vector2(0, 0); clRT.anchorMax = new Vector2(0, 1);
            clRT.pivot = new Vector2(0, 0.5f);
            clRT.sizeDelta = new Vector2(200, 110);
            clRT.anchoredPosition = new Vector2(20, -8);

            // Reserve (smaller, to the right of clip).
            var reserve = Txt(root.transform, "Reserve", "/ 0", 28, FontStyle.Bold, TextAnchor.MiddleLeft, TextDim);
            var reRT = reserve.rectTransform;
            reRT.anchorMin = new Vector2(0, 0); reRT.anchorMax = new Vector2(1, 1);
            reRT.pivot = new Vector2(0, 0.5f);
            reRT.sizeDelta = new Vector2(0, 60);
            reRT.anchoredPosition = new Vector2(160, -8);

            var hud = canvasGO.AddComponent<AmmoHUD>();
            hud.root = root;
            hud.clipText = clip;
            hud.reserveText = reserve;
            hud.weaponNameText = weaponName;

            root.SetActive(false);

            Selection.activeGameObject = canvasGO;
            Debug.Log("AmmoHUD canvas created. Put it in the mission scene (or any scene where the player fires).");
        }

        private static Image Img(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text Txt(Transform parent, string name, string content, int size,
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
    }
}
