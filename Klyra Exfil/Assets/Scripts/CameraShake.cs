using UnityEngine;
using System.Collections;

/// <summary>
/// Simple camera shake effect. Attach to camera or call statically.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    private Transform cameraTransform;
    private Vector3 originalPosition;
    private bool isShaking = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Get the camera transform
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            cameraTransform = transform;
        }

        originalPosition = cameraTransform.localPosition;
    }

    /// <summary>
    /// Shake the camera with specified intensity and duration
    /// </summary>
    public static void Shake(float duration = 0.3f, float intensity = 0.2f)
    {
        Debug.Log($"CameraShake.Shake called! Duration: {duration}, Intensity: {intensity}, Instance: {instance != null}");

        if (instance != null)
        {
            instance.StartCoroutine(instance.DoShake(duration, intensity));
        }
        else
        {
            Debug.LogWarning("CameraShake instance not found! Add CameraShake component to camera.");
        }
    }

    private IEnumerator DoShake(float duration, float intensity)
    {
        if (isShaking)
        {
            Debug.Log("Already shaking, skipping...");
            yield break; // Don't shake if already shaking
        }

        Debug.Log($"Starting camera shake! Duration: {duration}, Intensity: {intensity}");
        isShaking = true;
        float elapsed = 0f;
        Vector3 startPosition = cameraTransform.localPosition;

        while (elapsed < duration)
        {
            // Random offset
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            cameraTransform.localPosition = startPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to original position
        cameraTransform.localPosition = startPosition;
        isShaking = false;
        Debug.Log("Camera shake finished");
    }
}
