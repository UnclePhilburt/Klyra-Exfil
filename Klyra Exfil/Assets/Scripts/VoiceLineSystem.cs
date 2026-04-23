using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Photon.Pun;

/// <summary>
/// Voice line system for tactical commands like Ready or Not.
/// Press a key to trigger voice lines that play for nearby players.
/// </summary>
public class VoiceLineSystem : MonoBehaviourPun
{
    [Header("Voice Line Settings")]
    [Tooltip("Key to open voice line menu (default V)")]
    public string voiceKey = "v";

    [Tooltip("Hold time before menu opens (seconds)")]
    public float holdTime = 0.25f;

    [Tooltip("Audio range for voice lines (meters)")]
    public float audioRange = 20f;

    [Header("Voice Lines")]
    [Tooltip("List of available voice lines")]
    public VoiceLine[] voiceLines;

    [Header("UI Settings")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
    public Color optionNormalColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    public Color optionHoverColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color textColor = Color.white;

    private bool isMenuOpen = false;
    private float holdTimer = 0f;
    private int selectedOption = -1;
    private bool hasExecuted = false;
    private AudioSource audioSource;
    private GUIStyle labelStyle;
    private bool isLocalPlayer = false;

    [System.Serializable]
    public class VoiceLine
    {
        public string name = "Get Down!";
        public AudioClip audioClip;
        [TextArea(2, 4)]
        public string subtitleText = "GET ON THE FUCKING GROUND!";
        public float subtitleDuration = 2f;
    }

    void Start()
    {
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D audio
        audioSource.maxDistance = audioRange;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // Check if this is the local player
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            isLocalPlayer = pv.IsMine;
        }
        else
        {
            isLocalPlayer = true; // Single player
        }

        // Setup default voice lines if none assigned
        if (voiceLines == null || voiceLines.Length == 0)
        {
            voiceLines = new VoiceLine[]
            {
                new VoiceLine { name = "GET DOWN", subtitleText = "GET ON THE FUCKING GROUND!" },
                new VoiceLine { name = "HANDS UP", subtitleText = "HANDS UP! NOW!" },
                new VoiceLine { name = "DON'T MOVE", subtitleText = "DON'T FUCKING MOVE!" },
                new VoiceLine { name = "POLICE", subtitleText = "POLICE! GET DOWN!" },
                new VoiceLine { name = "CLEAR", subtitleText = "CLEAR!" },
                new VoiceLine { name = "MOVING UP", subtitleText = "MOVING UP!" }
            };
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        bool isHoldingKey = false;

        // Check if voice key is held
        if (Keyboard.current != null)
        {
            var key = Keyboard.current[voiceKey];
            if (key is UnityEngine.InputSystem.Controls.ButtonControl button)
            {
                isHoldingKey = button.isPressed;
            }
        }

        if (isHoldingKey)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= holdTime && !isMenuOpen)
            {
                OpenMenu();
            }

            if (isMenuOpen)
            {
                UpdateSelection();
            }
        }
        else
        {
            if (isMenuOpen && !hasExecuted)
            {
                ExecuteSelection();
                hasExecuted = true;
                CloseMenu();
            }
            else if (holdTimer > 0f && holdTimer < holdTime && !hasExecuted)
            {
                // Quick tap - play first voice line
                PlayVoiceLine(0);
                hasExecuted = true;
            }

            holdTimer = 0f;
            if (!isMenuOpen)
            {
                hasExecuted = false;
            }
        }
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        selectedOption = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void CloseMenu()
    {
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExecuteSelection()
    {
        if (selectedOption >= 0 && selectedOption < voiceLines.Length && !hasExecuted)
        {
            PlayVoiceLine(selectedOption);
        }
    }

    void UpdateSelection()
    {
        if (!isMenuOpen || voiceLines == null || voiceLines.Length == 0) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePos = Mouse.current.position.ReadValue();

        float mouseY = mousePos.y;
        float screenHeight = Screen.height;

        // Divide screen into sections based on number of options
        float sectionHeight = screenHeight / voiceLines.Length;

        selectedOption = Mathf.Clamp((int)(mouseY / sectionHeight), 0, voiceLines.Length - 1);
    }

    void PlayVoiceLine(int index)
    {
        if (index < 0 || index >= voiceLines.Length) return;

        VoiceLine voiceLine = voiceLines[index];

        Debug.Log($"Playing voice line: {voiceLine.name}");

        // Play audio over network
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_PlayVoiceLine", RpcTarget.All, index);
        }
        else
        {
            DoPlayVoiceLine(index);
        }
    }

    [PunRPC]
    void RPC_PlayVoiceLine(int index)
    {
        DoPlayVoiceLine(index);
    }

    void DoPlayVoiceLine(int index)
    {
        if (index < 0 || index >= voiceLines.Length) return;

        VoiceLine voiceLine = voiceLines[index];

        // Play audio
        if (voiceLine.audioClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(voiceLine.audioClip);
        }

        // Show subtitle (only for local player or nearby players)
        if (isLocalPlayer || Vector3.Distance(transform.position, Camera.main.transform.position) < audioRange)
        {
            SubtitleManager.Instance?.ShowSubtitle(voiceLine.subtitleText, voiceLine.subtitleDuration);
        }

        // Notify nearby AI enemies
        NotifyNearbyAI(voiceLine.name);
    }

    void NotifyNearbyAI(string command)
    {
        // Find all AI enemies
        TacticalAI[] aiEnemies = FindObjectsOfType<TacticalAI>();

        foreach (TacticalAI ai in aiEnemies)
        {
            ai.OnVoiceCommandHeard(transform.position, command);
        }
    }

    void OnGUI()
    {
        if (!isMenuOpen || !isLocalPlayer || voiceLines == null || voiceLines.Length == 0) return;

        // Setup style
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle();
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontSize = 24;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = textColor;
        }

        // Dark overlay
        DrawFullScreenOverlay(new Color(0f, 0f, 0f, 0.7f));

        // Background panel
        float panelWidth = 400f;
        float panelHeight = Screen.height * 0.6f;
        float panelX = Screen.width / 2f - panelWidth / 2f;
        float panelY = Screen.height / 2f - panelHeight / 2f;

        DrawBox(new Rect(panelX, panelY, panelWidth, panelHeight), backgroundColor);

        // Title
        GUIStyle titleStyle = new GUIStyle(labelStyle);
        titleStyle.fontSize = 32;
        titleStyle.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(panelX, panelY + 20, panelWidth, 40), "VOICE COMMANDS", titleStyle);

        // Voice line options
        float optionHeight = 60f;
        float spacing = 10f;
        float startY = panelY + 80f;

        for (int i = 0; i < voiceLines.Length; i++)
        {
            bool isSelected = (i == selectedOption);
            Color optionColor = isSelected ? optionHoverColor : optionNormalColor;

            float optionY = startY + (i * (optionHeight + spacing));
            Rect optionRect = new Rect(panelX + 20, optionY, panelWidth - 40, optionHeight);

            DrawBox(optionRect, optionColor);

            // Text
            GUIStyle textStyle = new GUIStyle(labelStyle);
            textStyle.fontSize = isSelected ? 26 : 22;
            textStyle.normal.textColor = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.9f, 1f);

            Rect textRect = new Rect(optionRect.x + 20, optionRect.y, optionRect.width - 20, optionRect.height);
            GUI.Label(textRect, voiceLines[i].name, textStyle);
        }

        // Bottom instruction
        GUIStyle instructStyle = new GUIStyle(labelStyle);
        instructStyle.fontSize = 18;
        instructStyle.alignment = TextAnchor.LowerCenter;
        instructStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f, 1f);
        GUI.Label(new Rect(panelX, panelY + panelHeight - 40, panelWidth, 30), $"Release [{voiceKey.ToUpper()}] to Confirm", instructStyle);
    }

    void DrawFullScreenOverlay(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tex);
    }

    void DrawBox(Rect rect, Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        GUI.DrawTexture(rect, tex);
    }

    public bool IsMenuOpen() => isMenuOpen;
}
