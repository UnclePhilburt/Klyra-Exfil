using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Evidence item that can be collected like in Ready or Not.
/// Tracks collected evidence for mission completion.
/// </summary>
public class Evidence : MonoBehaviourPun
{
    [Header("Evidence Info")]
    public EvidenceType evidenceType = EvidenceType.Weapon;
    public string evidenceName = "Evidence";
    public string description = "Weapon dropped by suspect";

    [Header("Collection Settings")]
    [Tooltip("Interaction distance")]
    public float interactionDistance = 2f;

    [Tooltip("Key to collect evidence")]
    public string collectKey = "g";

    [Header("UI Settings")]
    public bool showPrompt = true;

    [Header("Highlight Settings")]
    public bool highlightWhenDropped = true;
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 2f;

    private Transform player;
    private bool isCollected = false;
    private bool isLocalPlayerNearby = false;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private GUIStyle promptStyle;

    public enum EvidenceType
    {
        Weapon,
        Shell,
        Drug,
        Document,
        Money,
        Other
    }

    void Start()
    {
        Debug.Log($"Evidence Start() on {gameObject.name}, showPrompt={showPrompt}");

        FindPlayer();
        SetupHighlight();

        // Delay UI setup to next frame so properties are set
        StartCoroutine(SetupUIDelayed());

        if (player != null)
        {
            Debug.Log($"Evidence found player: {player.name}");
        }
        else
        {
            Debug.LogWarning($"Evidence could NOT find player!");
        }
    }

    System.Collections.IEnumerator SetupUIDelayed()
    {
        yield return null; // Wait one frame

        Debug.Log($"Setting up UI (delayed), showPrompt={showPrompt}");

        // Setup GUI style
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 18;
        promptStyle.normal.textColor = Color.yellow;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.fontStyle = FontStyle.Bold;

        Debug.Log($"GUI style created for evidence");
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
            }
        }
    }


    void Update()
    {
        if (isCollected) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Show prompt when close
        if (distance <= interactionDistance)
        {
            if (!isLocalPlayerNearby)
            {
                isLocalPlayerNearby = true;
                Debug.Log($"Player is near {gameObject.name}, showing prompt. Distance: {distance}");
            }

            // Check for G key press
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                CollectEvidence();
            }
        }
        else
        {
            if (isLocalPlayerNearby)
            {
                isLocalPlayerNearby = false;
            }
        }
    }

    void OnGUI()
    {
        if (!showPrompt || isCollected || !isLocalPlayerNearby || Camera.main == null) return;

        // Convert world position to screen position
        Vector3 worldPos = transform.position + Vector3.up * 1f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Only show if in front of camera
        if (screenPos.z > 0)
        {
            // Flip Y coordinate (GUI coords are top-left origin)
            screenPos.y = Screen.height - screenPos.y;

            // Draw background box
            Rect bgRect = new Rect(screenPos.x - 150, screenPos.y - 15, 300, 30);
            GUI.Box(bgRect, "");

            // Draw text
            Rect textRect = new Rect(screenPos.x - 150, screenPos.y - 15, 300, 30);
            GUI.Label(textRect, "Press [G] to Collect Evidence", promptStyle);
        }
    }

    void CollectEvidence()
    {
        if (isCollected) return;

        Debug.Log($"Collecting evidence: {evidenceName}");

        // Notify evidence manager
        EvidenceManager.Instance?.RegisterEvidence(this);

        // Sync collection over network
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_CollectEvidence", RpcTarget.All);
        }
        else
        {
            DoCollectEvidence();
        }
    }

    [PunRPC]
    void RPC_CollectEvidence()
    {
        DoCollectEvidence();
    }

    void DoCollectEvidence()
    {
        if (isCollected) return;
        isCollected = true;

        // Remove highlight
        RemoveHighlight();

        // Visual feedback - fade out
        StartCoroutine(FadeOutAndDestroy());
    }

    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeTime);

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    // Fade transparency
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }

            yield return null;
        }

        // Destroy or deactivate
        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupHighlight()
    {
        if (!highlightWhenDropped) return;

        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // Store original materials and create highlight materials
        List<Material> origList = new List<Material>();
        List<Material> highlightList = new List<Material>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                origList.Add(mat);

                // Create highlighted material
                Material highlightMat = new Material(mat);
                highlightMat.EnableKeyword("_EMISSION");
                highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
                highlightList.Add(highlightMat);
            }

            // Apply highlight materials
            renderer.materials = highlightList.ToArray();
        }

        originalMaterials = origList.ToArray();
        highlightMaterials = highlightList.ToArray();
    }

    void RemoveHighlight()
    {
        if (originalMaterials == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        int matIndex = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (matIndex < originalMaterials.Length)
                {
                    mats[i] = originalMaterials[matIndex];
                    matIndex++;
                }
            }
            renderer.materials = mats;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
