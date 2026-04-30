using UnityEngine;
using System.Collections;

/// <summary>
/// Advanced combat tactics for TacticalAI that works with CoverPoint system.
/// Makes AI:
/// - Move to cover when not already there
/// - Shoot FROM cover (waist-high cover allows shooting)
/// - Dynamically relocate to different cover positions
/// - Vary behavior (sometimes aggressive, sometimes defensive)
/// - Change tactics based on cover type (Stand/Crouch/Prone)
///
/// Attach this to the same GameObject as TacticalAI.
/// </summary>
[RequireComponent(typeof(TacticalAI))]
public class AdvancedAICombatTactics : MonoBehaviour
{
    [Header("Combat Behavior")]
    [Tooltip("Shots to fire in the open before seeking cover")]
    [Range(1, 10)]
    public int shotsBeforeSeekingCover = 3;

    [Tooltip("Time after seeing player to seek cover (even if lost sight)")]
    [Range(0f, 10f)]
    public float timeBeforeSeekingCover = 2f;

    [Tooltip("Use shot count OR timer (whichever comes first)")]
    public bool useTimerOrShotCount = true;

    [Tooltip("How long AI stays at one cover position before considering relocation")]
    [Range(3f, 20f)]
    public float minTimeAtCover = 5f;
    [Range(5f, 30f)]
    public float maxTimeAtCover = 15f;

    [Tooltip("Chance to relocate after staying at cover for a while")]
    [Range(0f, 1f)]
    public float relocateChance = 0.5f;

    [Tooltip("Shots fired before considering relocation")]
    [Range(3, 20)]
    public int shotsBeforeRelocate = 8;

    [Header("Aggression")]
    [Tooltip("Current aggression level (randomized each combat)")]
    [Range(0f, 1f)]
    public float currentAggression = 0.5f;

    [Tooltip("How often to change aggression style")]
    public float aggressionChangeInterval = 20f;

    [Header("Flanking")]
    [Tooltip("Chance to attempt flanking when relocating")]
    [Range(0f, 1f)]
    public float flankingChance = 0.25f;

    [Tooltip("Distance to search for flanking positions")]
    public float flankingSearchRange = 15f;

    [Tooltip("Minimum angle for flanking position (degrees)")]
    [Range(30f, 90f)]
    public float minFlankAngle = 60f;

    [Header("Cover Preference")]
    [Tooltip("Prefer Stand cover when aggressive, Crouch when defensive")]
    public bool useCoverTypeStrategy = true;

    [Header("Engagement Range")]
    [Tooltip("Preferred distance to engage from cover")]
    [Range(5f, 30f)]
    public float preferredEngagementRange = 12f;

    [Tooltip("Will leave cover if enemy gets this close")]
    [Range(2f, 10f)]
    public float fallbackDistance = 4f;

    [Header("Defensive Territory")]
    [Tooltip("AI defends their area - won't chase player too far")]
    public bool defendTerritory = true;

    [Tooltip("Maximum distance AI will chase from spawn point")]
    [Range(5f, 50f)]
    public float maxChaseDistance = 20f;

    [Tooltip("If player leaves this range, AI returns to defensive position")]
    [Range(10f, 60f)]
    public float pursuitBreakDistance = 30f;

    [Header("Debug")]
    public bool debugTactics = false;

    // State
    private TacticalAI tacticalAI;
    private CoverPoint currentCover = null;
    private float timeAtCurrentCover = 0f;
    private float nextRelocateCheckTime = 0f;
    private int shotsFiredFromCurrentCover = 0;
    private int totalShotsFiredInCombat = 0;
    private float lastAggressionChangeTime = 0f;
    private bool isRelocating = false;
    private CoverPoint targetCover = null;
    private bool hasReservedTargetCover = false;
    private bool hasInitiallyEngaged = false; // True after firing first few shots
    private float combatStartTime = 0f; // When combat started
    private bool combatTimerStarted = false;
    private Vector3 spawnPosition; // Defensive territory anchor point

    // Combat style
    private CombatStyle currentStyle = CombatStyle.Balanced;

    public enum CombatStyle
    {
        Defensive,  // Stay in cover, only shoot when safe
        Balanced,   // Normal tactical play
        Aggressive  // Push forward, take risks
    }

    void Start()
    {
        tacticalAI = GetComponent<TacticalAI>();

        if (tacticalAI == null)
        {
            Debug.LogError($"[AdvancedAICombatTactics] No TacticalAI found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Save spawn position as defensive territory anchor
        spawnPosition = transform.position;

        // Start with random combat style
        RandomizeCombatStyle();

        Debug.Log($"[AdvancedAICombatTactics] Initialized on {gameObject.name} - Defensive territory: {spawnPosition}");
    }

    void Update()
    {
        // Only work when AI is in combat state
        if (tacticalAI == null || tacticalAI.currentState != TacticalAI.AIState.Combat)
        {
            ResetCombatState();
            return;
        }

        // Start combat timer when entering combat for the first time
        if (!combatTimerStarted)
        {
            combatStartTime = Time.time;
            combatTimerStarted = true;
            Debug.Log($"[{gameObject.name}] Combat started - timer begins");
        }

        // Periodically randomize combat style for variety
        if (Time.time - lastAggressionChangeTime > aggressionChangeInterval)
        {
            RandomizeCombatStyle();
            lastAggressionChangeTime = Time.time;
        }

        // Update combat tactics
        UpdateCombatTactics();
    }

    void RandomizeCombatStyle()
    {
        float roll = Random.value;

        if (roll < 0.25f)
        {
            currentStyle = CombatStyle.Defensive;
            currentAggression = Random.Range(0.2f, 0.4f);
        }
        else if (roll < 0.75f)
        {
            currentStyle = CombatStyle.Balanced;
            currentAggression = Random.Range(0.4f, 0.7f);
        }
        else
        {
            currentStyle = CombatStyle.Aggressive;
            currentAggression = Random.Range(0.7f, 1f);
        }

        if (debugTactics)
        {
            Debug.Log($"[{gameObject.name}] New combat style: {currentStyle} (aggression: {currentAggression:F2})");
        }
    }

    void UpdateCombatTactics()
    {
        // Get current cover from TacticalAI
        UpdateCurrentCoverReference();

        // Initial engagement phase - shoot first, THEN seek cover
        if (!hasInitiallyEngaged)
        {
            float timeInCombat = Time.time - combatStartTime;

            if (debugTactics && Time.frameCount % 60 == 0) // Log every 60 frames
            {
                Debug.Log($"[{gameObject.name}] Initial engagement: {totalShotsFiredInCombat}/{shotsBeforeSeekingCover} shots | {timeInCombat:F1}/{timeBeforeSeekingCover}s");
            }

            // Check if should seek cover (either by shot count OR timer)
            bool shouldSeekByShotCount = totalShotsFiredInCombat >= shotsBeforeSeekingCover;
            bool shouldSeekByTimer = timeInCombat >= timeBeforeSeekingCover;

            bool shouldSeek = false;
            string reason = "";

            if (useTimerOrShotCount)
            {
                // Whichever comes first
                if (shouldSeekByShotCount)
                {
                    shouldSeek = true;
                    reason = $"shot count reached ({totalShotsFiredInCombat} shots)";
                }
                else if (shouldSeekByTimer)
                {
                    shouldSeek = true;
                    reason = $"timer expired ({timeInCombat:F1}s)";
                }
            }
            else
            {
                // Require BOTH conditions
                if (shouldSeekByShotCount && shouldSeekByTimer)
                {
                    shouldSeek = true;
                    reason = $"both conditions met ({totalShotsFiredInCombat} shots + {timeInCombat:F1}s)";
                }
            }

            if (shouldSeek)
            {
                hasInitiallyEngaged = true;

                Debug.Log($"[{gameObject.name}] *** INITIAL ENGAGEMENT COMPLETE: {reason} - SEEKING COVER NOW ***");

                // Now seek cover
                SeekInitialCover();
            }
            // Still in initial engagement - don't seek cover yet, just keep shooting
            return;
        }

        // Update time at cover
        if (currentCover != null && !isRelocating)
        {
            timeAtCurrentCover += Time.deltaTime;
        }

        // Check if we're at cover
        if (currentCover != null)
        {
            // At cover - decide if we should relocate
            if (Time.time >= nextRelocateCheckTime && !isRelocating)
            {
                CheckShouldRelocate();
            }
        }
        else
        {
            // Not at cover after initial engagement - try to find some
            // But don't panic if there's none available
            if (Random.value < 0.1f) // Check occasionally, not every frame
            {
                SeekInitialCover();
            }

            timeAtCurrentCover = 0f;
            shotsFiredFromCurrentCover = 0;
        }

        // Handle relocation if active
        if (isRelocating && targetCover != null)
        {
            UpdateRelocation();
        }

        // Check if enemy is too close (need to fall back)
        CheckFallbackCondition();

        // Check if player has left our defensive territory
        if (defendTerritory)
        {
            CheckTerritoryDefense();
        }
    }

    void UpdateCurrentCoverReference()
    {
        // Use reflection to get current cover from TacticalAI
        var currentCoverField = typeof(TacticalAI).GetField("currentCoverPoint",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        CoverPoint aiCover = currentCoverField?.GetValue(tacticalAI) as CoverPoint;

        // Check if cover changed
        if (aiCover != currentCover)
        {
            // Moved to new cover
            if (aiCover != null && debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Now at cover: {aiCover.name} ({aiCover.coverType})");
            }

            currentCover = aiCover;
            timeAtCurrentCover = 0f;
            shotsFiredFromCurrentCover = 0;
            isRelocating = false;

            // Set next relocation check time
            nextRelocateCheckTime = Time.time + Random.Range(minTimeAtCover, maxTimeAtCover);
        }
    }

    void SeekInitialCover()
    {
        Debug.Log($"[{gameObject.name}] *** SEEKING INITIAL COVER - Calling ForceSeekCover() ***");

        // Use TacticalAI's built-in cover seeking
        if (tacticalAI != null)
        {
            tacticalAI.ForceSeekCover();

            // Double check that cover was actually set
            var currentCoverField = typeof(TacticalAI).GetField("currentCoverPoint",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            CoverPoint assignedCover = currentCoverField?.GetValue(tacticalAI) as CoverPoint;

            if (assignedCover != null)
            {
                Debug.Log($"[{gameObject.name}] SUCCESS! Cover assigned: {assignedCover.name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] WARNING! No cover was assigned - might be none available");
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ERROR! tacticalAI is null!");
        }
    }

    void CheckShouldRelocate()
    {
        // Decide if AI should move to different cover
        bool shouldRelocate = false;
        string reason = "";

        // Check time at cover
        if (timeAtCurrentCover >= maxTimeAtCover)
        {
            shouldRelocate = Random.value < relocateChance;
            reason = "time limit reached";
        }

        // Check shots fired
        if (shotsFiredFromCurrentCover >= shotsBeforeRelocate)
        {
            shouldRelocate = Random.value < relocateChance * 1.5f; // Higher chance
            reason = "fired too many shots";
        }

        // Aggressive AI relocates more often
        if (currentStyle == CombatStyle.Aggressive && Random.value < 0.3f)
        {
            shouldRelocate = true;
            reason = "aggressive push";
        }

        if (shouldRelocate)
        {
            InitiateRelocation(reason);
        }
        else
        {
            // Stay at current cover, check again later
            nextRelocateCheckTime = Time.time + Random.Range(3f, 8f);
        }
    }

    void InitiateRelocation(string reason)
    {
        if (debugTactics)
        {
            Debug.Log($"[{gameObject.name}] Initiating relocation: {reason}");
        }

        // Decide relocation type based on aggression
        bool attemptFlank = Random.value < flankingChance && currentAggression > 0.5f;

        CoverPoint newCover = null;

        if (attemptFlank)
        {
            newCover = FindFlankingCover();
            if (newCover != null && debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Found flanking cover: {newCover.name}");
            }
        }

        // If no flanking cover, find best tactical cover
        if (newCover == null)
        {
            newCover = FindBestTacticalCover();
        }

        if (newCover != null && newCover != currentCover)
        {
            // Reserve the new cover
            if (newCover.Reserve())
            {
                // Release old target cover if we had one
                if (hasReservedTargetCover && targetCover != null)
                {
                    targetCover.Release();
                }

                targetCover = newCover;
                hasReservedTargetCover = true;
                isRelocating = true;

                // Force TacticalAI to move to new cover
                ForceMoveToCover(newCover);

                if (debugTactics)
                {
                    Debug.Log($"[{gameObject.name}] Relocating to: {newCover.name} ({newCover.coverType})");
                }
            }
        }
        else
        {
            if (debugTactics)
            {
                if (newCover == currentCover)
                {
                    Debug.Log($"[{gameObject.name}] Already at best cover - staying put");
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] No other cover available - staying at current position");
                }
            }

            // Stay at current position (this is fine!)
            nextRelocateCheckTime = Time.time + Random.Range(5f, 10f);
        }
    }

    CoverPoint FindBestTacticalCover()
    {
        // Get current target
        var currentTargetField = typeof(TacticalAI).GetField("currentTarget",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        Transform currentTarget = currentTargetField?.GetValue(tacticalAI) as Transform;
        if (currentTarget == null) return null;

        // Find all cover in range
        CoverPoint[] allCover = CoverPoint.FindAllCoverPoints();
        CoverPoint bestCover = null;
        float bestScore = float.MinValue;

        foreach (var cover in allCover)
        {
            // Skip current cover
            if (cover == currentCover) continue;

            // Skip if too far
            float distance = Vector3.Distance(transform.position, cover.transform.position);
            if (distance > tacticalAI.coverSearchRange) continue;

            // DEFENSIVE: Skip cover that's too far from spawn point (outside our territory)
            if (defendTerritory)
            {
                float distanceFromSpawn = Vector3.Distance(cover.transform.position, spawnPosition);
                if (distanceFromSpawn > maxChaseDistance)
                {
                    if (debugTactics)
                    {
                        Debug.Log($"[{gameObject.name}] Skipping {cover.name} - outside defensive territory ({distanceFromSpawn:F1}m from spawn)");
                    }
                    continue; // Too far from our defensive position
                }
            }

            // Get base score
            float score = cover.GetCoverScore(transform.position, currentTarget.position);

            // Apply cover type preference based on combat style
            if (useCoverTypeStrategy)
            {
                if (currentStyle == CombatStyle.Aggressive && cover.coverType == CoverPoint.CoverType.Stand)
                {
                    score += 20f; // Aggressive AI prefers standing cover
                }
                else if (currentStyle == CombatStyle.Defensive && cover.coverType == CoverPoint.CoverType.Crouch)
                {
                    score += 20f; // Defensive AI prefers crouching cover
                }
            }

            // Prefer cover closer to preferred engagement range
            float distanceToCover = Vector3.Distance(cover.transform.position, currentTarget.position);
            float rangeDifference = Mathf.Abs(distanceToCover - preferredEngagementRange);
            score -= rangeDifference * 2f;

            // DEFENSIVE: Prefer cover closer to spawn point (defend our territory)
            if (defendTerritory)
            {
                float distanceFromSpawn = Vector3.Distance(cover.transform.position, spawnPosition);
                float territoryBonus = (maxChaseDistance - distanceFromSpawn) * 0.5f; // Closer to spawn = better
                score += territoryBonus;
            }

            // Check if this is better than current best
            if (score > bestScore)
            {
                bestScore = score;
                bestCover = cover;
            }
        }

        return bestCover;
    }

    CoverPoint FindFlankingCover()
    {
        // Get current target
        var currentTargetField = typeof(TacticalAI).GetField("currentTarget",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        Transform currentTarget = currentTargetField?.GetValue(tacticalAI) as Transform;
        if (currentTarget == null) return null;

        // Calculate flanking direction
        Vector3 toTarget = (currentTarget.position - transform.position).normalized;
        Vector3 currentPosition = transform.position;

        // Find all cover
        CoverPoint[] allCover = CoverPoint.FindAllCoverPoints();
        CoverPoint bestFlankCover = null;
        float bestFlankScore = float.MinValue;

        foreach (var cover in allCover)
        {
            // Skip current cover
            if (cover == currentCover) continue;

            // Check if cover is in flanking range
            float distance = Vector3.Distance(currentPosition, cover.transform.position);
            if (distance > flankingSearchRange) continue;

            // Calculate angle from current position to target
            Vector3 toCover = (cover.transform.position - currentPosition).normalized;
            float angle = Vector3.Angle(toTarget, toCover);

            // Check if this is a flanking angle
            if (angle >= minFlankAngle && angle <= 120f)
            {
                // This is a flanking position!
                float score = cover.GetCoverScore(currentPosition, currentTarget.position);

                // Bonus for better flanking angle (closer to 90 degrees is better)
                float angleBonus = 100f - Mathf.Abs(90f - angle);
                score += angleBonus;

                if (score > bestFlankScore)
                {
                    bestFlankScore = score;
                    bestFlankCover = cover;
                }
            }
        }

        return bestFlankCover;
    }

    void ForceMoveToCover(CoverPoint cover)
    {
        // Use reflection to set the cover directly in TacticalAI
        var hasValidCoverField = typeof(TacticalAI).GetField("hasValidCover",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        var currentCoverField = typeof(TacticalAI).GetField("currentCoverPoint",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (hasValidCoverField != null && currentCoverField != null)
        {
            // Release old cover
            CoverPoint oldCover = currentCoverField.GetValue(tacticalAI) as CoverPoint;
            if (oldCover != null && oldCover != cover)
            {
                oldCover.Release();
            }

            // Set new cover as target
            currentCoverField.SetValue(tacticalAI, cover);
            hasValidCoverField.SetValue(tacticalAI, true);

            if (debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Set TacticalAI cover to: {cover.name}");
            }
        }
    }

    void UpdateRelocation()
    {
        // Check if we've reached target cover
        if (targetCover == null) return;

        float distanceToCover = Vector3.Distance(transform.position, targetCover.transform.position);

        if (distanceToCover < 1.5f)
        {
            // Reached cover
            if (debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Reached new cover: {targetCover.name}");
            }

            // Clean up relocation state
            targetCover = null;
            hasReservedTargetCover = false;
            isRelocating = false;
            timeAtCurrentCover = 0f;
            shotsFiredFromCurrentCover = 0;

            // Set next check time
            nextRelocateCheckTime = Time.time + Random.Range(minTimeAtCover, maxTimeAtCover);
        }
    }

    void CheckFallbackCondition()
    {
        // If enemy gets too close, fall back to different cover
        var currentTargetField = typeof(TacticalAI).GetField("currentTarget",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        Transform currentTarget = currentTargetField?.GetValue(tacticalAI) as Transform;
        if (currentTarget == null) return;

        float distanceToEnemy = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToEnemy < fallbackDistance && !isRelocating)
        {
            if (debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Enemy too close! Falling back!");
            }

            InitiateRelocation("enemy too close");
        }
    }

    /// <summary>
    /// Called by TacticalAI when it takes damage
    /// </summary>
    public void OnTakingFire()
    {
        // Increment shot counter
        shotsFiredFromCurrentCover++;

        // Higher chance to relocate when taking hits
        if (!isRelocating && Random.value < relocateChance * 2f)
        {
            if (debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Taking fire - emergency relocation!");
            }

            InitiateRelocation("taking fire");
        }
    }

    /// <summary>
    /// AI can always shoot from cover (waist-high cover allows shooting)
    /// </summary>
    public bool ShouldAllowShooting()
    {
        // Always allow shooting - AI shoots FROM cover, not hiding behind it
        return true;
    }

    /// <summary>
    /// Called when AI fires a shot - track for relocation decision
    /// </summary>
    public void OnShotFired()
    {
        shotsFiredFromCurrentCover++;
        totalShotsFiredInCombat++;

        // ALWAYS log during initial engagement for debugging
        if (!hasInitiallyEngaged)
        {
            Debug.Log($"[{gameObject.name}] *** SHOT FIRED {totalShotsFiredInCombat}/{shotsBeforeSeekingCover} - Still in initial engagement ***");
        }
        else if (debugTactics)
        {
            Debug.Log($"[{gameObject.name}] Shot from cover: {shotsFiredFromCurrentCover} total from this position");
        }
    }

    void ResetCombatState()
    {
        currentCover = null;
        timeAtCurrentCover = 0f;
        shotsFiredFromCurrentCover = 0;
        totalShotsFiredInCombat = 0;
        hasInitiallyEngaged = false;
        isRelocating = false;
        combatTimerStarted = false;
        combatStartTime = 0f;

        // Release reserved cover
        if (hasReservedTargetCover && targetCover != null)
        {
            targetCover.Release();
            targetCover = null;
            hasReservedTargetCover = false;
        }
    }

    void OnDestroy()
    {
        // Clean up any reserved cover
        if (hasReservedTargetCover && targetCover != null)
        {
            targetCover.Release();
        }
    }

    void CheckTerritoryDefense()
    {
        // Get current target
        var currentTargetField = typeof(TacticalAI).GetField("currentTarget",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        Transform currentTarget = currentTargetField?.GetValue(tacticalAI) as Transform;
        if (currentTarget == null) return;

        // Check if player has left our pursuit range
        float distanceToPlayer = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToPlayer > pursuitBreakDistance)
        {
            // Player too far - stop pursuing, return to defensive position
            if (debugTactics)
            {
                Debug.Log($"[{gameObject.name}] Player left pursuit range ({distanceToPlayer:F1}m > {pursuitBreakDistance}m) - breaking contact");
            }

            // This will be handled by TacticalAI transitioning to Investigate/Patrol
            // We just stop trying to relocate
            isRelocating = false;

            if (hasReservedTargetCover && targetCover != null)
            {
                targetCover.Release();
                targetCover = null;
                hasReservedTargetCover = false;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!debugTactics) return;

        // Draw current combat style
        Gizmos.color = currentStyle switch
        {
            CombatStyle.Defensive => Color.blue,
            CombatStyle.Balanced => Color.yellow,
            CombatStyle.Aggressive => Color.red,
            _ => Color.white
        };

        Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);

        // Draw target cover if relocating
        if (isRelocating && targetCover != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetCover.transform.position);
            Gizmos.DrawWireSphere(targetCover.transform.position, 1f);
        }

        // Draw current cover
        if (currentCover != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentCover.transform.position);
        }

        // Draw engagement range
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, preferredEngagementRange);

        // Draw fallback distance
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, fallbackDistance);

        // Draw defensive territory
        if (defendTerritory)
        {
            Vector3 territoryCenter = Application.isPlaying ? spawnPosition : transform.position;

            // Draw max chase distance (where AI stops pursuing)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(territoryCenter, maxChaseDistance);

            // Draw pursuit break distance (where AI gives up entirely)
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(territoryCenter, pursuitBreakDistance);

            // Draw line to spawn point
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, territoryCenter);
            Gizmos.DrawWireCube(territoryCenter, Vector3.one * 0.5f);
        }
    }
}
