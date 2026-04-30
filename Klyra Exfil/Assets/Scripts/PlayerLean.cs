using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player lean system - leans character spine left (Q) and right (E)
/// Works with Opsive Ultimate Character Controller and Animator
/// Rotates spine bones for realistic leaning like Ready or Not / Arma / Tarkov
/// Uses new Input System with direct keyboard input
/// </summary>
public class PlayerLean : MonoBehaviour
{
    [Header("Spine Lean Settings")]
    [Tooltip("Spine bone to rotate (usually 'Spine' or 'Spine1')")]
    public Transform spineBone;

    [Tooltip("Upper spine bone (usually 'Spine2' or 'Chest') - optional for more natural lean")]
    public Transform upperSpineBone;

    [Tooltip("How far to lean the spine in degrees")]
    [Range(5f, 30f)]
    public float spineLeanAngle = 15f;

    [Tooltip("How far to lean upper spine (less than main spine for natural look)")]
    [Range(0f, 20f)]
    public float upperSpineLeanAngle = 8f;

    [Tooltip("How fast to lean")]
    public float leanSpeed = 8f;

    [Header("Rotation Axis")]
    [Tooltip("Which axis to rotate for leaning - try different ones if lean direction is wrong")]
    public LeanAxis leanAxis = LeanAxis.Z_Roll;

    public enum LeanAxis
    {
        X_Pitch,  // Forward/backward tilt
        Y_Yaw,    // Left/right turn
        Z_Roll    // Left/right lean (most common)
    }

    [Header("Position Offset")]
    [Tooltip("Move character controller sideways when leaning (for peeking around corners)")]
    public bool usePositionOffset = true;

    [Tooltip("How far to move character sideways when fully leaned")]
    public float positionOffset = 0.2f;

    [Header("Auto-Find Bones")]
    [Tooltip("Automatically find spine bones in character hierarchy")]
    public bool autoFindBones = true;

    [Header("Debug")]
    public bool showDebug = false;

    private float currentLean = 0f; // -1 = left, 0 = center, 1 = right
    private Quaternion originalSpineRotation;
    private Quaternion originalUpperSpineRotation;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 originalControllerCenter;
    private bool isInitialized = false;

    void Start()
    {
        // Get animator
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerLean] No Animator found! Lean system requires an animated character.");
            enabled = false;
            return;
        }

        // Get character controller
        characterController = GetComponent<CharacterController>();

        // Auto-find spine bones if needed
        if (autoFindBones)
        {
            FindSpineBones();
        }

        // Validate spine bone
        if (spineBone == null)
        {
            Debug.LogError("[PlayerLean] No spine bone assigned! Please assign manually or enable autoFindBones.");
            enabled = false;
            return;
        }

        // Store original rotations
        originalSpineRotation = spineBone.localRotation;
        if (upperSpineBone != null)
        {
            originalUpperSpineRotation = upperSpineBone.localRotation;
        }

        // Store original character controller center
        if (characterController != null)
        {
            originalControllerCenter = characterController.center;
        }

        isInitialized = true;

        if (showDebug)
        {
            Debug.Log($"[PlayerLean] Initialized - Spine: {spineBone.name}, Upper: {(upperSpineBone != null ? upperSpineBone.name : "None")}");
        }
    }

    void FindSpineBones()
    {
        if (animator == null) return;

        // Try to find spine using Humanoid rig first
        if (animator.isHuman)
        {
            spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            upperSpineBone = animator.GetBoneTransform(HumanBodyBones.Chest);

            if (showDebug)
            {
                Debug.Log("[PlayerLean] Found spine bones using Humanoid rig");
            }
        }
        else
        {
            // Fallback: search by name
            Transform[] allBones = animator.GetComponentsInChildren<Transform>();
            foreach (Transform bone in allBones)
            {
                string boneName = bone.name.ToLower();

                // Look for spine
                if (spineBone == null && (boneName.Contains("spine") && !boneName.Contains("spine2") && !boneName.Contains("spine3")))
                {
                    spineBone = bone;
                }

                // Look for upper spine/chest
                if (upperSpineBone == null && (boneName.Contains("spine2") || boneName.Contains("chest")))
                {
                    upperSpineBone = bone;
                }
            }

            if (showDebug)
            {
                Debug.Log($"[PlayerLean] Searched by name - Spine: {(spineBone != null ? spineBone.name : "Not found")}, Upper: {(upperSpineBone != null ? upperSpineBone.name : "Not found")}");
            }
        }
    }

    void LateUpdate()
    {
        // LateUpdate so it applies AFTER animator updates bones
        if (!isInitialized) return;

        // Get lean input from new Input System keyboard
        float targetLean = 0f;

        if (Keyboard.current != null)
        {
            // Q = lean left (-1)
            if (Keyboard.current.qKey.isPressed)
            {
                targetLean = -1f;
            }
            // E = lean right (+1)
            else if (Keyboard.current.eKey.isPressed)
            {
                targetLean = 1f;
            }
        }

        // Smoothly interpolate to target lean
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // Apply lean to spine bones
        ApplySpineLean();

        // Apply position offset for peeking
        if (usePositionOffset && characterController != null)
        {
            ApplyPositionOffset();
        }
    }

    void ApplySpineLean()
    {
        // Calculate lean angle (positive = inverted from default)
        float leanAngleValue = currentLean * spineLeanAngle;

        // Create rotation based on selected axis
        Quaternion leanRotation;
        switch (leanAxis)
        {
            case LeanAxis.X_Pitch:
                leanRotation = Quaternion.Euler(leanAngleValue, 0f, 0f);
                break;
            case LeanAxis.Y_Yaw:
                leanRotation = Quaternion.Euler(0f, leanAngleValue, 0f);
                break;
            case LeanAxis.Z_Roll:
            default:
                leanRotation = Quaternion.Euler(0f, 0f, leanAngleValue);
                break;
        }

        // Apply to main spine
        spineBone.localRotation = originalSpineRotation * leanRotation;

        // Apply to upper spine (less rotation for natural look)
        if (upperSpineBone != null)
        {
            float upperLeanAngleValue = currentLean * upperSpineLeanAngle;

            Quaternion upperLeanRotation;
            switch (leanAxis)
            {
                case LeanAxis.X_Pitch:
                    upperLeanRotation = Quaternion.Euler(upperLeanAngleValue, 0f, 0f);
                    break;
                case LeanAxis.Y_Yaw:
                    upperLeanRotation = Quaternion.Euler(0f, upperLeanAngleValue, 0f);
                    break;
                case LeanAxis.Z_Roll:
                default:
                    upperLeanRotation = Quaternion.Euler(0f, 0f, upperLeanAngleValue);
                    break;
            }

            upperSpineBone.localRotation = originalUpperSpineRotation * upperLeanRotation;
        }

        if (showDebug && Mathf.Abs(currentLean) > 0.01f)
        {
            Debug.Log($"[PlayerLean] Spine leaning {(currentLean < 0 ? "LEFT" : "RIGHT")}: {Mathf.Abs(leanAngleValue):F1}° on {leanAxis} axis");
        }
    }

    void ApplyPositionOffset()
    {
        // Move character controller center sideways for peeking
        Vector3 offset = Vector3.right * (currentLean * positionOffset);
        characterController.center = originalControllerCenter + offset;
    }

    void OnDisable()
    {
        // Reset spine bones when disabled
        if (isInitialized)
        {
            if (spineBone != null)
            {
                spineBone.localRotation = originalSpineRotation;
            }
            if (upperSpineBone != null)
            {
                upperSpineBone.localRotation = originalUpperSpineRotation;
            }
            if (characterController != null)
            {
                characterController.center = originalControllerCenter;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebug || !isInitialized || spineBone == null) return;

        // Draw spine bone position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spineBone.position, 0.1f);

        if (upperSpineBone != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upperSpineBone.position, 0.08f);
            Gizmos.DrawLine(spineBone.position, upperSpineBone.position);
        }

        // Draw lean direction
        if (Mathf.Abs(currentLean) > 0.01f)
        {
            Gizmos.color = currentLean < 0 ? Color.red : Color.green;
            Vector3 leanDir = transform.right * currentLean * 0.5f;
            Gizmos.DrawRay(spineBone.position, leanDir);
        }
    }
}
