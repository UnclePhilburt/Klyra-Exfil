using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Photon.Pun;

/// <summary>
/// Allows player to drag dead bodies around like in Ready or Not.
/// Press G near a corpse to grab and drag it.
/// </summary>
public class BodyDrag : MonoBehaviourPun
{
    [Header("Drag Settings")]
    [Tooltip("Distance to drag body behind player")]
    public float dragDistance = 2f;

    [Tooltip("How fast the body follows the player (0-1, higher = faster)")]
    public float dragSpeed = 0.8f;

    [Tooltip("Distance to detect bodies")]
    public float detectionRange = 2f;

    [Header("UI Settings")]
    public bool showPrompt = true;

    private Canvas promptCanvas;
    private Text promptText;
    private Transform player;
    private bool isPlayerNearby = false;
    private BodyDragger playerDragger;
    private Rigidbody bodyRigidbody;

    private Rigidbody[] allRigidbodies;

    void Start()
    {
        Debug.Log($"BodyDrag component started on {gameObject.name}");

        SetupUI();
        FindPlayer();

        if (player != null)
        {
            Debug.Log($"BodyDrag found player: {player.name}");
        }
        else
        {
            Debug.LogWarning($"BodyDrag could not find player!");
        }

        // Get ALL rigidbodies (ragdoll has multiple)
        allRigidbodies = GetComponentsInChildren<Rigidbody>();
        bodyRigidbody = GetComponent<Rigidbody>();

        Debug.Log($"Found {allRigidbodies.Length} rigidbodies in {gameObject.name}");
    }

    void FindPlayer()
    {
        // Find local player
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                player = p.transform;
                playerDragger = p.GetComponent<BodyDragger>();
                if (playerDragger == null)
                {
                    playerDragger = p.AddComponent<BodyDragger>();
                }
                return;
            }
        }

        // Fallback for single player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerDragger = playerObj.GetComponent<BodyDragger>();
                if (playerDragger == null)
                {
                    playerDragger = playerObj.AddComponent<BodyDragger>();
                }
            }
        }
    }

    void SetupUI()
    {
        if (!showPrompt) return;

        // Create canvas
        GameObject canvasObj = new GameObject("DragPromptCanvas");
        canvasObj.transform.SetParent(transform);

        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Position above body
        RectTransform canvasRT = promptCanvas.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(2, 0.5f);
        canvasRT.localPosition = new Vector3(0, 0.5f, 0);

        // Create text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptCanvas.transform);

        promptText = textObj.AddComponent<Text>();
        promptText.text = "Press [G] to Drag Body";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 14;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);

        RectTransform textRT = promptText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;

        // Start hidden
        promptCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || playerDragger == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Show prompt when close
        if (distance <= detectionRange)
        {
            if (!isPlayerNearby)
            {
                isPlayerNearby = true;
                if (promptCanvas != null)
                {
                    promptCanvas.gameObject.SetActive(true);
                }
            }

            // Make prompt face camera
            if (promptCanvas != null && Camera.main != null)
            {
                promptCanvas.transform.LookAt(Camera.main.transform);
                promptCanvas.transform.Rotate(0, 180, 0);
            }

            // Check for G key press
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                Debug.Log($"G key pressed near {gameObject.name}");

                if (playerDragger.IsDragging() && playerDragger.GetDraggedBody() == this)
                {
                    Debug.Log("Stopping drag");
                    // Stop dragging
                    playerDragger.StopDragging();
                }
                else if (!playerDragger.IsDragging())
                {
                    Debug.Log("Starting drag");
                    // Start dragging
                    playerDragger.StartDragging(this);
                }
                else
                {
                    Debug.LogWarning("Already dragging a different body");
                }
            }
        }
        else
        {
            if (isPlayerNearby)
            {
                isPlayerNearby = false;
                if (promptCanvas != null)
                {
                    promptCanvas.gameObject.SetActive(false);
                }
            }
        }

        // Update prompt text based on drag state
        if (promptText != null && playerDragger != null)
        {
            if (playerDragger.IsDragging() && playerDragger.GetDraggedBody() == this)
            {
                promptText.text = "Press [G] to Release Body";
            }
            else
            {
                promptText.text = "Press [G] to Drag Body";
            }
        }
    }

    public void DragToPosition(Vector3 targetPosition)
    {
        // Simple approach - just lerp the main transform position
        transform.position = Vector3.Lerp(transform.position, targetPosition, dragSpeed);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
