using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

/// <summary>
/// Handles flashbang visual and audio effects on the local player.
/// Attach this to the player's camera or UI canvas.
/// </summary>
public class FlashbangEffect : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Canvas for the flash overlay (will be created automatically if not set)")]
    public Canvas flashCanvas;

    [Tooltip("White image that covers the screen (will be created automatically if not set)")]
    public Image flashImage;

    [Header("Effect Settings")]
    [Tooltip("Color of the flash effect")]
    public Color flashColor = Color.white;

    [Tooltip("Curve controlling how the flash fades (X=time 0-1, Y=opacity 0-1)")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("Should ringing sound play when flashed?")]
    public bool enableRingingSound = true;

    [Tooltip("Ringing/tinnitus sound effect")]
    public AudioClip ringingSound;

    [Header("Audio Muffling")]
    [Tooltip("How much to muffle game audio when deafened (0=normal, 1=silent)")]
    [Range(0f, 1f)]
    public float audioMuffleAmount = 0.7f;

    private AudioSource audioSource;
    private bool isFlashed = false;
    private Coroutine flashCoroutine;
    private AudioListener audioListener;

    void Awake()
    {
        // Only create UI for local player
        if (!IsLocalPlayer())
        {
            enabled = false;
            return;
        }

        SetupUI();
        SetupAudio();
    }

    void Start()
    {
        // Find audio listener (usually on camera)
        if (audioListener == null)
        {
            audioListener = Camera.main?.GetComponent<AudioListener>();
            if (audioListener == null)
            {
                audioListener = FindObjectOfType<AudioListener>();
            }
        }
    }

    /// <summary>
    /// Check if this is the local player
    /// </summary>
    bool IsLocalPlayer()
    {
        // Check if this is networked
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv != null)
        {
            return pv.IsMine;
        }

        // If not networked, assume it's local
        return true;
    }

    /// <summary>
    /// Setup the flash overlay UI
    /// </summary>
    void SetupUI()
    {
        // Create canvas if needed
        if (flashCanvas == null)
        {
            GameObject canvasObj = new GameObject("FlashbangCanvas");
            canvasObj.transform.SetParent(transform);

            flashCanvas = canvasObj.AddComponent<Canvas>();
            flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flashCanvas.sortingOrder = 9999; // Render on top of everything

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create white image if needed
        if (flashImage == null)
        {
            GameObject imageObj = new GameObject("FlashOverlay");
            imageObj.transform.SetParent(flashCanvas.transform);

            flashImage = imageObj.AddComponent<Image>();
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

            RectTransform rt = flashImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        // Start with no flash
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    /// <summary>
    /// Setup audio for ringing effect
    /// </summary>
    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    /// <summary>
    /// Apply flashbang effect to this player
    /// </summary>
    /// <param name="flashDuration">How long the white screen lasts</param>
    /// <param name="deafenDuration">How long audio is muffled</param>
    public void Flash(float flashDuration, float deafenDuration)
    {
        // Stop any existing flash
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashCoroutine(flashDuration, deafenDuration));
    }

    IEnumerator FlashCoroutine(float flashDuration, float deafenDuration)
    {
        isFlashed = true;

        Debug.Log($"Player flashed! Duration: {flashDuration}s, Deafen: {deafenDuration}s");

        // Play ringing sound
        if (enableRingingSound && ringingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(ringingSound);
        }

        // Muffle game audio
        if (audioListener != null)
        {
            audioListener.enabled = false;
            yield return new WaitForEndOfFrame();
            audioListener.enabled = true;
            AudioListener.volume = 1f - audioMuffleAmount;
        }

        // Flash effect
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            // Use animation curve for smooth fade
            float opacity = fadeCurve.Evaluate(t);

            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, opacity);

            yield return null;
        }

        // Ensure fully faded
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

        // Wait for deafen to finish
        if (deafenDuration > flashDuration)
        {
            yield return new WaitForSeconds(deafenDuration - flashDuration);
        }

        // Restore audio
        if (audioListener != null)
        {
            AudioListener.volume = 1f;
        }

        isFlashed = false;
        Debug.Log("Flashbang effect ended");
    }

    /// <summary>
    /// Check if player is currently flashed
    /// </summary>
    public bool IsFlashed()
    {
        return isFlashed;
    }

    void OnDestroy()
    {
        // Restore audio on cleanup
        if (audioListener != null)
        {
            AudioListener.volume = 1f;
        }
    }
}
