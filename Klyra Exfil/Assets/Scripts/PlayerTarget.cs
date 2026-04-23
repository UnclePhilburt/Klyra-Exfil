using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any GameObject that AI should treat as a targetable player.
/// The AI reads from the static registry — no per-frame scene scans.
/// </summary>
public class PlayerTarget : MonoBehaviour
{
    private static readonly List<PlayerTarget> s_All = new List<PlayerTarget>();

    [Tooltip("Transform the AI should aim at. Defaults to this transform if unset (usually the chest or head).")]
    public Transform aimPoint;

    public static IReadOnlyList<PlayerTarget> All => s_All;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    private void OnEnable()
    {
        if (!s_All.Contains(this)) s_All.Add(this);
    }

    private void OnDisable()
    {
        s_All.Remove(this);
    }
}
