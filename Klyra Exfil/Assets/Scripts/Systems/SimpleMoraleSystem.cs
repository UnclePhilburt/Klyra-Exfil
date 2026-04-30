using UnityEngine;

namespace Klyra.AI
{
    /// <summary>
    /// SIMPLE morale system with probability-based surrender.
    /// Bots have a CHANCE to surrender based on:
    /// - How wounded they are
    /// - How outnumbered they are
    /// - Their personality (brave vs coward)
    /// No guaranteed surrenders - just chances that increase with bad situations.
    /// </summary>
    public class SimpleMoraleSystem : MonoBehaviour
    {
        [Header("Personality")]
        [Tooltip("Base bravery (0 = coward, 1 = fearless). Randomized on spawn.")]
        [Range(0f, 1f)]
        public float bravery = 0.5f;

        [Header("Surrender Probability")]
        [Tooltip("Max chance to surrender when critically wounded (0-1)")]
        [Range(0f, 1f)]
        public float maxWoundedSurrenderChance = 0.6f; // 60% at low health

        [Tooltip("Max chance to surrender when heavily outnumbered (0-1)")]
        [Range(0f, 1f)]
        public float maxOutnumberedSurrenderChance = 0.4f; // 40% when outnumbered

        [Header("Check Interval")]
        [Tooltip("Check surrender conditions every X seconds")]
        public float checkInterval = 20f; // Check every 20 seconds instead of 5

        [Header("Debug")]
        public bool debugSurrender = true;

        private TacticalAI tacticalAI;
        private Opsive.UltimateCharacterController.Traits.Health health;
        private float nextCheckTime = 0f;
        private bool hasSurrendered = false;

        void Start()
        {
            tacticalAI = GetComponent<TacticalAI>();
            health = GetComponent<Opsive.UltimateCharacterController.Traits.Health>();

            if (tacticalAI == null)
            {
                Debug.LogError($"[SimpleMoraleSystem] No TacticalAI on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Randomize bravery for variety (some bots are braver than others)
            bravery = Random.Range(0.3f, 0.9f); // Most bots are somewhat brave

            if (debugSurrender)
            {
                Debug.Log($"[SimpleMoraleSystem] {gameObject.name} bravery: {bravery:F2} (0=coward, 1=fearless)");
            }
        }

        void Update()
        {
            if (hasSurrendered) return;
            if (tacticalAI.currentState == TacticalAI.AIState.Compliant) return;
            if (tacticalAI.currentState == TacticalAI.AIState.Flashbanged) return;

            // Only check surrender if in combat or investigate state
            // Don't check while patrolling peacefully
            if (tacticalAI.currentState != TacticalAI.AIState.Combat &&
                tacticalAI.currentState != TacticalAI.AIState.Investigate)
            {
                if (debugSurrender && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[SimpleMoraleSystem] {gameObject.name} is in {tacticalAI.currentState} state - not checking surrender");
                }
                return;
            }

            if (debugSurrender && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SimpleMoraleSystem] {gameObject.name} is in {tacticalAI.currentState} state - will check surrender at {nextCheckTime:F1}s (current time: {Time.time:F1}s)");
            }

            // Check surrender conditions periodically
            if (Time.time >= nextCheckTime)
            {
                Debug.Log($"[SimpleMoraleSystem] {gameObject.name} CHECKING SURRENDER NOW (state: {tacticalAI.currentState})");
                CheckSurrender();
                nextCheckTime = Time.time + checkInterval;
            }
        }

        void CheckSurrender()
        {
            float surrenderChance = 0f;

            // Factor 1: Health
            if (health != null && health.IsAlive())
            {
                float healthPercent = GetHealthPercentage();

                // Low health increases surrender chance
                // 100% HP = 0% chance, 0% HP = max chance
                float healthFactor = 1f - healthPercent;
                float woundedChance = healthFactor * maxWoundedSurrenderChance;
                surrenderChance += woundedChance;

                if (debugSurrender && woundedChance > 0.1f)
                {
                    Debug.Log($"[SimpleMoraleSystem] {gameObject.name} health: {healthPercent * 100f:F0}% - adds {woundedChance * 100f:F0}% surrender chance");
                }
            }

            // Factor 2: Outnumbered
            int allyCount = CountNearbyAllies();
            int enemyCount = CountNearbyEnemies();

            if (enemyCount > 0)
            {
                int totalAllies = allyCount + 1; // +1 for self

                // Only add surrender chance if ACTUALLY outnumbered
                if (enemyCount > totalAllies)
                {
                    // Calculate how outnumbered we are
                    // 1v2 = 0.5 ratio, 1v3 = 0.67 ratio, 1v4 = 0.75 ratio
                    float outnumberedRatio = (enemyCount - totalAllies) / (float)enemyCount;
                    float outnumberedChance = outnumberedRatio * maxOutnumberedSurrenderChance;
                    surrenderChance += outnumberedChance;

                    if (debugSurrender && outnumberedChance > 0.05f)
                    {
                        Debug.Log($"[SimpleMoraleSystem] {gameObject.name} outnumbered: {totalAllies} vs {enemyCount} - adds {outnumberedChance * 100f:F0}% surrender chance");
                    }
                }
                else if (debugSurrender)
                {
                    Debug.Log($"[SimpleMoraleSystem] {gameObject.name} has advantage: {totalAllies} allies vs {enemyCount} enemies - no surrender penalty");
                }
            }

            // Apply bravery modifier
            // Brave bots are less likely to surrender
            surrenderChance *= (1f - bravery * 0.5f); // Bravery reduces chance by up to 50%

            // Cap at reasonable max
            surrenderChance = Mathf.Clamp01(surrenderChance);

            if (debugSurrender && surrenderChance > 0.05f)
            {
                Debug.Log($"[SimpleMoraleSystem] {gameObject.name} total surrender chance: {surrenderChance * 100f:F0}% (bravery: {bravery:F2})");
            }

            // Roll the dice!
            float roll = Random.value;
            if (surrenderChance > 0f && roll < surrenderChance)
            {
                // Build detailed surrender reason
                string reason = "";
                float healthPercent = GetHealthPercentage();

                if (healthPercent < 0.3f)
                {
                    reason += $"CRITICALLY WOUNDED ({healthPercent * 100f:F0}% HP)";
                }
                else if (healthPercent < 0.5f)
                {
                    reason += $"Wounded ({healthPercent * 100f:F0}% HP)";
                }

                if (enemyCount > allyCount + 1)
                {
                    if (reason.Length > 0) reason += " + ";
                    reason += $"OUTNUMBERED ({allyCount + 1} allies vs {enemyCount} enemies)";
                }

                if (reason.Length == 0)
                {
                    reason = "Random chance";
                }

                Debug.LogWarning($"*** SURRENDER *** {gameObject.name} is SURRENDERING!\n" +
                                $"Component: SimpleMoraleSystem\n" +
                                $"Reason: {reason}\n" +
                                $"Surrender Chance: {surrenderChance * 100f:F1}%\n" +
                                $"Dice Roll: {roll:F2} (needed < {surrenderChance:F2})\n" +
                                $"Bravery: {bravery:F2}\n" +
                                $"Health: {healthPercent * 100f:F0}%\n" +
                                $"Nearby: {allyCount} allies, {enemyCount} enemies");

                Surrender();
            }
        }

        void Surrender()
        {
            hasSurrendered = true;

            // Get stack trace to see what called this
            Debug.LogWarning($"[SimpleMoraleSystem] {gameObject.name} SURRENDERED - Stack trace:\n{System.Environment.StackTrace}");

            if (tacticalAI != null)
            {
                StartCoroutine(tacticalAI.ComplyWithCommand());
            }
        }

        int CountNearbyAllies()
        {
            int count = 0;
            // Increased range to 50m so allies across the building are counted
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 50f);

            foreach (var col in nearbyColliders)
            {
                if (col.gameObject == gameObject) continue;

                // Check parent in case collider is on child object
                var ai = col.GetComponent<TacticalAI>();
                if (ai == null)
                    ai = col.GetComponentInParent<TacticalAI>();

                if (ai != null && ai.enabled)
                {
                    var aiHealth = ai.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
                    if (aiHealth != null && aiHealth.IsAlive())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        int CountNearbyEnemies()
        {
            int count = 0;
            var players = PlayerTarget.All;

            foreach (var player in players)
            {
                if (player == null) continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= 50f) // Match ally detection range
                {
                    count++;
                }
            }

            return count;
        }

        float GetHealthPercentage()
        {
            if (health == null) return 1f;

            try
            {
                var healthType = health.GetType();

                float currentHealth = 0f;
                var valueProperty = healthType.GetProperty("Value");
                if (valueProperty != null)
                {
                    currentHealth = (float)valueProperty.GetValue(health);
                }

                float maxHealth = 100f;
                var maxHealthProperty = healthType.GetProperty("MaxHealth");
                if (maxHealthProperty == null)
                    maxHealthProperty = healthType.GetProperty("MaxHealthValue");
                if (maxHealthProperty == null)
                    maxHealthProperty = healthType.GetProperty("Max");

                if (maxHealthProperty != null)
                {
                    maxHealth = (float)maxHealthProperty.GetValue(health);
                }

                return Mathf.Clamp01(currentHealth / maxHealth);
            }
            catch
            {
                return 1f;
            }
        }
    }
}
