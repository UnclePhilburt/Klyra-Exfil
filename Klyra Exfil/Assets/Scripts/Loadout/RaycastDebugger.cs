using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Klyra.Loadout
{
    /// <summary>
    /// Shows EXACTLY what is blocking clicks
    /// </summary>
    public class RaycastDebugger : MonoBehaviour
    {
        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = mouse.position.ReadValue()
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                Debug.Log($"========== CLICK at {mouse.position.ReadValue()} - Found {results.Count} hits ==========");

                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    string path = GetPath(result.gameObject.transform);
                    var img = result.gameObject.GetComponent<Image>();
                    var txt = result.gameObject.GetComponent<Text>();
                    var btn = result.gameObject.GetComponent<Button>();
                    var scroll = result.gameObject.GetComponent<ScrollRect>();

                    Debug.Log($"  [{i}] {path} | Depth: {result.depth} | SortOrder: {result.sortingOrder} | Image: {img != null} (raycast={img?.raycastTarget}) | Text: {txt != null} | Button: {btn != null} | Scroll: {scroll != null}");
                }
            }
        }

        string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
