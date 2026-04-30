using UnityEngine;

/// <summary>
/// Randomly decides if a light should be on or off when the scene loads.
/// Attach this to any GameObject with a Light component.
/// </summary>
[RequireComponent(typeof(Light))]
public class RandomLightState : MonoBehaviour
{
    [Header("Random State Settings")]
    [Tooltip("Chance that the light will be ON when the scene loads (0 = always off, 1 = always on)")]
    [Range(0f, 1f)]
    public float chanceToBeOn = 0.5f;

    [Header("Optional: Also Toggle GameObject")]
    [Tooltip("If true, will disable the entire GameObject when the light is off")]
    public bool disableGameObject = false;

    private void Awake()
    {
        Light lightComponent = GetComponent<Light>();

        // Random roll to see if light should be on
        bool shouldBeOn = Random.value <= chanceToBeOn;

        if (disableGameObject)
        {
            // Disable the entire GameObject
            gameObject.SetActive(shouldBeOn);
        }
        else
        {
            // Just enable/disable the light component
            lightComponent.enabled = shouldBeOn;
        }

        Debug.Log($"[RandomLightState] {gameObject.name} is {(shouldBeOn ? "ON" : "OFF")}");
    }
}
