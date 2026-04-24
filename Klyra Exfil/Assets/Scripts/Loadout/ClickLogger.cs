using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Klyra.Loadout
{
    /// <summary>
    /// Logs EVERY SINGLE CLICK on the canvas to debug what the fuck is being clicked
    /// </summary>
    public class ClickLogger : MonoBehaviour, IPointerClickHandler
    {
        void Start()
        {
            // Add this to EVERYTHING in the canvas
            var allObjects = GetComponentsInChildren<Transform>(true);
            foreach (var obj in allObjects)
            {
                if (obj.GetComponent<ClickLogger>() == null)
                {
                    obj.gameObject.AddComponent<ClickLogger>();
                }
            }
            Debug.Log($"[ClickLogger] Added click logging to {allObjects.Length} objects");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            string path = GetPath(transform);
            var button = GetComponent<Button>();
            var image = GetComponent<Image>();
            var text = GetComponent<Text>();
            var canvasGroup = GetComponent<CanvasGroup>();
            var scrollRect = GetComponent<ScrollRect>();

            // Get sibling index
            int siblingIndex = transform.GetSiblingIndex();
            int totalSiblings = transform.parent != null ? transform.parent.childCount : 0;

            string cgInfo = canvasGroup != null ? $"blocks={canvasGroup.blocksRaycasts}" : "null";
            Debug.Log($"[CLICK] {path} | SiblingIndex: {siblingIndex}/{totalSiblings} | Button: {button != null} | Image: {image != null} (raycast={image?.raycastTarget}) | Text: {text != null} (raycast={text?.raycastTarget}) | CanvasGroup: {cgInfo} | ScrollRect: {scrollRect != null}");
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
