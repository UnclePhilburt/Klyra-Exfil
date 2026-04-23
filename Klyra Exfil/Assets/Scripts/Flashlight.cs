using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple flashlight that follows the player's head/camera and toggles with L key.
/// Attach this to the player character.
/// </summary>
public class Flashlight : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [Tooltip("The camera/head transform to follow")]
    public Transform playerCamera;

    [Tooltip("Key to toggle flashlight")]
    public Key toggleKey = Key.L;

    [Header("Light Settings")]
    [Tooltip("Light intensity when on")]
    public float intensity = 3f;

    [Tooltip("Light range")]
    public float range = 20f;

    [Tooltip("Spotlight angle")]
    public float spotAngle = 60f;

    [Tooltip("Light color")]
    public Color lightColor = Color.white;

    private Light flashlightLight;
    private bool isOn = false;

    void Start()
    {
        // Try to find camera if not assigned
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
            }
            else
            {
                Debug.LogWarning("Flashlight: No camera found! Please assign playerCamera in inspector.");
            }
        }

        // Create the flashlight light
        GameObject flashlightObject = new GameObject("Flashlight");
        flashlightObject.transform.SetParent(playerCamera);
        flashlightObject.transform.localPosition = Vector3.zero;
        flashlightObject.transform.localRotation = Quaternion.identity;

        flashlightLight = flashlightObject.AddComponent<Light>();
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = intensity;
        flashlightLight.range = range;
        flashlightLight.spotAngle = spotAngle;
        flashlightLight.color = lightColor;
        flashlightLight.shadows = LightShadows.Soft;
        flashlightLight.enabled = false; // Start off

        Debug.Log("Flashlight created! Press L to toggle.");
    }

    void Update()
    {
        // Toggle flashlight with L key
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleFlashlight();
        }
    }

    void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlightLight.enabled = isOn;
        Debug.Log($"Flashlight: {(isOn ? "ON" : "OFF")}");
    }

    public bool IsOn() => isOn;
}
