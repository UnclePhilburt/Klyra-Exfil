using UnityEngine;

/// <summary>
/// Uses IK to make the character's arms aim the weapon towards the camera look direction.
/// Attach this to your character and assign the arm bones.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ArmIKAiming : MonoBehaviour
{
    [Header("Arm Bones")]
    [Tooltip("Right shoulder bone")]
    public Transform rightShoulder;

    [Tooltip("Right hand bone")]
    public Transform rightHand;

    [Tooltip("Left shoulder bone (optional)")]
    public Transform leftShoulder;

    [Tooltip("Left hand bone (optional)")]
    public Transform leftHand;

    [Header("IK Settings")]
    [Tooltip("Weight of the right hand IK (0-1)")]
    [Range(0f, 1f)]
    public float rightHandWeight = 1f;

    [Tooltip("Weight of the left hand IK (0-1)")]
    [Range(0f, 1f)]
    public float leftHandWeight = 1f;

    [Tooltip("Distance in front of the character to place the aim target")]
    public float aimDistance = 2f;

    [Tooltip("Vertical offset for the aim target")]
    public float aimHeightOffset = 0f;

    [Tooltip("Horizontal offset for right hand")]
    public Vector3 rightHandOffset = Vector3.zero;

    [Tooltip("Horizontal offset for left hand")]
    public Vector3 leftHandOffset = Vector3.zero;

    [Header("Camera Reference")]
    [Tooltip("Leave empty to auto-find the camera")]
    public Transform cameraTransform;

    [Header("Debug")]
    [Tooltip("Show debug gizmos")]
    public bool showDebug = true;

    private Animator animator;
    private Vector3 rightHandTargetPosition;
    private Vector3 leftHandTargetPosition;
    private Quaternion rightHandTargetRotation;
    private Quaternion leftHandTargetRotation;

    private void Start()
    {
        animator = GetComponent<Animator>();

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[ArmIKAiming] No camera found! Please assign a camera.");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || cameraTransform == null) return;

        // Calculate aim target position based on camera direction
        Vector3 aimTarget = cameraTransform.position + cameraTransform.forward * aimDistance;
        aimTarget.y += aimHeightOffset;

        // Right hand IK
        if (rightHand != null && rightHandWeight > 0f)
        {
            rightHandTargetPosition = aimTarget + transform.TransformDirection(rightHandOffset);
            rightHandTargetRotation = Quaternion.LookRotation(cameraTransform.forward);

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTargetPosition);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTargetRotation);
        }

        // Left hand IK
        if (leftHand != null && leftHandWeight > 0f)
        {
            leftHandTargetPosition = aimTarget + transform.TransformDirection(leftHandOffset);
            leftHandTargetRotation = Quaternion.LookRotation(cameraTransform.forward);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTargetPosition);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTargetRotation);
        }

        // Optional: Adjust body rotation slightly towards aim
        if (rightShoulder != null)
        {
            Vector3 shoulderToTarget = (aimTarget - rightShoulder.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(shoulderToTarget);
            animator.SetLookAtWeight(0.3f);
            animator.SetLookAtPosition(aimTarget);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || cameraTransform == null) return;

        // Draw aim direction
        Vector3 aimTarget = cameraTransform.position + cameraTransform.forward * aimDistance;
        aimTarget.y += aimHeightOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(aimTarget, 0.1f);
        Gizmos.DrawLine(cameraTransform.position, aimTarget);

        // Draw hand target positions
        if (rightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHandTargetPosition, 0.05f);
            Gizmos.DrawLine(rightHand.position, rightHandTargetPosition);
        }

        if (leftHand != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftHandTargetPosition, 0.05f);
            Gizmos.DrawLine(leftHand.position, leftHandTargetPosition);
        }
    }
}
