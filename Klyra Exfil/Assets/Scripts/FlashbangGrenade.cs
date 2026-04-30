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

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging for this flashbang")]
    public bool enableDebug = true;

    private AudioSource audioSource;
    private bool hasDetonated = false;
    private float timeActive = 0f;
    private bool fuseStarted = false;
    private int flashbangID;
    private static int nextID = 1;

    void Awake()
    {
        flashbangID = nextID++;
        DebugLog($"============ FLASHBANG #{flashbangID} CREATED ============");
        DebugLog($"Position: {transform.position}");
        DebugLog($"IsConnected: {PhotonNetwork.IsConnected}");
        DebugLog($"PhotonView: {(photonView != null ? "Present" : "NULL")}");
        if (photonView != null)
        {
            DebugLog($"ViewID: {photonView.ViewID}");
            DebugLog($"IsMine: {photonView.IsMine}");
        }
    }

    void Start()
    {
        DebugLog($"Start() called - GameObject active: {gameObject.activeInHierarchy}, enabled: {enabled}");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 50f;
            DebugLog("AudioSource created");
        }

        // Play pin pull sound
        if (pinSound != null)
        {
            audioSource.PlayOneShot(pinSound);
            DebugLog("Pin pull sound played");
        }

        // Delay fuse start slightly to ensure PhotonView is initialized in multiplayer
        if (PhotonNetwork.IsConnected)
        {
            DebugLog("Multiplayer mode - starting DelayedFuseStart coroutine");
            StartCoroutine(DelayedFuseStart());
        }
        else
        {
            DebugLog("Offline mode - starting fuse immediately");
            StartFuse();
        }
    }

    /// <summary>
    /// Wait for PhotonView to be initialized before starting fuse (multiplayer only)
    /// </summary>
    IEnumerator DelayedFuseStart()
    {
        DebugLog("DelayedFuseStart coroutine started");
        // Wait until PhotonView has a valid ViewID (with timeout)
        float timeout = 2f;
        float elapsed = 0f;

        if (photonView != null)
        {
            DebugLog($"Waiting for PhotonView ViewID (currently {photonView.ViewID})...");
            int frameCount = 0;
            while (photonView.ViewID == 0 && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
                frameCount++;
            }

            DebugLog($"PhotonView wait finished after {frameCount} frames, {elapsed:F3}s - ViewID: {photonView.ViewID}");

            if (photonView.ViewID == 0)
            {
                DebugLog($"WARNING: PhotonView failed to initialize after {timeout}s! Starting fuse anyway.", true);
            }
            else
            {
                DebugLog($"PhotonView initialized successfully - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");
            }
        }
        else
        {
            DebugLog("WARNING: PhotonView is NULL!", true);
        }

        StartFuse();
    }

    void Update()
    {
        // In multiplayer, only the owner runs the timer to prevent double detonation
        // Non-owners wait for RPC from owner
        if (PhotonNetwork.IsConnected && photonView != null && !photonView.IsMine)
        {
            return;
        }

        if (fuseStarted && !hasDetonated)
        {
            timeActive += Time.deltaTime;

            // Log every 0.5 seconds
            if (Mathf.FloorToInt(timeActive / 0.5f) > Mathf.FloorToInt((timeActive - Time.deltaTime) / 0.5f))
            {
                DebugLog($"Fuse burning... {timeActive:F2}s / {fuseTime:F2}s (enabled: {enabled}, active: {gameObject.activeInHierarchy})");
            }

            if (timeActive >= fuseTime)
            {
                DebugLog($"FUSE EXPIRED! Time: {timeActive:F2}s >= {fuseTime:F2}s - Calling Detonate()");
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
        DebugLog($"======== FUSE STARTED ======== Will detonate in {fuseTime}s");
        DebugLog($"GameObject: {gameObject.name}, Active: {gameObject.activeInHierarchy}, Enabled: {enabled}");
        DebugLog($"Rigidbody: {(GetComponent<Rigidbody>() != null ? "Present" : "NULL")}");

        // Backup detonation using Invoke - guarantees detonation even if Update stops
        float backupTime = fuseTime + 0.1f;
        Invoke(nameof(DetonateBackup), backupTime);
        DebugLog($"Backup detonation scheduled for {backupTime}s from now");
    }

    /// <summary>
    /// Backup detonation method called via Invoke as a failsafe
    /// </summary>
    void DetonateBackup()
    {
        if (!hasDetonated)
        {
            DebugLog("⚠️ BACKUP DETONATION TRIGGERED! Update loop failed or was disabled", true);
            DebugLog($"Stats: fuseStarted={fuseStarted}, timeActive={timeActive:F2}s, enabled={enabled}, active={gameObject.activeInHierarchy}");
            Detonate();
        }
        else
        {
            DebugLog("Backup detonation called but already detonated (normal)");
        }
    }

    /// <summary>
    /// Detonate the flashbang
    /// </summary>
    public void Detonate()
    {
        if (hasDetonated)
        {
            DebugLog("Detonate() called but already detonated - ignoring");
            return;
        }

        DebugLog("💥 ======== DETONATE() CALLED ========");
        DebugLog($"Position: {transform.position}");
        DebugLog($"IsConnected: {PhotonNetwork.IsConnected}");
        DebugLog($"PhotonView: {(photonView != null ? "Present" : "NULL")}");

        // Sync detonation over network
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            DebugLog($"Multiplayer mode - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");

            if (photonView.ViewID != 0)
            {
                DebugLog($"Sending RPC_Detonate to RpcTarget.All");
                // RpcTarget.All includes the sender, so everyone (including us) will call DoDetonate
                photonView.RPC("RPC_Detonate", RpcTarget.All);
            }
            else
            {
                // ViewID not assigned yet - this shouldn't happen with DelayedFuseStart, but handle it
                DebugLog("⚠️ WARNING: PhotonView ViewID is 0! Forcing local detonation only!", true);
                DoDetonate();
            }
        }
        else
        {
            // Single-player or no network
            DebugLog("Single-player mode - calling DoDetonate() directly");
            DoDetonate();
        }
    }

    [PunRPC]
    void RPC_Detonate()
    {
        DebugLog($"📡 RPC_Detonate received! (IsMine: {photonView?.IsMine})");
        DoDetonate();
    }

    void DoDetonate()
    {
        if (hasDetonated)
        {
            DebugLog("⚠️ DoDetonate() called but already detonated - ignoring duplicate", true);
            return;
        }

        DebugLog("💥💥💥 EXECUTING DETONATION EFFECTS 💥💥💥");
        DebugLog($"Position: {transform.position}, Active: {gameObject.activeInHierarchy}");
        hasDetonated = true;

        // Play bang sound
        if (bangSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bangSound);
            DebugLog("✓ Bang sound played");
        }
        else
        {
            DebugLog($"⚠️ Bang sound NOT played - bangSound: {(bangSound != null ? "OK" : "NULL")}, audioSource: {(audioSource != null ? "OK" : "NULL")}");
        }

        // Spawn flash effect
        if (flashEffect != null)
        {
            Instantiate(flashEffect, transform.position, Quaternion.identity);
            DebugLog("✓ Flash effect spawned");
        }
        else
        {
            DebugLog("⚠️ No flash effect prefab assigned");
        }

        // Flash light
        if (flashLight != null)
        {
            StartCoroutine(FlashLightEffect());
            DebugLog("✓ Flash light effect started");
        }
        else
        {
            DebugLog("⚠️ No flash light assigned");
        }

        // Find all players in radius and flash them
        DebugLog("Searching for players to flash...");
        FlashNearbyPlayers();

        // Destroy grenade after a delay (let audio finish)
        DebugLog("Scheduling destruction in 2 seconds");
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

        DebugLog($"DestroyAfterDelay: Destroying flashbang after {delay}s delay");

        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            DebugLog("Destroying via PhotonNetwork.Destroy (multiplayer)");
            PhotonNetwork.Destroy(gameObject);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            DebugLog("Destroying via Unity Destroy (offline)");
            Destroy(gameObject);
        }
        else
        {
            DebugLog("⚠️ NOT destroying - not mine in multiplayer (will be destroyed by owner)");
        }
    }

    /// <summary>
    /// Flash all nearby players based on distance and line of sight
    /// </summary>
    void FlashNearbyPlayers()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius);
        DebugLog($"Found {colliders.Length} colliders in {effectRadius}m radius");

        // Dedupe: characters have many colliders (ragdoll bones) and we only
        // want to flash each one once.
        var flashedPlayers = new System.Collections.Generic.HashSet<FlashbangEffect>();
        var flashedAI = new System.Collections.Generic.HashSet<TacticalAI>();

        int playerCount = 0;
        int aiCount = 0;

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
                playerCount++;
                Vector3 toTarget = flashEffect.transform.position - transform.position;
                if (Physics.Raycast(transform.position, toTarget.normalized, toTarget.magnitude, obstacleMask))
                {
                    DebugLog($"  Player {flashEffect.name} - BLOCKED by wall");
                }
                else
                {
                    float distance = toTarget.magnitude;
                    float normalizedDistance = Mathf.Clamp01(distance / effectRadius);
                    float flashDuration = Mathf.Lerp(maxFlashDuration, minFlashDuration, normalizedDistance);
                    float deafenDuration = Mathf.Lerp(maxDeafenDuration, maxDeafenDuration * 0.5f, normalizedDistance);
                    DebugLog($"  ✓ FLASHING player {flashEffect.name} at {distance:F1}m - Duration: {flashDuration:F1}s");
                    flashEffect.Flash(flashDuration, deafenDuration);
                }
            }

            if (aiEnemy != null && flashedAI.Add(aiEnemy))
            {
                aiCount++;
                float distance = Vector3.Distance(transform.position, aiEnemy.transform.position);
                float normalizedDistance = Mathf.Clamp01(distance / effectRadius);
                float flashDuration = Mathf.Lerp(maxFlashDuration, minFlashDuration, normalizedDistance);
                DebugLog($"  ✓ FLASHBANGING AI {aiEnemy.name} at {distance:F1}m - Duration: {flashDuration:F1}s");
                aiEnemy.OnFlashbanged(flashDuration);
            }
        }

        DebugLog($"Flash summary: {playerCount} players, {aiCount} AI affected");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize effect radius in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }

    void OnDisable()
    {
        DebugLog("⚠️ OnDisable() called - GameObject being disabled!");
    }

    void OnDestroy()
    {
        if (!hasDetonated)
        {
            DebugLog("❌ OnDestroy() called WITHOUT detonation! Flashbang was destroyed before exploding!", true);
            DebugLog($"Stats: fuseStarted={fuseStarted}, timeActive={timeActive:F2}s/{fuseTime:F2}s");
        }
        else
        {
            DebugLog("OnDestroy() called after successful detonation (normal cleanup)");
        }
    }

    /// <summary>
    /// Debug logging helper with flashbang ID
    /// </summary>
    void DebugLog(string message, bool isWarning = false)
    {
        if (!enableDebug) return;

        string prefix = $"[FB #{flashbangID}] ";
        if (isWarning)
        {
            Debug.LogWarning(prefix + message, this);
        }
        else
        {
            Debug.Log(prefix + message, this);
        }
    }
}
