using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Shooting target that falls down when hit and pops back up after a delay.
/// Synced over network with PUN2.
/// </summary>
public class ShootingTarget : MonoBehaviourPun
{
    [Header("Target Settings")]
    [Tooltip("How much damage needed to knock down the target")]
    public float damageThreshold = 10f;

    [Tooltip("Time before target pops back up")]
    public float resetTime = 3f;

    [Header("Animation")]
    [Tooltip("The target mesh that rotates down")]
    public Transform targetTransform;

    [Tooltip("Rotation when standing (default)")]
    public Vector3 standingRotation = Vector3.zero;

    [Tooltip("Rotation when knocked down")]
    public Vector3 downRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("How fast the target falls/rises")]
    public float rotationSpeed = 2f;

    [Header("Audio")]
    [Tooltip("Sound when hit")]
    public AudioClip hitSound;

    [Tooltip("Sound when popping back up")]
    public AudioClip popupSound;

    private AudioSource audioSource;
    private bool isDown = false;
    private float currentDamage = 0f;
    private Quaternion targetStandingRotation;
    private Quaternion targetDownRotation;

    void Start()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // If no target transform specified, use this object
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        // Store rotations
        targetStandingRotation = Quaternion.Euler(standingRotation);
        targetDownRotation = Quaternion.Euler(downRotation);
    }

    void Update()
    {
        // Smoothly rotate to target rotation
        Quaternion targetRotation = isDown ? targetDownRotation : targetStandingRotation;
        targetTransform.localRotation = Quaternion.Lerp(targetTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    /// <summary>
    /// Call this when the target takes damage (from UCC's Health component or raycast hit)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDown) return; // Already down

        currentDamage += damage;

        if (currentDamage >= damageThreshold)
        {
            KnockDown();
        }
    }

    void KnockDown()
    {
        if (isDown) return;

        // Network sync
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_KnockDown", RpcTarget.All);
        }
        else
        {
            DoKnockDown();
        }
    }

    [PunRPC]
    void RPC_KnockDown()
    {
        DoKnockDown();
    }

    void DoKnockDown()
    {
        isDown = true;
        currentDamage = 0f;

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        Debug.Log("Target knocked down!");

        // Start reset timer
        StartCoroutine(ResetAfterDelay());
    }

    IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetTime);

        // Network sync reset
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_Reset", RpcTarget.All);
        }
        else
        {
            DoReset();
        }
    }

    [PunRPC]
    void RPC_Reset()
    {
        DoReset();
    }

    void DoReset()
    {
        isDown = false;

        // Play popup sound
        if (popupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popupSound);
        }

        Debug.Log("Target reset!");
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        Gizmos.color = isDown ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
