using UnityEngine;

/// <summary>
/// Rotates the character's spine to aim weapons up and down in third person.
/// Attach this to your character and assign the spine bone.
/// </summary>
public class SpineAimRotation : MonoBehaviour
{
    [Header("Bone References")]
    [Tooltip("The spine bone to rotate (usually Spine1 or Spine2)")]
    public Transform spineBone;

    [Tooltip("Optional: Additional upper spine bone for smoother rotation")]
    public Transform upperSpineBone;

    [Header("Rotation Settings")]
    [Tooltip("Maximum degrees the spine can rotate up")]
    [Range(0f, 90f)]
    public float maxUpRotation = 45f;

    [Tooltip("Maximum degrees the spine can rotate down")]
    [Range(0f, 90f)]
    public float maxDownRotation = 45f;

    [Tooltip("How quickly the spine rotates to match the aim direction")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;

    [Tooltip("Percentage of rotation applied to spine (rest goes to upper spine if assigned)")]
    [Range(0f, 1f)]
    public float spineRotationWeight = 0.6f;

    [Tooltip("Which local axis to rotate around (try different ones if rotation is wrong)")]
    public RotationAxis rotationAxis = RotationAxis.X;

    public enum RotationAxis
    {
        X,      // Right/Left axis (pitch up/down)
        Y,      // Up/Down axis (yaw left/right)
        Z,      // Forward/Back axis (roll)
        NegativeX,
        NegativeY,
        NegativeZ
    }

    [Header("Camera Reference")]
    [Tooltip("Leave empty to auto-find the camera")]
    public Transform cameraTransform;

    [Header("Debug")]
    [Tooltip("Enable debug logging to see what angles are being applied")]
    public bool enableDebug = false;

    private Quaternion spineOriginalRotation;
    private Quaternion upperSpineOriginalRotation;
    private bool hasUpperSpine;
    private bool originalRotationsCaptured = false;

    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        if (spineBone == null)
        {
            Debug.LogError("[SpineAimRotation] Spine bone is not assigned! Please assign it in the inspector.");
        }
        else
        {
            Debug.Log($"[SpineAimRotation] Spine bone assigned: {spineBone.name}");
        }

        hasUpperSpine = upperSpineBone != null;

        if (hasUpperSpine)
        {
            Debug.Log($"[SpineAimRotation] Upper spine bone assigned: {upperSpineBone.name}");
        }
    }

    private void CaptureOriginalRotations()
    {
        if (originalRotationsCaptured) return;

        // Delay capturing until a few frames in so animations settle
        if (Time.frameCount < 10) return;

        if (spineBone != null)
        {
            spineOriginalRotation = spineBone.localRotation;
            Debug.Log($"[SpineAimRotation] Captured spine original rotation: {spineOriginalRotation.eulerAngles}");
        }

        if (hasUpperSpine)
        {
            upperSpineOriginalRotation = upperSpineBone.localRotation;
            Debug.Log($"[SpineAimRotation] Captured upper spine original rotation: {upperSpineOriginalRotation.eulerAngles}");
        }

        originalRotationsCaptured = true;
    }

    private void LateUpdate()
    {
        if (spineBone == null || cameraTransform == null) return;

        ApplySpineRotation();
    }

    // Use OnAnimatorIK to apply rotation AFTER animation
    private void OnAnimatorIK(int layerIndex)
    {
        if (spineBone == null || cameraTransform == null) return;

        ApplySpineRotation();
    }

    private void ApplySpineRotation()
    {
        // Capture original rotations after a few frames
        CaptureOriginalRotations();
        if (!originalRotationsCaptured) return;

        // Get the camera's forward direction
        Vector3 cameraForward = cameraTransform.forward;

        // Project camera forward onto character's forward plane to get pitch
        Vector3 characterForward = transform.forward;
        Vector3 characterRight = transform.right;

        // Calculate pitch by comparing camera forward to character forward
        float pitchAngle = Vector3.SignedAngle(
            new Vector3(characterForward.x, 0, characterForward.z).normalized,
            cameraForward,
            characterRight
        );

        // Clamp the pitch angle
        pitchAngle = Mathf.Clamp(pitchAngle, -maxDownRotation, maxUpRotation);

        if (enableDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[SpineAimRotation] Pitch Angle: {pitchAngle:F2}°, Camera Forward: {cameraForward}");
        }

        // Calculate target rotation for spine
        float spineAngle = pitchAngle * spineRotationWeight;

        // Get the rotation axis based on settings
        Vector3 axis = GetRotationAxisVector();

        // Rotate around the selected axis
        Quaternion targetSpineRotation = spineOriginalRotation * Quaternion.AngleAxis(spineAngle, axis);

        // Apply rotation directly (no smoothing for better responsiveness)
        spineBone.localRotation = targetSpineRotation;

        if (enableDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[SpineAimRotation] Applying spine angle: {spineAngle:F2}° around axis {rotationAxis} to {spineBone.name}");
        }

        // If we have an upper spine, rotate it too
        if (hasUpperSpine)
        {
            float upperSpineAngle = pitchAngle * (1f - spineRotationWeight);
            Quaternion targetUpperSpineRotation = upperSpineOriginalRotation * Quaternion.AngleAxis(upperSpineAngle, axis);

            upperSpineBone.localRotation = targetUpperSpineRotation;
        }
    }

    private Vector3 GetRotationAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            case RotationAxis.NegativeX: return Vector3.left;
            case RotationAxis.NegativeY: return Vector3.down;
            case RotationAxis.NegativeZ: return Vector3.back;
            default: return Vector3.right;
        }
    }

    // Visualize the aim direction in the scene view
    private void OnDrawGizmos()
    {
        if (spineBone == null || cameraTransform == null) return;

        // Draw a line showing the aim direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(spineBone.position, spineBone.position + cameraTransform.forward * 2f);

        // Draw the spine bone position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(spineBone.position, 0.05f);

        if (upperSpineBone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(upperSpineBone.position, 0.05f);
        }
    }
}
