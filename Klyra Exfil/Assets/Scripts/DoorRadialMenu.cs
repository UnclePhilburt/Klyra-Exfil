using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Professional radial menu for door interactions with smooth graphics.
/// </summary>
public class DoorRadialMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    public float holdTime = 0.25f;
    public float menuRadius = 140f;
    public float optionSize = 80f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
    public Color segmentNormalColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    public Color segmentHoverColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color centerColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    public Color accentColor = new Color(0.4f, 0.7f, 1f, 1f);
    public Color textColor = Color.white;

    private bool isMenuOpen = false;
    private float holdTimer = 0f;
    private int selectedOption = -1;
    private Door targetDoor;
    private DoorInteractable doorInteractable;
    private DoorSnakeCam doorSnakeCam;
    private bool hasExecuted = false;

    private class MenuOption
    {
        public string name;
        public float angle;
        public System.Action action;
    }

    private MenuOption[] menuOptions;
    private Texture2D circleTexture;
    private GUIStyle labelStyle;

    public void SetDoor(Door door, DoorInteractable interactable)
    {
        targetDoor = door;
        doorInteractable = interactable;
        doorSnakeCam = door?.GetComponent<DoorSnakeCam>();
        hasExecuted = false;
        holdTimer = 0f;
        isMenuOpen = false;

        menuOptions = new MenuOption[]
        {
            new MenuOption { name = "OPEN", angle = 45f, action = () => targetDoor?.Toggle() },
            new MenuOption { name = "SNAKE CAM", angle = 135f, action = () => doorSnakeCam?.ToggleSnakeCam() },
            new MenuOption { name = "BREACH", angle = 225f, action = () => targetDoor?.Breach() },
            new MenuOption { name = "EXPLOSIVE", angle = 315f, action = () => doorInteractable?.PlaceExplosiveCharge() }
        };

        InitializeGraphics();
    }

    void InitializeGraphics()
    {
        if (circleTexture == null)
        {
            circleTexture = CreateCircleTexture(128);
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle();
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 16;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = textColor;
        }
    }

    public void UpdateMenu(bool isHoldingF)
    {
        if (isHoldingF)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= holdTime && !isMenuOpen)
            {
                OpenMenu();
            }

            if (isMenuOpen)
            {
                UpdateSelection();
            }
        }
        else
        {
            if (isMenuOpen && !hasExecuted)
            {
                ExecuteSelection();
                hasExecuted = true;
                CloseMenu();
            }
            else if (holdTimer > 0f && holdTimer < holdTime && !hasExecuted)
            {
                targetDoor?.Toggle();
                hasExecuted = true;
            }

            holdTimer = 0f;
            if (!isMenuOpen)
            {
                hasExecuted = false;
            }
        }
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        selectedOption = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void CloseMenu()
    {
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExecuteSelection()
    {
        if (selectedOption >= 0 && selectedOption < menuOptions.Length && !hasExecuted)
        {
            menuOptions[selectedOption].action?.Invoke();
        }
    }

    public void UpdateSelection()
    {
        if (!isMenuOpen || menuOptions == null) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 direction = mousePos - screenCenter;

        if (direction.magnitude < 40f)
        {
            selectedOption = 0;
            return;
        }

        // Invert Y direction for correct radial menu orientation
        float angle = Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        float closestDist = float.MaxValue;
        for (int i = 0; i < menuOptions.Length; i++)
        {
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, menuOptions[i].angle));
            if (angleDiff < closestDist)
            {
                closestDist = angleDiff;
                selectedOption = i;
            }
        }
    }

    void OnGUI()
    {
        if (!isMenuOpen || menuOptions == null) return;

        InitializeGraphics();

        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Dark overlay
        DrawFullScreenOverlay(new Color(0f, 0f, 0f, 0.6f));

        // Outer glow
        DrawCircle(center, menuRadius + 60f, new Color(accentColor.r, accentColor.g, accentColor.b, 0.1f));
        DrawCircle(center, menuRadius + 40f, new Color(accentColor.r, accentColor.g, accentColor.b, 0.2f));

        // Background circle
        DrawCircle(center, menuRadius + 20f, backgroundColor);

        // Draw segments
        for (int i = 0; i < menuOptions.Length; i++)
        {
            bool isSelected = (i == selectedOption);
            Color segmentColor = isSelected ? segmentHoverColor : segmentNormalColor;

            DrawSegment(center, menuRadius, menuOptions[i].angle, 120f, segmentColor, isSelected);
        }

        // Center circle
        DrawCircle(center, 70f, centerColor);
        DrawCircle(center, 68f, new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f));
        DrawCircle(center, 60f, centerColor);

        // Draw options
        for (int i = 0; i < menuOptions.Length; i++)
        {
            bool isSelected = (i == selectedOption);

            float angleRad = menuOptions[i].angle * Mathf.Deg2Rad;
            Vector2 pos = center + new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * menuRadius;

            // Option circle background
            Color optionBg = isSelected ? accentColor : new Color(0.2f, 0.2f, 0.25f, 0.9f);
            DrawCircle(pos, optionSize / 2f, optionBg);

            // Inner glow
            if (isSelected)
            {
                DrawCircle(pos, (optionSize / 2f) - 3f, new Color(1f, 1f, 1f, 0.3f));
            }

            // Icon circle
            DrawCircle(pos, (optionSize / 2f) - 5f, new Color(0.1f, 0.1f, 0.15f, 0.95f));

            // Text
            GUIStyle textStyle = new GUIStyle(labelStyle);
            textStyle.fontSize = isSelected ? 18 : 15;
            textStyle.normal.textColor = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.9f, 1f);

            Rect textRect = new Rect(pos.x - 80f, pos.y - 12f, 160f, 24f);
            GUI.Label(textRect, menuOptions[i].name, textStyle);
        }

        // Center text
        GUIStyle centerStyle = new GUIStyle(labelStyle);
        centerStyle.fontSize = 12;
        centerStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f, 1f);
        Rect centerRect = new Rect(center.x - 100f, center.y - 8f, 200f, 16f);
        GUI.Label(centerRect, "Release F", centerStyle);
    }

    void DrawFullScreenOverlay(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tex);
    }

    void DrawCircle(Vector2 center, float radius, Color color)
    {
        if (circleTexture == null) return;

        Color oldColor = GUI.color;
        GUI.color = color;

        float size = radius * 2f;
        Rect rect = new Rect(center.x - radius, center.y - radius, size, size);
        GUI.DrawTexture(rect, circleTexture);

        GUI.color = oldColor;
    }

    void DrawSegment(Vector2 center, float radius, float angle, float arc, Color color, bool isSelected)
    {
        float startAngle = angle - arc / 2f;
        float endAngle = angle + arc / 2f;

        int segments = 30;
        Vector2[] points = new Vector2[segments];

        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.Lerp(startAngle, endAngle, i / (float)(segments - 1)) * Mathf.Deg2Rad;
            float dist = isSelected ? radius + 10f : radius;
            points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
        }

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();

        for (int i = 0; i < segments - 1; i++)
        {
            DrawLine(points[i], points[i + 1], 25f, color);
        }
    }

    void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 direction = end - start;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float length = direction.magnitude;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();

        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness / 2f, length, thickness), tex);
        GUI.matrix = matrix;
    }

    Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01((distance - radius + 2f) / 4f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    public bool IsMenuOpen() => isMenuOpen;

    public void ForceClose()
    {
        CloseMenu();
        holdTimer = 0f;
        hasExecuted = false;
    }
}
