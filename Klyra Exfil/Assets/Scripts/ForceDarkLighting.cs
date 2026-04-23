using UnityEngine;

/// <summary>
/// Forces scene lighting to be very dark by directly controlling the main directional light
/// and ambient lighting. Attach this to any GameObject in the scene.
/// </summary>
public class ForceDarkLighting : MonoBehaviour
{
    [Header("Darkness Settings")]
    [Range(0f, 1f)]
    [Tooltip("Overall darkness multiplier (0 = pitch black, 1 = normal)")]
    public float darknessLevel = 0.2f;

    [Header("Light Override")]
    [Tooltip("Override the main directional light?")]
    public bool overrideDirectionalLight = true;

    [Tooltip("Override ambient lighting?")]
    public bool overrideAmbientLight = true;

    private Light mainLight;
    private float originalLightIntensity;

    void Start()
    {
        // Find the main directional light (sun)
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                mainLight = light;
                originalLightIntensity = light.intensity;
                Debug.Log($"Found directional light: {light.name}, original intensity: {originalLightIntensity}");
                break;
            }
        }

        ApplyDarkness();
    }

    void Update()
    {
        // Continuously apply darkness (in case Cozy tries to override)
        ApplyDarkness();
    }

    void ApplyDarkness()
    {
        // Override directional light
        if (overrideDirectionalLight && mainLight != null)
        {
            mainLight.intensity = darknessLevel;
            mainLight.color = Color.Lerp(Color.black, mainLight.color, darknessLevel);
        }

        // Override ambient light
        if (overrideAmbientLight)
        {
            RenderSettings.ambientIntensity = darknessLevel;
            RenderSettings.reflectionIntensity = darknessLevel;

            // Make ambient color darker
            Color darkAmbient = Color.Lerp(Color.black, RenderSettings.ambientSkyColor, darknessLevel);
            RenderSettings.ambientSkyColor = darkAmbient;
            RenderSettings.ambientEquatorColor = darkAmbient;
            RenderSettings.ambientGroundColor = darkAmbient;
        }
    }

    void OnValidate()
    {
        // Apply changes in editor when slider is moved
        if (Application.isPlaying)
        {
            ApplyDarkness();
        }
    }
}
