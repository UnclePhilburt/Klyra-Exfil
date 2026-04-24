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
        private GameObject currentPlayer = null;

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

        private Collider triggerCollider;

        private Collider GetTriggerCollider()
        {
            if (triggerCollider != null) return triggerCollider;
            var cols = GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].isTrigger) { triggerCollider = cols[i]; return cols[i]; }
            }
            return null;
        }

        /// <summary>
        /// Polls every frame for the local player inside our trigger. We use
        /// this instead of OnTriggerEnter/Exit because when the player is
        /// destroyed + respawned in place (character swap), Unity doesn't
        /// always fire a fresh enter event on the newly spawned body.
        /// </summary>
        private void UpdatePlayerInRange()
        {
            var local = FindLocalPlayer();
            bool nowInRange = false;
            if (local != null)
            {
                var trg = GetTriggerCollider();
                if (trg != null)
                {
                    // Take the closest point on the trigger to the player; if
                    // it equals the player's position, they're inside.
                    var closest = trg.ClosestPoint(local.transform.position);
                    nowInRange = (closest - local.transform.position).sqrMagnitude < 0.0001f;
                }
            }

            if (nowInRange != playerInRange)
            {
                playerInRange = nowInRange;
                currentPlayer = nowInRange ? local : null;
                if (promptUI != null) promptUI.SetActive(nowInRange);
                if (nowInRange)
                {
                    Debug.Log($"[LoadoutStation:{name}] Player in range — press {interactKey}");
                }
                else
                {
                    Debug.Log($"[LoadoutStation:{name}] Player left range");
                    CloseLoadoutUI();
                }
            }
            else if (nowInRange)
            {
                // Keep currentPlayer fresh in case the local player respawned.
                currentPlayer = local;
            }
        }

        /// <summary>
        /// Full close: hides the UI, restores cursor lock, and re-enables
        /// gameplay input on whatever the local player currently is (the old
        /// reference may have been destroyed by a respawn, so re-resolve).
        /// </summary>
        public void CloseLoadoutUI()
        {
            if (loadoutUI != null && loadoutUI.activeSelf) loadoutUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnablePlayerCamera(true);
        }

        private void Update()
        {
            UpdatePlayerInRange();
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
            EnablePlayerCamera(!open);
        }

        /// <summary>
        /// Enables or disables the camera controller so the player can't look
        /// around while the loadout menu is open.
        /// </summary>
        private void EnablePlayerCamera(bool enable)
        {
            // Prefer the stored reference but fall back to the local player in
            // the scene — the stored one may have been destroyed during a
            // character-swap respawn.
            var target = currentPlayer != null ? currentPlayer : FindLocalPlayer();
            if (target == null) return;

            var handler = target.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotionHandler>();
            if (handler == null) return;

            Opsive.Shared.Events.EventHandler.ExecuteEvent(target, "OnEnableGameplayInput", enable);
            Debug.Log($"[LoadoutStation] Gameplay input {(enable ? "enabled" : "disabled")} on '{target.name}'");
        }

        private GameObject FindLocalPlayer()
        {
            var pvs = Object.FindObjectsOfType<Photon.Pun.PhotonView>();
            for (int i = 0; i < pvs.Length; i++)
            {
                if (!pvs[i].IsMine) continue;
                if (pvs[i].GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>() != null)
                {
                    return pvs[i].gameObject;
                }
            }
            return null;
        }
    }
}
