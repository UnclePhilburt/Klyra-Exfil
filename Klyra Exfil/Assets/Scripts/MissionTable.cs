using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.InputSystem;

/// <summary>
/// Mission selection table like Ready or Not.
/// Player walks up and interacts to select a level/mission.
/// </summary>
public class MissionTable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Key to press to interact with table (use f, e, g, etc.)")]
    public string interactKey = "f";

    [Tooltip("Distance player must be to interact")]
    public float interactionDistance = 3f;

    [Tooltip("Show interaction prompt?")]
    public bool showPrompt = true;

    [Header("UI References")]
    [Tooltip("Canvas for mission selection UI (will auto-create if empty)")]
    public Canvas missionCanvas;

    [Tooltip("Panel containing mission buttons (will auto-create if empty)")]
    public GameObject missionPanel;

    [Tooltip("Prompt text showing 'Press E to interact' (will auto-create if empty)")]
    public Text promptText;

    [Header("Missions")]
    [Tooltip("List of available missions")]
    public MissionData[] missions;

    [Header("Audio (Optional)")]
    [Tooltip("Sound when opening mission menu")]
    public AudioClip openMenuSound;

    [Tooltip("Sound when selecting a mission")]
    public AudioClip selectMissionSound;

    private Transform player;
    private bool menuOpen = false;
    private AudioSource audioSource;
    private bool isLocalPlayer = false;

    [System.Serializable]
    public class MissionData
    {
        public string missionName = "Mission Name";
        public string sceneName = "SampleScene";
        public string description = "Mission description here";
        public Sprite thumbnail; // Optional mission image
    }

    void Start()
    {
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Find the local player
        FindLocalPlayer();

        // Setup UI
        SetupUI();

        // Hide menu initially
        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.enabled = false;
        }
    }

    void FindLocalPlayer()
    {
        // Find the local player (for multiplayer)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                player = p.transform;
                isLocalPlayer = true;
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
                isLocalPlayer = true;
            }
        }

        // Last resort - find camera
        if (player == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
                isLocalPlayer = true;
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Show prompt when close enough
        if (distance <= interactionDistance && !menuOpen)
        {
            if (showPrompt && promptText != null)
            {
                promptText.enabled = true;
            }

            // Check for interact key (New Input System)
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenMenu();
            }
        }
        else
        {
            if (promptText != null)
            {
                promptText.enabled = false;
            }
        }

        // Close menu with Escape (New Input System)
        if (menuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }

    void SetupUI()
    {
        // Create canvas if needed
        if (missionCanvas == null)
        {
            GameObject canvasObj = new GameObject("MissionSelectionCanvas");
            canvasObj.transform.SetParent(transform);

            missionCanvas = canvasObj.AddComponent<Canvas>();
            missionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            missionCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create prompt text if needed
        if (promptText == null && showPrompt)
        {
            GameObject promptObj = new GameObject("InteractPrompt");
            promptObj.transform.SetParent(missionCanvas.transform);

            promptText = promptObj.AddComponent<Text>();
            promptText.text = $"Press [{interactKey}] to Select Mission";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 24;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;

            // Add outline for visibility
            Outline outline = promptObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);

            RectTransform rt = promptText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.2f);
            rt.anchorMax = new Vector2(0.5f, 0.2f);
            rt.sizeDelta = new Vector2(400, 50);
            rt.anchoredPosition = Vector2.zero;

            promptText.enabled = false;
        }

        // Create mission panel if needed
        if (missionPanel == null)
        {
            CreateMissionPanel();
        }
    }

    void CreateMissionPanel()
    {
        // Create panel background
        GameObject panelObj = new GameObject("MissionPanel");
        panelObj.transform.SetParent(missionCanvas.transform);

        missionPanel = panelObj;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.9f);

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.2f, 0.2f);
        panelRT.anchorMax = new Vector2(0.8f, 0.8f);
        panelRT.sizeDelta = Vector2.zero;
        panelRT.anchoredPosition = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "SELECT MISSION";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperCenter;

        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.sizeDelta = new Vector2(-40, 60);
        titleRT.anchoredPosition = new Vector2(0, -30);

        // Create mission buttons
        if (missions != null && missions.Length > 0)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                CreateMissionButton(panelObj.transform, missions[i], i);
            }
        }

        // Close button
        CreateCloseButton(panelObj.transform);
    }

    void CreateMissionButton(Transform parent, MissionData mission, int index)
    {
        // Button
        GameObject buttonObj = new GameObject($"Mission_{index}_Button");
        buttonObj.transform.SetParent(parent);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.1f, 0.7f - (index * 0.15f));
        buttonRT.anchorMax = new Vector2(0.9f, 0.85f - (index * 0.15f));
        buttonRT.sizeDelta = Vector2.zero;
        buttonRT.anchoredPosition = Vector2.zero;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = mission.missionName;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 28;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleLeft;

        RectTransform textRT = buttonText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = new Vector2(-20, 0);
        textRT.anchoredPosition = new Vector2(10, 0);

        // Description (smaller text)
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(buttonObj.transform);

        Text descText = descObj.AddComponent<Text>();
        descText.text = mission.description;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 16;
        descText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        descText.alignment = TextAnchor.LowerLeft;

        RectTransform descRT = descText.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero;
        descRT.anchorMax = Vector2.one;
        descRT.sizeDelta = new Vector2(-20, -10);
        descRT.anchoredPosition = new Vector2(10, 5);

        // Add button click event
        button.onClick.AddListener(() => LoadMission(mission));

        // Hover effect
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        button.colors = colors;
    }

    void CreateCloseButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("CloseButton");
        buttonObj.transform.SetParent(parent);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = new Color(0.5f, 0, 0, 1f);

        RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.4f, 0.05f);
        buttonRT.anchorMax = new Vector2(0.6f, 0.15f);
        buttonRT.sizeDelta = Vector2.zero;
        buttonRT.anchoredPosition = Vector2.zero;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "CLOSE [ESC]";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRT = buttonText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;

        button.onClick.AddListener(CloseMenu);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.7f, 0, 0, 1f);
        colors.pressedColor = new Color(0.9f, 0, 0, 1f);
        button.colors = colors;
    }

    public void OpenMenu()
    {
        menuOpen = true;

        if (missionPanel != null)
        {
            missionPanel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.enabled = false;
        }

        // Play sound
        if (openMenuSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openMenuSound);
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player input while menu is open
        if (player != null)
        {
            var characterLocomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (characterLocomotion != null)
            {
                characterLocomotion.enabled = false;
            }

            var cameraController = Camera.main?.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                cameraController.enabled = false;
            }
        }

        Debug.Log("Mission table menu opened");
    }

    public void CloseMenu()
    {
        menuOpen = false;

        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player input
        if (player != null)
        {
            var characterLocomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (characterLocomotion != null)
            {
                characterLocomotion.enabled = true;
            }

            var cameraController = Camera.main?.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                cameraController.enabled = true;
            }
        }

        Debug.Log("Mission table menu closed");
    }

    public void LoadMission(MissionData mission)
    {
        Debug.Log($"Loading mission: {mission.missionName} (Scene: {mission.sceneName})");

        // Play sound
        if (selectMissionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(selectMissionSound);
        }

        // Load scene
        if (PhotonNetwork.IsConnected)
        {
            // For multiplayer, only master client loads
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(mission.sceneName);
            }
            else
            {
                Debug.LogWarning("Only the master client can start missions!");
            }
        }
        else
        {
            // Single player
            SceneManager.LoadScene(mission.sceneName);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize interaction range in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
