using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Klyra.Loadout.EditorTools
{
    public class FixLoadoutRaycasts : EditorWindow
    {
        [MenuItem("Tools/Klyra/Fix Loadout Button Raycasts")]
        public static void FixRaycasts()
        {
            // Find the LoadoutCanvas in the scene (include inactive)
            var loadoutUI = Object.FindObjectOfType<LoadoutUI>(true);
            if (loadoutUI == null)
            {
                // Try finding by name instead
                var canvasObj = GameObject.Find("LoadoutCanvas");
                if (canvasObj != null)
                {
                    loadoutUI = canvasObj.GetComponent<LoadoutUI>();
                }

                if (loadoutUI == null)
                {
                    Debug.LogError("No LoadoutUI found in scene. Make sure you have a LoadoutCanvas object with a LoadoutUI component.");
                    return;
                }
            }

            Debug.Log($"Found LoadoutUI on: {loadoutUI.gameObject.name}");

            int fixedCount = 0;

            // Fix all Text components in the entire canvas to not block raycasts
            var allTexts = loadoutUI.GetComponentsInChildren<Text>(true);
            foreach (var text in allTexts)
            {
                if (text.raycastTarget)
                {
                    Undo.RecordObject(text, "Disable Text Raycast");
                    text.raycastTarget = false;
                    fixedCount++;
                    Debug.Log($"Disabled raycast on Text: {GetGameObjectPath(text.gameObject)}");
                }
            }

            // Disable raycast on ALL images except button target graphics
            var allImages = loadoutUI.GetComponentsInChildren<Image>(true);
            foreach (var image in allImages)
            {
                var button = image.GetComponent<Button>();
                // Only keep raycast enabled if this image IS a button's target graphic
                bool isButtonGraphic = (button != null && button.targetGraphic == image);

                if (!isButtonGraphic && image.raycastTarget)
                {
                    Undo.RecordObject(image, "Disable Image Raycast");
                    image.raycastTarget = false;
                    fixedCount++;
                    Debug.Log($"Disabled raycast on Image: {GetGameObjectPath(image.gameObject)}");
                }
            }

            // Make sure the +/- buttons specifically have proper setup
            if (loadoutUI.amountMinus != null)
            {
                Debug.Log($"Minus button found: {loadoutUI.amountMinus.name}");
                Debug.Log($"  - Interactable: {loadoutUI.amountMinus.interactable}");
                Debug.Log($"  - Target Graphic: {loadoutUI.amountMinus.targetGraphic}");
                if (loadoutUI.amountMinus.targetGraphic != null)
                {
                    Debug.Log($"  - Target raycast: {loadoutUI.amountMinus.targetGraphic.raycastTarget}");
                }
            }
            if (loadoutUI.amountPlus != null)
            {
                Debug.Log($"Plus button found: {loadoutUI.amountPlus.name}");
                Debug.Log($"  - Interactable: {loadoutUI.amountPlus.interactable}");
                Debug.Log($"  - Target Graphic: {loadoutUI.amountPlus.targetGraphic}");
                if (loadoutUI.amountPlus.targetGraphic != null)
                {
                    Debug.Log($"  - Target raycast: {loadoutUI.amountPlus.targetGraphic.raycastTarget}");
                }
            }

            Debug.Log($"<color=green>Fixed {fixedCount} raycast targets in LoadoutUI!</color>");
            EditorUtility.DisplayDialog("Success", $"Fixed {fixedCount} raycast targets.\n\nYour buttons should now be fully clickable!", "OK");
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
