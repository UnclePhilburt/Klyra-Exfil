using UnityEngine;
using Photon.Pun;

/// <summary>
/// Basic door that can be opened and closed. Synced over network with PUN2.
/// </summary>
public class Door : MonoBehaviourPun
{
    [Header("Door Settings")]
    [Tooltip("Is the door currently open?")]
    public bool isOpen = false;

    [Tooltip("Is the door locked?")]
    public bool isLocked = false;

    [Tooltip("Can this door be breached?")]
    public bool canBeBreach = true;

    [Tooltip("Time it takes for door to open (seconds)")]
    public float openSpeed = 1f;

    [Tooltip("How fast door opens when breached (faster = more violent)")]
    public float breachSpeed = 0.2f;

    [Header("Breach Effects")]
    [Tooltip("Camera shake duration when breached")]
    public float breachShakeDuration = 0.3f;

    [Tooltip("Camera shake intensity when breached")]
    public float breachShakeIntensity = 0.3f;

    [Tooltip("Radius within which players feel the breach shake")]
    public float breachShakeRadius = 10f;

    [Header("Animation")]
    [Tooltip("The door object that will rotate (hinge side should be at parent's pivot)")]
    public Transform doorTransform;

    [Tooltip("How far the door opens in degrees")]
    public float openAngle = 90f;

    [Tooltip("Which axis to rotate (Y is typical for vertical doors)")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Collision")]
    [Tooltip("The collider that blocks players (will be disabled when door opens)")]
    public Collider doorCollider;

    [Header("Audio")]
    [Tooltip("Sound when door opens")]
    public AudioClip openSound;

    [Tooltip("Sound when door closes")]
    public AudioClip closeSound;

    [Tooltip("Sound when trying to open locked door")]
    public AudioClip lockedSound;

    [Tooltip("Sound when door is breached")]
    public AudioClip breachSound;

    private AudioSource audioSource;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating = false;
    private float animationProgress = 0f;
    private bool isBreeched = false;
    private float currentOpenSpeed;

    void Start()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // If no door transform specified, use this object
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        // Store initial rotation as closed state
        closedRotation = doorTransform.localRotation;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

        // Set initial state
        if (isOpen)
        {
            doorTransform.localRotation = openRotation;
            animationProgress = 1f;
        }
    }

    void Update()
    {
        // Animate door opening/closing
        if (isAnimating)
        {
            float speed = isBreeched ? currentOpenSpeed : openSpeed;

            if (isOpen)
            {
                // Opening
                animationProgress += Time.deltaTime / speed;
                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    isAnimating = false;
                    isBreeched = false; // Reset breach flag
                }
            }
            else
            {
                // Closing
                animationProgress -= Time.deltaTime / speed;
                if (animationProgress <= 0f)
                {
                    animationProgress = 0f;
                    isAnimating = false;
                }
            }

            // Smooth rotation
            doorTransform.localRotation = Quaternion.Lerp(closedRotation, openRotation, animationProgress);
        }
    }

    /// <summary>
    /// Attempt to open the door. Returns true if successful.
    /// </summary>
    public bool TryOpen()
    {
        if (isLocked)
        {
            PlaySound(lockedSound);
            Debug.Log("Door is locked!");
            return false;
        }

        if (isOpen)
        {
            return true; // Already open
        }

        // Network sync - check if PhotonView is valid first
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            Debug.Log($"[Door] Sending RPC_SetDoorState(true) on door {gameObject.name}, ViewID: {photonView.ViewID}, IsConnected: {PhotonNetwork.IsConnected}");
            photonView.RPC("RPC_SetDoorState", RpcTarget.AllBuffered, true);
        }
        else
        {
            Debug.LogWarning($"[Door] NOT sending RPC - IsConnected: {PhotonNetwork.IsConnected}, photonView null: {photonView == null}, ViewID: {photonView?.ViewID ?? 0}");
            SetDoorState(true);
        }

        return true;
    }

    /// <summary>
    /// Close the door.
    /// </summary>
    public void Close()
    {
        if (!isOpen)
        {
            return; // Already closed
        }

        // Network sync - check if PhotonView is valid first
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_SetDoorState", RpcTarget.AllBuffered, false);
        }
        else
        {
            SetDoorState(false);
        }
    }

    /// <summary>
    /// Toggle door state (open if closed, close if open)
    /// </summary>
    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            TryOpen();
        }
    }

    /// <summary>
    /// Breach the door - forces it open violently (kick)
    /// </summary>
    public bool Breach()
    {
        if (!canBeBreach)
        {
            Debug.Log("This door cannot be breached!");
            return false;
        }

        if (isOpen)
        {
            return true; // Already open
        }

        // Network sync - check if PhotonView is valid first
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_BreachDoor", RpcTarget.AllBuffered);
        }
        else
        {
            BreachDoor();
        }

        return true;
    }

    /// <summary>
    /// Explosive breach - even more violent than kick
    /// </summary>
    public void ExplosiveBreach()
    {
        if (isOpen) return;

        // Network sync - check if PhotonView is valid first
        if (PhotonNetwork.IsConnected && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_ExplosiveBreach", RpcTarget.AllBuffered);
        }
        else
        {
            DoExplosiveBreach();
        }
    }

    [PunRPC]
    void RPC_ExplosiveBreach()
    {
        DoExplosiveBreach();
    }

    void DoExplosiveBreach()
    {
        isOpen = true;
        isBreeched = true;
        isLocked = false;

        // Disable collider immediately
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        // Make the door fly off!
        if (doorTransform != null)
        {
            StartCoroutine(BlowDoorOff());
        }

        // Play breach sound (louder)
        PlaySound(breachSound);

        Debug.Log("Door blown off its hinges!");
    }

    System.Collections.IEnumerator BlowDoorOff()
    {
        // Move door slightly away from frame first to prevent collision
        doorTransform.position += doorTransform.forward * 0.3f;

        // Wait a tiny moment
        yield return new WaitForFixedUpdate();

        // Detach door from parent so it can fly free
        doorTransform.SetParent(null);

        // Add rigidbody for physics
        Rigidbody rb = doorTransform.gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = doorTransform.gameObject.AddComponent<Rigidbody>();
        }

        // Make it not kinematic so physics affects it
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Apply explosion force (impulse mode for instant effect)
        Vector3 explosionDirection = doorTransform.forward;
        float explosionForce = 1000f;
        rb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);

        // Add some random torque for spinning
        rb.AddTorque(Random.insideUnitSphere * 300f, ForceMode.Impulse);

        Debug.Log($"Door launched with force! Velocity: {rb.linearVelocity}");

        // Destroy the door after a few seconds
        Destroy(doorTransform.gameObject, 5f);
    }

    [PunRPC]
    void RPC_BreachDoor()
    {
        BreachDoor();
    }

    void BreachDoor()
    {
        isOpen = true;
        isBreeched = true;
        isLocked = false; // Breaching breaks the lock
        currentOpenSpeed = breachSpeed; // Use faster breach speed
        isAnimating = true;

        // Disable collider immediately
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        // Play breach sound
        PlaySound(breachSound);

        // Camera shake for nearby players
        CameraShake.Shake(breachShakeDuration, breachShakeIntensity);

        Debug.Log("Door breached!");
    }

    [PunRPC]
    void RPC_SetDoorState(bool open)
    {
        Debug.Log($"[Door] RPC_SetDoorState({open}) received on door {gameObject.name}, ViewID: {photonView.ViewID}");
        SetDoorState(open);
    }

    void SetDoorState(bool open)
    {
        Debug.Log($"[Door] SetDoorState({open}) called on door {gameObject.name}");
        isOpen = open;
        isAnimating = true;

        // Enable/disable door collider
        if (doorCollider != null)
        {
            doorCollider.enabled = !open; // Disable when open, enable when closed
        }

        // Play sound
        if (open)
        {
            PlaySound(openSound);
        }
        else
        {
            PlaySound(closeSound);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (doorTransform != null)
        {
            Gizmos.color = isLocked ? Color.red : (isOpen ? Color.green : Color.yellow);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
