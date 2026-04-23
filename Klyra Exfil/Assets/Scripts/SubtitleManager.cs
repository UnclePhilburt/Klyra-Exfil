using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple subtitle system for voice lines.
/// Shows text at the bottom of the screen.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    [Header("Subtitle Settings")]
    public Color subtitleColor = Color.white;
    public int fontSize = 24;
    public float fadeTime = 0.3f;

    private Canvas subtitleCanvas;
    private Text subtitleText;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("SubtitleCanvas");
        canvasObj.transform.SetParent(transform);

        subtitleCanvas = canvasObj.AddComponent<Canvas>();
        subtitleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        subtitleCanvas.sortingOrder = 200; // High priority

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create subtitle text
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(subtitleCanvas.transform);

        subtitleText = textObj.AddComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = fontSize;
        subtitleText.color = subtitleColor;
        subtitleText.alignment = TextAnchor.LowerCenter;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        // Add outline for readability
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, 3);

        // Position at bottom center
        RectTransform rt = subtitleText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.1f);
        rt.anchorMax = new Vector2(0.8f, 0.25f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Start hidden
        subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 0f);
    }

    public void ShowSubtitle(string text, float duration)
    {
        if (subtitleText == null) return;

        // Stop any existing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Show subtitle
        subtitleText.text = text;
        fadeCoroutine = StartCoroutine(ShowSubtitleCoroutine(duration));
    }

    IEnumerator ShowSubtitleCoroutine(float duration)
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, alpha);
            yield return null;
        }

        // Show full
        subtitleText.color = subtitleColor;

        // Wait
        yield return new WaitForSeconds(duration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, alpha);
            yield return null;
        }

        // Hide
        subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 0f);
        subtitleText.text = "";
    }
}
