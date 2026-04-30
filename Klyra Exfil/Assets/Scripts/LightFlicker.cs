using UnityEngine;

/// <summary>
/// Makes a light flicker at random or specified intervals.
/// Attach this to any GameObject with a Light component.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Mode")]
    [Tooltip("If true, uses random intervals. If false, uses the specified interval.")]
    public bool useRandomIntervals = true;

    [Header("Random Interval Settings")]
    [Tooltip("Minimum time between flickers (in seconds)")]
    public float minFlickerInterval = 0.05f;

    [Tooltip("Maximum time between flickers (in seconds)")]
    public float maxFlickerInterval = 0.5f;

    [Header("Fixed Interval Settings")]
    [Tooltip("Fixed time between flickers (in seconds). Only used when useRandomIntervals is false.")]
    public float fixedFlickerInterval = 0.2f;

    [Header("Flicker Behavior")]
    [Tooltip("Minimum intensity when flickering (0 = off)")]
    [Range(0f, 8f)]
    public float minIntensity = 0f;

    [Tooltip("Maximum intensity when flickering")]
    [Range(0f, 8f)]
    public float maxIntensity = 1f;

    [Tooltip("How smooth the flicker is (higher = smoother, 0 = instant snap)")]
    [Range(0f, 1f)]
    public float smoothness = 0.1f;

    [Tooltip("If true, the light will flicker on/off. If false, it will vary intensity smoothly.")]
    public bool snapOnOff = false;

    private Light lightComponent;
    private float nextFlickerTime;
    private float targetIntensity;
    private float originalIntensity;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        originalIntensity = lightComponent.intensity;

        // Start with max intensity
        targetIntensity = maxIntensity;

        // Schedule first flicker
        ScheduleNextFlicker();
    }

    private void Update()
    {
        // Check if it's time to flicker
        if (Time.time >= nextFlickerTime)
        {
            Flicker();
            ScheduleNextFlicker();
        }

        // Smoothly transition to target intensity (if smoothness > 0)
        if (smoothness > 0f && !snapOnOff)
        {
            lightComponent.intensity = Mathf.Lerp(
                lightComponent.intensity,
                targetIntensity,
                Time.deltaTime / smoothness
            );
        }
    }

    private void Flicker()
    {
        if (snapOnOff)
        {
            // Snap between on and off
            lightComponent.intensity = lightComponent.intensity > 0.5f ? minIntensity : maxIntensity;
            targetIntensity = lightComponent.intensity;
        }
        else
        {
            // Pick a random intensity
            targetIntensity = Random.Range(minIntensity, maxIntensity);

            // If no smoothness, snap immediately
            if (smoothness == 0f)
            {
                lightComponent.intensity = targetIntensity;
            }
        }
    }

    private void ScheduleNextFlicker()
    {
        if (useRandomIntervals)
        {
            nextFlickerTime = Time.time + Random.Range(minFlickerInterval, maxFlickerInterval);
        }
        else
        {
            nextFlickerTime = Time.time + fixedFlickerInterval;
        }
    }

    private void OnValidate()
    {
        // Make sure min is not greater than max
        if (minIntensity > maxIntensity)
        {
            minIntensity = maxIntensity;
        }

        if (minFlickerInterval > maxFlickerInterval)
        {
            minFlickerInterval = maxFlickerInterval;
        }
    }
}
