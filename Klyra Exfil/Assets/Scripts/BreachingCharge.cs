using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Breaching charge that can be placed on doors and detonated.
/// </summary>
public class BreachingCharge : MonoBehaviourPun
{
    [Header("Charge Settings")]
    [Tooltip("Time before auto-detonation after placement")]
    public float fuseTime = 3f;

    [Tooltip("Can be manually detonated before fuse runs out?")]
    public bool allowManualDetonation = true;

    [Header("Explosion")]
    [Tooltip("Explosion radius for camera shake and effects")]
    public float explosionRadius = 10f;

    [Tooltip("Camera shake duration")]
    public float shakeDuration = 0.5f;

    [Tooltip("Camera shake intensity")]
    public float shakeIntensity = 0.5f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect to spawn on explosion (optional)")]
    public GameObject explosionEffect;

    [Tooltip("Light flash on explosion (optional)")]
    public Light explosionLight;

    [Tooltip("Flash duration")]
    public float flashDuration = 0.2f;

    [Header("Audio")]
    [Tooltip("Beeping sound while armed")]
    public AudioClip beepSound;

    [Tooltip("Explosion sound")]
    public AudioClip explosionSound;

    private Door targetDoor;
    private AudioSource audioSource;
    private bool isArmed = false;
    private bool hasDetonated = false;
    private float timeArmed = 0f;
    private float lastBeepTime = 0f;

    void OnEnable()
    {
        // Reset state when object is spawned/reused (important for PUN pooling)
        isArmed = false;
        hasDetonated = false;
        timeArmed = 0f;
        lastBeepTime = 0f;
        targetDoor = null;

        Debug.Log($"BreachingCharge OnEnable - state reset");
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isArmed && !hasDetonated)
        {
            timeArmed += Time.deltaTime;

            // Beeping effect once per second
            if (beepSound != null && timeArmed - lastBeepTime >= 1f)
            {
                audioSource.PlayOneShot(beepSound);
                lastBeepTime = timeArmed;
                Debug.Log($"Charge beeping... Time armed: {timeArmed:F2}s / {fuseTime}s");
            }

            // Auto-detonate after fuse time
            if (timeArmed >= fuseTime)
            {
                Debug.Log($"Fuse time reached! Detonating charge. TimeArmed: {timeArmed}, FuseTime: {fuseTime}");
                Detonate();
            }
        }
        else if (!isArmed)
        {
            // Debug: charge not armed
            if (Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.LogWarning($"Charge exists but is NOT armed! isArmed={isArmed}, hasDetonated={hasDetonated}");
            }
        }
    }

    /// <summary>
    /// Arm the charge on a door
    /// </summary>
    public void Arm(Door door)
    {
        Debug.Log($"Arm() called. Current state: isArmed={isArmed}, targetDoor={targetDoor?.name}, newDoor={door?.name}");

        if (isArmed)
        {
            Debug.LogWarning($"Charge already armed on {targetDoor?.name}! Ignoring arm request for {door?.name}");
            return;
        }

        targetDoor = door;
        isArmed = true;
        timeArmed = 0f;
        lastBeepTime = 0f;

        Debug.Log($"✓ Breaching charge ARMED on {door.name}! Detonating in {fuseTime} seconds... Position: {transform.position}");

        // Keep the charge at the position it was spawned at (already centered)
        transform.SetParent(door.transform);
    }

    /// <summary>
    /// Detonate the charge
    /// </summary>
    public void Detonate()
    {
        if (hasDetonated) return;

        Debug.Log("BOOM! Breaching charge detonated!");

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

        // Breach the door (door handles network sync)
        if (targetDoor != null)
        {
            targetDoor.ExplosiveBreach();
        }

        // Camera shake (local effect)
        CameraShake.Shake(shakeDuration, shakeIntensity);

        // Explosion sound
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound);
        }

        // Visual effects
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Light flash
        if (explosionLight != null)
        {
            StartCoroutine(FlashLight());
        }

        // Destroy charge for everyone
        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            Destroy(gameObject, 0.1f);
        }
    }

    IEnumerator FlashLight()
    {
        explosionLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        explosionLight.enabled = false;
    }
}
