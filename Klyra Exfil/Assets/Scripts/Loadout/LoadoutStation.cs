using UnityEngine;
using UnityEngine.InputSystem;

namespace Klyra.Loadout
{
    /// <summary>
    /// Drop this on any furniture (dresser, locker, table) along with a
    /// trigger collider. When a player stands in the trigger and presses
    /// the interact key, the assigned loadout UI is shown.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LoadoutStation : MonoBehaviour
    {
        [Tooltip("The Canvas (or any GameObject) that is the loadout UI. Enabled on interact, disabled otherwise.")]
        public GameObject loadoutUI;

        [Tooltip("Key the player presses to open the loadout while standing in range.")]
        public Key interactKey = Key.F;

        [Tooltip("Tag used to detect the player entering the trigger.")]
        public string playerTag = "Player";

        [Tooltip("Optional prompt GameObject shown while the player is in range (e.g. a floating 'Press E' label).")]
        public GameObject promptUI;

        private bool playerInRange = false;

        private void Awake()
        {
            // Prefer an existing trigger collider; only warn if none exists.
            // Multi-collider rigs (Mesh + Box) are normal — the mesh blocks,
            // the trigger opens the UI.
            var cols = GetComponents<Collider>();
            bool hasTrigger = false;
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].isTrigger) { hasTrigger = true; break; }
            }
            if (!hasTrigger)
            {
                Debug.LogWarning($"LoadoutStation on '{name}' has no trigger collider — add a Box/Sphere Collider with Is Trigger enabled.", this);
            }

            if (loadoutUI != null) loadoutUI.SetActive(false);
            if (promptUI != null) promptUI.SetActive(false);
        }

        private bool IsPlayer(Collider other)
        {
            // Child bone colliders on UCC characters are Untagged; the root
            // holds the tag. Rigidbody.attachedRigidbody walks to the root.
            var body = other.attachedRigidbody;
            var root = body != null ? body.gameObject : other.transform.root.gameObject;
            return root.CompareTag(playerTag);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            playerInRange = true;
            Debug.Log($"[LoadoutStation:{name}] Player in range — press {interactKey}");
            if (promptUI != null) promptUI.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            playerInRange = false;
            Debug.Log($"[LoadoutStation:{name}] Player left range");
            if (promptUI != null) promptUI.SetActive(false);
            if (loadoutUI != null) loadoutUI.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb[interactKey].wasPressedThisFrame) return;

            Debug.Log($"[LoadoutStation:{name}] Interact pressed. loadoutUI assigned? {(loadoutUI != null)}", this);
            if (loadoutUI == null) return;

            bool open = !loadoutUI.activeSelf;
            loadoutUI.SetActive(open);
            if (promptUI != null) promptUI.SetActive(!open);
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }
    }
}
