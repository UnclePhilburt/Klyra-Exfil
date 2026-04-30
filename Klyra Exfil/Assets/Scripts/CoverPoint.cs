using UnityEngine;

/// <summary>
/// Manual cover point that AI can use for tactical positioning.
/// Place these behind walls, crates, vehicles, etc.
/// </summary>
public class CoverPoint : MonoBehaviour
{
    [Header("Cover Properties")]
    [Tooltip("Type of cover (affects AI behavior)")]
    public CoverType coverType = CoverType.Stand;

    [Tooltip("Quality of this cover (1-10, higher = better)")]
    [Range(1, 10)]
    public int coverQuality = 5;

    [Tooltip("Maximum AI that can use this cover simultaneously")]
    public int maxOccupants = 1;

    [Tooltip("Is this cover currently available?")]
    public bool isAvailable = true;

    [Header("Cover Direction")]
    [Tooltip("Direction this cover protects from (blue arrow in scene)")]
    public bool autoDetectDirection = true;

    [Tooltip("Manual direction if auto-detect is off")]
    public Vector3 protectedDirection = Vector3.forward;

    [Header("Visual")]
    [Tooltip("Color in editor")]
    public Color gizmoColor = Color.green;

    [Tooltip("Show cover arc in editor")]
    public bool showCoverArc = true;

    private int currentOccupants = 0;
    private static readonly Color[] coverTypeColors = new Color[]
    {
        new Color(0, 1, 0, 0.3f),    // Stand - green
        new Color(0, 0.5f, 1, 0.3f), // Crouch - blue
        new Color(1, 0.5f, 0, 0.3f)  // Prone - orange
    };

    public enum CoverType
    {
        Stand,   // Full height cover (walls, tall obstacles)
        Crouch,  // Medium height (crates, low walls)
        Prone    // Very low cover (requires going prone)
    }

    void Start()
    {
        // Auto-detect protected direction based on raycast
        if (autoDetectDirection)
        {
            DetectCoverDirection();
        }
    }

    /// <summary>
    /// Automatically detect which direction this cover protects from
    /// </summary>
    void DetectCoverDirection()
    {
        // Raycast in all cardinal directions to find which one hits an obstacle first
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        float closestDistance = float.MaxValue;
        Vector3 bestDirection = transform.forward;

        foreach (var dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, out hit, 2f))
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    bestDirection = -dir; // Protect FROM the direction of the obstacle
                }
            }
        }

        protectedDirection = bestDirection;
    }

    /// <summary>
    /// Check if this cover point can be used
    /// </summary>
    public bool CanUse()
    {
        return isAvailable && currentOccupants < maxOccupants;
    }

    /// <summary>
    /// Check if this cover provides protection from a threat direction
    /// </summary>
    public bool ProvidesCoverFrom(Vector3 threatPosition)
    {
        Vector3 directionToThreat = (threatPosition - transform.position).normalized;
        float angle = Vector3.Angle(protectedDirection, directionToThreat);

        // Cover is effective if threat is within ~90 degrees of protected direction
        return angle < 90f;
    }

    /// <summary>
    /// Get score for this cover point (higher = better)
    /// </summary>
    public float GetCoverScore(Vector3 aiPosition, Vector3 threatPosition)
    {
        if (!CanUse()) return 0f;

        float score = coverQuality * 10f;

        // Prefer closer cover
        float distance = Vector3.Distance(aiPosition, transform.position);
        score -= distance * 2f;

        // Bonus if it provides protection from threat
        if (ProvidesCoverFrom(threatPosition))
        {
            score += 50f;
        }

        // Penalty if occupied
        score -= currentOccupants * 20f;

        return Mathf.Max(0f, score);
    }

    /// <summary>
    /// Reserve this cover point
    /// </summary>
    public bool Reserve()
    {
        if (!CanUse()) return false;

        currentOccupants++;
        return true;
    }

    /// <summary>
    /// Release this cover point
    /// </summary>
    public void Release()
    {
        currentOccupants = Mathf.Max(0, currentOccupants - 1);
    }

    /// <summary>
    /// Get the exact position where AI should move to
    /// </summary>
    public Vector3 GetCoverPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Get the direction AI should face when in this cover
    /// </summary>
    public Quaternion GetCoverRotation()
    {
        return Quaternion.LookRotation(protectedDirection);
    }

    void OnDrawGizmos()
    {
        // Draw cover point indicator
        Color color = isAvailable ? coverTypeColors[(int)coverType] : Color.gray;
        Gizmos.color = color;

        // Draw cube at cover position
        Vector3 size = Vector3.one * 0.5f;
        switch (coverType)
        {
            case CoverType.Stand:
                size.y = 1.8f;
                break;
            case CoverType.Crouch:
                size.y = 1.2f;
                break;
            case CoverType.Prone:
                size.y = 0.5f;
                break;
        }
        Gizmos.DrawCube(transform.position + Vector3.up * (size.y / 2f), size);

        // Draw protected direction arrow
        Vector3 dir = autoDetectDirection && Application.isPlaying
            ? protectedDirection
            : transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up, dir * 2f);

        // Draw cover arc if enabled
        if (showCoverArc)
        {
            DrawCoverArc(dir);
        }

        // Draw label
        #if UNITY_EDITOR
        string label = $"{coverType}\nQuality: {coverQuality}\n{currentOccupants}/{maxOccupants}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        #endif
    }

    void DrawCoverArc(Vector3 direction)
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);

        // Draw 90 degree arc showing protected area
        Vector3 rightBound = Quaternion.Euler(0, 45, 0) * direction;
        Vector3 leftBound = Quaternion.Euler(0, -45, 0) * direction;

        Gizmos.DrawRay(transform.position + Vector3.up, rightBound * 3f);
        Gizmos.DrawRay(transform.position + Vector3.up, leftBound * 3f);

        // Draw arc
        int segments = 10;
        Vector3 previousPoint = transform.position + Vector3.up + rightBound * 3f;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-45f, 45f, (float)i / segments);
            Vector3 newDirection = Quaternion.Euler(0, angle, 0) * direction;
            Vector3 newPoint = transform.position + Vector3.up + newDirection * 3f;
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw coverage range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 15f);
    }

    /// <summary>
    /// Find all cover points in the scene
    /// </summary>
    public static CoverPoint[] FindAllCoverPoints()
    {
        return FindObjectsOfType<CoverPoint>();
    }

    /// <summary>
    /// Find best cover point for AI
    /// </summary>
    public static CoverPoint FindBestCover(Vector3 aiPosition, Vector3 threatPosition, float maxRange = 20f)
    {
        return FindBestCover(aiPosition, threatPosition, maxRange, Vector3.zero, 0f);
    }

    /// <summary>
    /// Find best cover point, with option to prioritize cover within a territory
    /// </summary>
    /// <param name="aiPosition">AI's current position</param>
    /// <param name="threatPosition">Enemy position</param>
    /// <param name="maxRange">Max search range</param>
    /// <param name="territoryCenter">Center of territory (e.g., spawn position)</param>
    /// <param name="territoryRadius">Radius of territory to defend</param>
    /// <returns>Best cover point, prioritizing ones within territory</returns>
    public static CoverPoint FindBestCover(Vector3 aiPosition, Vector3 threatPosition, float maxRange, Vector3 territoryCenter, float territoryRadius)
    {
        CoverPoint[] allCover = FindAllCoverPoints();
        CoverPoint bestCover = null;
        float bestScore = float.MinValue;

        foreach (var cover in allCover)
        {
            // Skip if too far
            float distance = Vector3.Distance(aiPosition, cover.transform.position);
            if (distance > maxRange) continue;

            // Get base score
            float score = cover.GetCoverScore(aiPosition, threatPosition);

            // Bonus for cover within territory
            if (territoryRadius > 0f)
            {
                float distanceFromTerritory = Vector3.Distance(cover.transform.position, territoryCenter);
                if (distanceFromTerritory <= territoryRadius)
                {
                    // Inside territory - BIG bonus
                    score += 50f;
                }
                else
                {
                    // Outside territory - penalty
                    score -= 30f;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCover = cover;
            }
        }

        return bestCover;
    }
}
