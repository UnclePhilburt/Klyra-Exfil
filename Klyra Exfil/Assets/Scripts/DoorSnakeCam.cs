using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Snake cam that lets players peek under doors like Ready or Not.
/// Shows a low-angle camera view when near a door.
/// </summary>
public class DoorSnakeCam : MonoBehaviour
{
    [Header("Snake Cam Settings")]
    [Tooltip("Distance player must be from door to use snake cam")]
    public float interactionDistance = 2f;

    [Tooltip("Height of snake cam (how low under the door)")]
    public float camHeight = 0.1f;

    [Tooltip("Distance snake cam peeks forward")]
    public float peekDistance = 1f;

    [Tooltip("Field of view for snake cam")]
    public float snakeCamFOV = 90f;

    [Tooltip("Mouse sensitivity for snake cam look")]
    public float mouseSensitivity = 2f;

    [Tooltip("Max vertical look angle")]
    public float maxVerticalAngle = 60f;

    [Header("UI")]
    [Tooltip("Show prompt to use snake cam?")]
    public bool showPrompt = true;

    [Tooltip("Canvas for snake cam UI (will auto-create if empty)")]
    public Canvas snakeCamCanvas;

    [Tooltip("RawImage to show snake cam feed")]
    public RawImage snakeCamDisplay;

    [Tooltip("Prompt text (will auto-create if empty)")]
    public Text promptText;

    [Header("Audio (Optional)")]
    [Tooltip("Sound when deploying snake cam")]
    public AudioClip deploySound;

    [Tooltip("Sound when retracting snake cam")]
    public AudioClip retractSound;

    private Camera snakeCam;
    private RenderTexture snakeCamRT;
    private Transform player;
    private Camera playerCamera;
    private bool isUsingSnakeCam = false;
    private AudioSource audioSource;
    private Vector3 snakeCamPosition;
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Debug.Log("DoorSnakeCam: Starting setup on " + gameObject.name);

        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }

        // Find player
        FindPlayer();

        if (player != null)
        {
            Debug.Log("DoorSnakeCam: Player found at " + player.name);
        }
        else
        {
            Debug.LogError("DoorSnakeCam: Could not find player!");
        }

        // Setup snake cam
        SetupSnakeCam();

        // Setup UI
        SetupUI();

        Debug.Log("DoorSnakeCam: Setup complete");
    }

    void FindPlayer()
    {
        // Find player GameObject (root character, not camera)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCamera = player.GetComponentInChildren<Camera>();
            Debug.Log($"DoorSnakeCam: Found player at {player.name}");
            return;
        }

        // Fallback - find by camera
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            // Try to find character root
            var characterLocomotion = playerCamera.GetComponentInParent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (characterLocomotion != null)
            {
                player = characterLocomotion.transform;
                Debug.Log($"DoorSnakeCam: Found player via camera at {player.name}");
            }
            else
            {
                player = playerCamera.transform;
                Debug.LogWarning("DoorSnakeCam: Using camera position - distance check may not work correctly");
            }
        }
    }

    void SetupSnakeCam()
    {
        // Create snake cam camera
        GameObject camObj = new GameObject("SnakeCam");
        camObj.transform.SetParent(transform);

        snakeCam = camObj.AddComponent<Camera>();
        snakeCam.fieldOfView = snakeCamFOV;
        snakeCam.enabled = false; // Start disabled
        snakeCam.depth = 10; // Render on top

        // Calculate position (low to ground, forward from door - NEGATIVE to go through to other side)
        snakeCamPosition = transform.position + (-transform.forward * peekDistance);
        snakeCamPosition.y = transform.position.y + camHeight;

        camObj.transform.position = snakeCamPosition;

        // Point camera forward and slightly down to see under door
        Vector3 lookDirection = transform.forward;
        camObj.transform.rotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(10, 0, 0); // Tilt down 10 degrees

        // Create render texture
        snakeCamRT = new RenderTexture(512, 512, 16);
        snakeCam.targetTexture = snakeCamRT;
    }

    void SetupUI()
    {
        // Create canvas if needed
        if (snakeCamCanvas == null)
        {
            GameObject canvasObj = new GameObject("SnakeCamCanvas");
            canvasObj.transform.SetParent(transform);

            snakeCamCanvas = canvasObj.AddComponent<Canvas>();
            snakeCamCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            snakeCamCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create snake cam display
        if (snakeCamDisplay == null)
        {
            GameObject displayObj = new GameObject("SnakeCamFeed");
            displayObj.transform.SetParent(snakeCamCanvas.transform);

            snakeCamDisplay = displayObj.AddComponent<RawImage>();
            snakeCamDisplay.texture = snakeCamRT;
            snakeCamDisplay.color = Color.white;

            // Position in corner or center
            RectTransform rt = snakeCamDisplay.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.3f);
            rt.anchorMax = new Vector2(0.7f, 0.7f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // Add border
            Outline outline = displayObj.AddComponent<Outline>();
            outline.effectColor = Color.green;
            outline.effectDistance = new Vector2(3, 3);

            snakeCamDisplay.gameObject.SetActive(false);
        }

        // Create prompt text
        if (promptText == null && showPrompt)
        {
            GameObject promptObj = new GameObject("SnakeCamPrompt");
            promptObj.transform.SetParent(snakeCamCanvas.transform);

            promptText = promptObj.AddComponent<Text>();
            promptText.text = "Press [G] to Use Snake Cam";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 20;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;

            Outline outline = promptObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);

            RectTransform rt = promptText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.3f);
            rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.sizeDelta = new Vector2(400, 40);
            rt.anchoredPosition = Vector2.zero;

            promptText.enabled = false;
        }

        // Instructions when using snake cam
        GameObject instructObj = new GameObject("SnakeCamInstructions");
        instructObj.transform.SetParent(snakeCamCanvas.transform);

        Text instructText = instructObj.AddComponent<Text>();
        instructText.text = "Press [G] to Close Snake Cam";
        instructText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructText.fontSize = 18;
        instructText.color = Color.green;
        instructText.alignment = TextAnchor.UpperCenter;

        Outline instructOutline = instructObj.AddComponent<Outline>();
        instructOutline.effectColor = Color.black;
        instructOutline.effectDistance = new Vector2(2, 2);

        RectTransform instructRT = instructText.GetComponent<RectTransform>();
        instructRT.anchorMin = new Vector2(0.5f, 0.75f);
        instructRT.anchorMax = new Vector2(0.5f, 0.75f);
        instructRT.sizeDelta = new Vector2(400, 40);
        instructRT.anchoredPosition = Vector2.zero;

        instructText.gameObject.SetActive(false);

        // Store reference
        instructText.name = "Instructions";
    }

    void Update()
    {
        // Close snake cam if player presses movement keys or Escape
        if (isUsingSnakeCam)
        {
            // Check for movement keys (WASD)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame ||
                    Keyboard.current.aKey.wasPressedThisFrame ||
                    Keyboard.current.sKey.wasPressedThisFrame ||
                    Keyboard.current.dKey.wasPressedThisFrame)
                {
                    Debug.Log("Movement key pressed - closing snake cam");
                    DeactivateSnakeCam();
                    return;
                }

                // Check for Escape key
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Debug.Log("Escape pressed - closing snake cam");
                    DeactivateSnakeCam();
                    return;
                }
            }
        }

        // Mouse look when snake cam is active
        if (isUsingSnakeCam && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationX += mouseDelta.x * mouseSensitivity * 0.1f;
            rotationY -= mouseDelta.y * mouseSensitivity * 0.1f;

            // Clamp vertical rotation
            rotationY = Mathf.Clamp(rotationY, -maxVerticalAngle, maxVerticalAngle);

            // Apply rotation to snake cam
            if (snakeCam != null)
            {
                snakeCam.transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
            }
        }
    }

    /// <summary>
    /// Toggle snake cam on/off (called from radial menu)
    /// </summary>
    public void ToggleSnakeCam()
    {
        if (isUsingSnakeCam)
        {
            DeactivateSnakeCam();
        }
        else
        {
            ActivateSnakeCam();
        }
    }

    void ActivateSnakeCam()
    {
        isUsingSnakeCam = true;

        // Reset rotation - face AWAY from door (180 degrees from door's forward)
        rotationX = 180f; // Turn around
        rotationY = 10f; // Start looking slightly down

        // Disable player camera (but keep movement enabled so they can walk away)
        if (player != null)
        {
            var cameraController = playerCamera?.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                cameraController.enabled = false;
            }
        }

        // Enable snake cam
        if (snakeCam != null)
        {
            snakeCam.enabled = true;
            snakeCam.transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
        }

        // Show display
        if (snakeCamDisplay != null)
        {
            snakeCamDisplay.gameObject.SetActive(true);
        }

        // Show instructions
        Transform instructions = snakeCamCanvas.transform.Find("Instructions");
        if (instructions != null)
        {
            instructions.gameObject.SetActive(true);
        }

        // Hide prompt
        if (promptText != null)
        {
            promptText.enabled = false;
        }

        // Play sound
        if (deploySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deploySound);
        }

        Debug.Log("Snake cam activated");
    }

    void DeactivateSnakeCam()
    {
        isUsingSnakeCam = false;

        // Re-enable player camera
        if (player != null)
        {
            var cameraController = playerCamera?.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                cameraController.enabled = true;
            }
        }

        // Disable snake cam
        if (snakeCam != null)
        {
            snakeCam.enabled = false;
        }

        // Hide display
        if (snakeCamDisplay != null)
        {
            snakeCamDisplay.gameObject.SetActive(false);
        }

        // Hide instructions
        Transform instructions = snakeCamCanvas.transform.Find("Instructions");
        if (instructions != null)
        {
            instructions.gameObject.SetActive(false);
        }

        // Play sound
        if (retractSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(retractSound);
        }

        Debug.Log("Snake cam deactivated");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Visualize snake cam position
        Vector3 camPos = transform.position + (transform.forward * peekDistance);
        camPos.y = transform.position.y + camHeight;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(camPos, 0.1f);
        Gizmos.DrawLine(transform.position, camPos);
    }

    void OnDestroy()
    {
        // Cleanup render texture
        if (snakeCamRT != null)
        {
            snakeCamRT.Release();
            Destroy(snakeCamRT);
        }
    }
}
