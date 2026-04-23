using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Flashbang grenade that blinds and deafens players in radius.
/// Can be thrown like a standard grenade using Opsive's throwable system.
/// </summary>
public class FlashbangGrenade : MonoBehaviourPun
{
    [Header("Flashbang Settings")]
    [Tooltip("Detonation delay after landing (seconds)")]
    public float fuseTime = 1.5f;

    [Tooltip("Radius in which players are affected")]
    public float effectRadius = 10f;

    [Tooltip("Maximum flash duration for players directly looking at it")]
    public float maxFlashDuration = 8f;

    [Tooltip("Minimum flash duration for players at edge of radius")]
    public float minFlashDuration = 3f;

    [Tooltip("Maximum deafen duration")]
    public float maxDeafenDuration = 4f;

    [Tooltip("Layer mask for line-of-sight checks (walls block flashbang)")]
    public LayerMask obstacleMask = -1;

    [Header("Visual Effects")]
    [Tooltip("Flash effect prefab (bright light)")]
    public GameObject flashEffect;

    [Tooltip("Light component for the flash")]
    public Light flashLight;

    [Tooltip("Flash light duration")]
    public float flashLightDuration = 0.3f;

    [Tooltip("Flash light intensity")]
    public float flashLightIntensity = 10f;

    [Header("Audio")]
    [Tooltip("Flashbang bang sound")]
    public AudioClip bangSound;

    [Tooltip("Pin pull sound (optional)")]
    public AudioClip pinSound;

    private AudioSource audioSource;
    private bool hasDetonated = false;
    private float timeActive = 0f;
    private bool fuseStarted = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 50f;
        }

        // Play pin pull sound
        if (pinSound != null)
        {
            audioSource.PlayOneShot(pinSound);
        }

        StartFuse();
    }

    void Update()
    {
        if (fuseStarted && !hasDetonated)
        {
            timeActive += Time.deltaTime;

            if (timeActive >= fuseTime)
            {
                Detonate();
            }
        }
    }

    /// <summary>
    /// Start the fuse timer
    /// </summary>
    public void StartFuse()
    {
        fuseStarted = true;
        timeActive = 0f;
        Debug.Log($"Flashbang fuse started! Detonating in {fuseTime}s");
    }

    /// <summary>
    /// Detonate the flashbang
    /// </summary>
    public void Detonate()
    {
        if (hasDetonated) return;

        Debug.Log("BANG! Flashbang detonated!");

        // Sync detonation over network
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_Detonate", RpcTarget.All);
        }
        else
        {
            DoDetonate();
        }
    }

    [PunRPC]
    void RPC_Detonate()
    {
        DoDetonate();
    }

    void DoDetonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;

        // Play bang sound
        if (bangSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bangSound);
        }

        // Spawn flash effect
        if (flashEffect != null)
        {
            Instantiate(flashEffect, transform.position, Quaternion.identity);
        }

        // Flash light
        if (flashLight != null)
        {
            StartCoroutine(FlashLightEffect());
        }

        // Find all players in radius and flash them
        FlashNearbyPlayers();

        // Destroy grenade after a delay (let audio finish)
        StartCoroutine(DestroyAfterDelay(2f));
    }

    IEnumerator FlashLightEffect()
    {
        flashLight.intensity = flashLightIntensity;
        flashLight.enabled = true;
        yield return new WaitForSeconds(flashLightDuration);
        flashLight.enabled = false;
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Flash all nearby players based on distance and line of sight
    /// </summary>
    void FlashNearbyPlayers()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius);

        // Dedupe: characters have many colliders (ragdoll bones) and we only
        // want to flash each one once.
        var flashedPlayers = new System.Collections.Generic.HashSet<FlashbangEffect>();
        var flashedAI = new System.Collections.Generic.HashSet<TacticalAI>();

        foreach (Collider col in colliders)
        {
            // Walk the hierarchy up and down so bone colliders still resolve
            // to the root component. Same pattern for both player and AI.
            FlashbangEffect flashEffect = col.GetComponentInParent<FlashbangEffect>();
            if (flashEffect == null) flashEffect = col.GetComponentInChildren<FlashbangEffect>();

            TacticalAI aiEnemy = col.GetComponentInParent<TacticalAI>();
            if (aiEnemy == null) aiEnemy = col.GetComponentInChildren<TacticalAI>();

            if (flashEffect != null && flashedPlayers.Add(flashEffect))
            {
                Vector3 toTarget = flashEffect.transform.position - transform.position;
                if (Physics.Raycast(transform.position, toTarget.normalized, toTarget.magnitude, obstacleMask))
                {
                    Debug.Log($"Flashbang blocked by wall for {flashEffect.name}");
                }
                else
                {
                    float distance = toTarget.magnitude;
                    float normalizedDistance = Mathf.Clamp01(distance / effectRadius);
                    float flashDuration = Mathf.Lerp(maxFlashDuration, minFlashDuration, normalizedDistance);
                    float deafenDuration = Mathf.Lerp(maxDeafenDuration, maxDeafenDuration * 0.5f, normalizedDistance);
                    Debug.Log($"Flashing {flashEffect.name} at distance {distance:F1}m - Duration: {flashDuration:F1}s");
                    flashEffect.Flash(flashDuration, deafenDuration);
                }
            }

            if (aiEnemy != null && flashedAI.Add(aiEnemy))
            {
                float distance = Vector3.Distance(transform.position, aiEnemy.transform.position);
                float normalizedDistance = Mathf.Clamp01(distance / effectRadius);
                float flashDuration = Mathf.Lerp(maxFlashDuration, minFlashDuration, normalizedDistance);
                Debug.Log($"Flashbanging AI {aiEnemy.name} at distance {distance:F1}m - Duration: {flashDuration:F1}s");
                aiEnemy.OnFlashbanged(flashDuration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize effect radius in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
