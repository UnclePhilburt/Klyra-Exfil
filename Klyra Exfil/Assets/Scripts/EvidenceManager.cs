using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Tracks collected evidence throughout the mission.
/// Shows evidence count on UI like Ready or Not.
/// </summary>
public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    [Header("Evidence Tracking")]
    public int totalEvidenceInLevel = 0; // Set this manually or auto-detect
    public bool autoDetectEvidence = true;

    [Header("UI Settings")]
    public bool showEvidenceCounter = true;
    public Color counterColor = Color.white;
    public int fontSize = 20;

    private List<Evidence> collectedEvidence = new List<Evidence>();
    private Canvas evidenceCanvas;
    private Text evidenceText;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupUI();
    }

    void Start()
    {
        if (autoDetectEvidence)
        {
            DetectTotalEvidence();
        }

        UpdateUI();
    }

    void DetectTotalEvidence()
    {
        // Find all evidence in scene
        Evidence[] allEvidence = FindObjectsOfType<Evidence>();
        totalEvidenceInLevel = allEvidence.Length;

        Debug.Log($"EvidenceManager: Detected {totalEvidenceInLevel} pieces of evidence in level");
    }

    void SetupUI()
    {
        if (!showEvidenceCounter) return;

        // Create canvas
        GameObject canvasObj = new GameObject("EvidenceCounterCanvas");
        canvasObj.transform.SetParent(transform);

        evidenceCanvas = canvasObj.AddComponent<Canvas>();
        evidenceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        evidenceCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create text
        GameObject textObj = new GameObject("EvidenceCounter");
        textObj.transform.SetParent(evidenceCanvas.transform);

        evidenceText = textObj.AddComponent<Text>();
        evidenceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        evidenceText.fontSize = fontSize;
        evidenceText.color = counterColor;
        evidenceText.alignment = TextAnchor.UpperLeft;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);

        // Position in top right
        RectTransform rt = evidenceText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(300, 50);
    }

    public void RegisterEvidence(Evidence evidence)
    {
        if (collectedEvidence.Contains(evidence))
        {
            Debug.LogWarning($"Evidence {evidence.evidenceName} already collected!");
            return;
        }

        collectedEvidence.Add(evidence);

        Debug.Log($"Evidence collected: {evidence.evidenceName} ({collectedEvidence.Count}/{totalEvidenceInLevel})");

        UpdateUI();

        // Check if all evidence collected
        if (collectedEvidence.Count >= totalEvidenceInLevel && totalEvidenceInLevel > 0)
        {
            OnAllEvidenceCollected();
        }
    }

    void UpdateUI()
    {
        if (evidenceText == null) return;

        evidenceText.text = $"EVIDENCE: {collectedEvidence.Count}/{totalEvidenceInLevel}";
    }

    void OnAllEvidenceCollected()
    {
        Debug.Log("ALL EVIDENCE COLLECTED!");

        // Show completion message
        if (evidenceText != null)
        {
            evidenceText.color = Color.green;
            evidenceText.text = $"EVIDENCE: {collectedEvidence.Count}/{totalEvidenceInLevel} - COMPLETE!";
        }

        // You could trigger mission completion here
        // MissionManager.Instance?.CompleteObjective("CollectAllEvidence");
    }

    public int GetCollectedCount()
    {
        return collectedEvidence.Count;
    }

    public int GetTotalCount()
    {
        return totalEvidenceInLevel;
    }

    public bool IsAllEvidenceCollected()
    {
        return collectedEvidence.Count >= totalEvidenceInLevel && totalEvidenceInLevel > 0;
    }

    public List<Evidence> GetCollectedEvidence()
    {
        return new List<Evidence>(collectedEvidence);
    }

    // Call this when loading a new level
    public void ResetEvidence()
    {
        collectedEvidence.Clear();
        totalEvidenceInLevel = 0;

        if (autoDetectEvidence)
        {
            DetectTotalEvidence();
        }

        UpdateUI();
    }
}
