using UnityEngine;

/// <summary>
/// Simple crosshair that displays a small white dot in the center of the screen.
/// Attach this to your player prefab.
/// </summary>
public class SimpleCrosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Tooltip("Size of the crosshair dot in pixels")]
    public float dotSize = 4f;

    [Tooltip("Color of the crosshair")]
    public Color crosshairColor = Color.white;

    [Tooltip("Show the crosshair")]
    public bool showCrosshair = true;

    private Texture2D crosshairTexture;

    private void Start()
    {
        // Create a simple white dot texture
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
        crosshairTexture.Apply();
    }

    private void OnGUI()
    {
        if (!showCrosshair) return;

        // Calculate center of screen
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Draw the dot centered on screen
        GUI.color = crosshairColor;
        GUI.DrawTexture(
            new Rect(
                centerX - dotSize / 2f,
                centerY - dotSize / 2f,
                dotSize,
                dotSize
            ),
            crosshairTexture
        );
    }

    private void OnDestroy()
    {
        if (crosshairTexture != null)
        {
            Destroy(crosshairTexture);
        }
    }
}
