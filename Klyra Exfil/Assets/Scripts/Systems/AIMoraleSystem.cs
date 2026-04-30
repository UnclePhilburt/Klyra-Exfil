using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Klyra.AI
{
    /// <summary>
    /// Advanced morale and surrender system for AI.
    /// AI will intelligently decide when to surrender based on:
    /// - Group strength (won't surrender if they outnumber player)
    /// - Witnessing ally deaths
    /// - Being wounded
    /// - Being isolated
    /// - Being flanked/surrounded
    /// - Low ammo
    /// </summary>
    public class AIMoraleSystem : MonoBehaviour
    {
        [Header("Morale Settings")]
        [Tooltip("Starting morale (0-100). Starts at maximum.")]
        [Range(0f, 100f)]
        public float currentMorale = 100f;

        [Tooltip("Morale below this triggers automatic surrender check")]
        [Range(0f, 100f)]
        public float surrenderMoraleThreshold = 30f;

        [Tooltip("Morale below this = instant panic surrender")]
        [Range(0f, 100f)]
        public float panicMoraleThreshold = 10f;

        [Header("Morale Loss Events")]
        [Tooltip("Morale lost when this AI takes damage")]
        [Range(0f, 20f)]
        public float moraleLossOnDamage = 5f;

        [Tooltip("Morale lost when witnessing an ally die")]
        [Range(0f, 30f)]
        public float moraleLossOnAllyDeath = 15f;

        [Tooltip("Morale lost per second when isolated (no allies nearby)")]
        [Range(0f, 5f)]
        public float moraleLossWhenIsolated = 2f;

        [Tooltip("Morale lost when flanked by player")]
        [Range(0f, 20f)]
        public float moraleLossWhenFlanked = 10f;

        [Header("Group Awareness")]
        [Tooltip("Range to detect nearby allies")]
        public float allyDetectionRange = 20f;

        [Tooltip("Range to detect nearby enemy players")]
        public float enemyDetectionRange = 30f;

        [Tooltip("Won't surrender if allies outnumber enemies by this ratio")]
        public float minimumNumbersRatio = 1.5f; // 3 allies vs 2 players = won't surrender

        [Tooltip("Morale bonus per nearby ally (up to max)")]
        [Range(0f, 20f)]
        public float moralePerAlly = 10f;

        [Tooltip("Maximum morale bonus from allies")]
        [Range(0f, 50f)]
        public float maxAllyMoraleBonus = 40f;

        [Tooltip("Morale penalty per nearby enemy player")]
        [Range(0f, 15f)]
        public float moralePenaltyPerNearbyEnemy = 8f;

        [Header("Health Influence")]
        [Tooltip("How much health affects morale (0 = no effect, 1 = full effect)")]
        [Range(0f, 1f)]
        public float healthMoraleInfluence = 0.5f;

        [Header("Surrender Behavior")]
        [Tooltip("Check for surrender conditions every X seconds")]
        public float surrenderCheckInterval = 2f;

        [Tooltip("Cooldown after refusing to surrender (won't check again for X seconds)")]
        public float surrenderRefusalCooldown = 10f;

        [Header("Debug")]
        public bool debugMorale = true;

        // State
        private TacticalAI tacticalAI;
        private AISurrenderAnimation surrenderAnimation;
        private Opsive.UltimateCharacterController.Traits.Health health;
        private float nextSurrenderCheckTime = 0f;
        private float lastSurrenderRefusalTime = -999f;
        private bool hasSurrendered = false;
        private List<AIMoraleSystem> nearbyAllies = new List<AIMoraleSystem>();
        private List<GameObject> nearbyEnemies = new List<GameObject>();
        private float lastIsolationCheckTime = 0f;
        private bool wasIsolatedLastCheck = false;
        private Vector3 lastPlayerPosition;
        private int lastNearbyEnemyCount = 0;

        // Static tracking for all AI
        private static List<AIMoraleSystem> allAI = new List<AIMoraleSystem>();
        private static List<GameObject> allPlayers = new List<GameObject>();

        void Awake()
        {
            allAI.Add(this);
        }

        void OnDestroy()
        {
            allAI.Remove(this);
        }

        void Start()
        {
            tacticalAI = GetComponent<TacticalAI>();
            surrenderAnimation = GetComponent<AISurrenderAnimation>();
            health = GetComponent<Opsive.UltimateCharacterController.Traits.Health>();

            if (tacticalAI == null)
            {
                Debug.LogError($"[AIMoraleSystem] No TacticalAI on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Subscribe to damage events
            Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(
                gameObject, "OnHealthDamage", OnTakeDamage);

            // Subscribe to death events of other AI
            foreach (var otherAI in allAI)
            {
                if (otherAI != this)
                {
                    Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(
                        otherAI.gameObject, "OnDeath", OnAllyDeath);
                }
            }

            UpdatePlayerList();
        }

        void Update()
        {
            if (hasSurrendered) return;
            if (tacticalAI.currentState == TacticalAI.AIState.Compliant) return;
            if (tacticalAI.currentState == TacticalAI.AIState.Flashbanged) return;

            // Update nearby allies and enemies
            UpdateNearbyAllies();
            UpdateNearbyEnemies();

            // Lose morale when isolated
            if (IsIsolated())
            {
                currentMorale -= moraleLossWhenIsolated * Time.deltaTime;
                if (!wasIsolatedLastCheck && debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] Isolated! Losing morale ({currentMorale:F1}/100)");
                }
                wasIsolatedLastCheck = true;
            }
            else
            {
                // Gain morale from nearby allies
                float allyBonus = Mathf.Min(nearbyAllies.Count * moralePerAlly, maxAllyMoraleBonus);
                currentMorale = Mathf.Min(100f, currentMorale + allyBonus * Time.deltaTime * 0.1f);
                wasIsolatedLastCheck = false;
            }

            // Lose morale from nearby enemies (outnumbered/surrounded)
            if (nearbyEnemies.Count > 0)
            {
                float enemyPenalty = nearbyEnemies.Count * moralePenaltyPerNearbyEnemy * Time.deltaTime;
                currentMorale -= enemyPenalty;

                // Log when multiple enemies get close
                if (nearbyEnemies.Count != lastNearbyEnemyCount && debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] {nearbyEnemies.Count} enemies nearby! Losing morale ({currentMorale:F1}/100)");
                }
                lastNearbyEnemyCount = nearbyEnemies.Count;
            }
            else
            {
                lastNearbyEnemyCount = 0;
            }

            // Apply health influence
            if (health != null && health.IsAlive())
            {
                // Try to get health percentage using reflection to avoid property name issues
                float healthPercent = GetHealthPercentage(health);
                float healthMoralePenalty = (1f - healthPercent) * healthMoraleInfluence * 50f;
                currentMorale = Mathf.Max(0f, currentMorale - healthMoralePenalty * Time.deltaTime);
            }

            // Clamp morale
            currentMorale = Mathf.Clamp(currentMorale, 0f, 100f);

            // Check for automatic surrender
            if (Time.time >= nextSurrenderCheckTime)
            {
                CheckSurrenderConditions();
                nextSurrenderCheckTime = Time.time + surrenderCheckInterval;
            }
        }

        void OnTakeDamage(float damage, Vector3 position, Vector3 force, GameObject attacker, object attackerObject, Collider hitCollider)
        {
            if (hasSurrendered) return;

            // Lose morale from damage
            currentMorale -= moraleLossOnDamage;

            if (debugMorale)
            {
                Debug.Log($"[{gameObject.name}] Took damage! Morale: {currentMorale:F1}/100");
            }

            // Check if being flanked
            if (attacker != null)
            {
                lastPlayerPosition = attacker.transform.position;
                if (IsFlanked(attacker.transform.position))
                {
                    currentMorale -= moraleLossWhenFlanked;
                    if (debugMorale)
                    {
                        Debug.Log($"[{gameObject.name}] FLANKED! Major morale loss! ({currentMorale:F1}/100)");
                    }
                }
            }
        }

        void OnAllyDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            if (hasSurrendered) return;

            // Check if we can see the death (within sight range)
            float distance = Vector3.Distance(transform.position, position);
            if (distance <= tacticalAI.sightRange)
            {
                currentMorale -= moraleLossOnAllyDeath;

                if (debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] Witnessed ally death! Morale: {currentMorale:F1}/100");
                }

                // Immediate surrender check if morale is critical
                if (currentMorale <= panicMoraleThreshold)
                {
                    TryPanicSurrender();
                }
            }
        }

        void CheckSurrenderConditions()
        {
            // Don't check too soon after refusing
            if (Time.time - lastSurrenderRefusalTime < surrenderRefusalCooldown) return;

            // Panic surrender - instant
            if (currentMorale <= panicMoraleThreshold)
            {
                TryPanicSurrender();
                return;
            }

            // Normal surrender threshold
            if (currentMorale <= surrenderMoraleThreshold)
            {
                TryConditionalSurrender();
            }
        }

        void TryPanicSurrender()
        {
            if (debugMorale)
            {
                Debug.Log($"[{gameObject.name}] PANIC! Morale collapsed! ({currentMorale:F1}/100) - Surrendering!");
            }

            ForceSurrender();
        }

        void TryConditionalSurrender()
        {
            // Check group strength - use NEARBY enemies for immediate threat assessment
            UpdatePlayerList();
            int allyCount = GetAliveAlliesCount();
            int totalEnemyCount = GetAliveEnemiesCount(); // Total enemies in level
            int nearbyEnemyCount = nearbyEnemies.Count; // Enemies right here

            if (debugMorale)
            {
                Debug.Log($"[{gameObject.name}] Considering surrender... Nearby: {allyCount} allies vs {nearbyEnemyCount} enemies | Total in level: {totalEnemyCount} enemies | Morale: {currentMorale:F1}/100");
            }

            // SURROUNDED - multiple enemies nearby
            if (nearbyEnemyCount >= 2 && nearbyAllies.Count == 0)
            {
                if (debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] SURROUNDED by {nearbyEnemyCount} enemies with no backup - surrendering!");
                }
                ForceSurrender();
                return;
            }

            // Don't surrender if we outnumber them significantly (locally)
            if (nearbyEnemyCount > 0) // If enemies are nearby, use local count
            {
                int localAllyCount = nearbyAllies.Count + 1; // +1 for self
                if (localAllyCount >= nearbyEnemyCount * minimumNumbersRatio)
                {
                    if (debugMorale)
                    {
                        Debug.Log($"[{gameObject.name}] Refusing to surrender - we outnumber them locally! ({localAllyCount} vs {nearbyEnemyCount})");
                    }
                    lastSurrenderRefusalTime = Time.time;
                    return;
                }
            }
            else // No enemies nearby, use total count
            {
                if (allyCount >= totalEnemyCount * minimumNumbersRatio)
                {
                    if (debugMorale)
                    {
                        Debug.Log($"[{gameObject.name}] Refusing to surrender - we outnumber them overall! ({allyCount} vs {totalEnemyCount})");
                    }
                    lastSurrenderRefusalTime = Time.time;
                    return;
                }
            }

            // Check if isolated
            if (IsIsolated())
            {
                if (debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] Isolated and demoralized - surrendering!");
                }
                ForceSurrender();
                return;
            }

            // Check health
            if (health != null && health.IsAlive())
            {
                float healthPercent = GetHealthPercentage(health);
                if (healthPercent < 0.3f) // Below 30% health
                {
                    if (debugMorale)
                    {
                        Debug.Log($"[{gameObject.name}] Badly wounded ({healthPercent * 100f:F0}% HP) and demoralized - surrendering!");
                    }
                    ForceSurrender();
                    return;
                }
            }

            // Outnumbered surrender (use nearby count if available, otherwise total)
            int relevantEnemyCount = nearbyEnemyCount > 0 ? nearbyEnemyCount : totalEnemyCount;
            int relevantAllyCount = nearbyEnemyCount > 0 ? (nearbyAllies.Count + 1) : allyCount;

            if (relevantAllyCount < relevantEnemyCount)
            {
                if (debugMorale)
                {
                    Debug.Log($"[{gameObject.name}] Outnumbered and demoralized - surrendering! ({relevantAllyCount} vs {relevantEnemyCount})");
                }
                ForceSurrender();
                return;
            }

            // Didn't meet conditions
            if (debugMorale)
            {
                Debug.Log($"[{gameObject.name}] Low morale but refusing to surrender (still have hope)");
            }
            lastSurrenderRefusalTime = Time.time;
        }

        void ForceSurrender()
        {
            hasSurrendered = true;

            // Trigger TacticalAI compliance
            if (tacticalAI != null)
            {
                StartCoroutine(tacticalAI.ComplyWithCommand());
            }

            if (debugMorale)
            {
                Debug.Log($"[{gameObject.name}] *** SURRENDERED *** Final morale: {currentMorale:F1}/100");
            }
        }

        bool IsIsolated()
        {
            return nearbyAllies.Count == 0;
        }

        bool IsFlanked(Vector3 attackerPosition)
        {
            // Check if attacker is behind us (more than 120 degrees from forward)
            Vector3 directionToAttacker = (attackerPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToAttacker);
            return angle > 120f;
        }

        void UpdateNearbyAllies()
        {
            nearbyAllies.Clear();

            foreach (var otherAI in allAI)
            {
                if (otherAI == this) continue;
                if (otherAI == null) continue;
                if (!otherAI.enabled) continue;

                // Check if alive
                var otherHealth = otherAI.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
                if (otherHealth != null && !otherHealth.IsAlive()) continue;

                // Check distance
                float distance = Vector3.Distance(transform.position, otherAI.transform.position);
                if (distance <= allyDetectionRange)
                {
                    nearbyAllies.Add(otherAI);
                }
            }
        }

        void UpdateNearbyEnemies()
        {
            nearbyEnemies.Clear();
            UpdatePlayerList();

            foreach (var player in allPlayers)
            {
                if (player == null) continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= enemyDetectionRange)
                {
                    nearbyEnemies.Add(player);
                }
            }
        }

        int GetAliveAlliesCount()
        {
            int count = 1; // Count self

            foreach (var ai in allAI)
            {
                if (ai == this) continue;
                if (ai == null) continue;

                var aiHealth = ai.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
                if (aiHealth != null && aiHealth.IsAlive())
                {
                    count++;
                }
            }

            return count;
        }

        int GetAliveEnemiesCount()
        {
            UpdatePlayerList();
            return allPlayers.Count;
        }

        void UpdatePlayerList()
        {
            allPlayers.Clear();
            var players = PlayerTarget.All;

            foreach (var player in players)
            {
                if (player != null && player.gameObject != null)
                {
                    // Check if alive
                    var playerHealth = player.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
                    if (playerHealth == null || playerHealth.IsAlive())
                    {
                        allPlayers.Add(player.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to get health percentage using reflection (UCC version-safe)
        /// </summary>
        float GetHealthPercentage(Opsive.UltimateCharacterController.Traits.Health healthComponent)
        {
            if (healthComponent == null) return 1f;

            try
            {
                // UCC Health uses a "Value" property for current health and other properties for max
                // We'll use reflection to be version-safe
                var healthType = healthComponent.GetType();

                // Try to get current health value
                float currentHealth = 0f;
                var valueProperty = healthType.GetProperty("Value");
                if (valueProperty != null)
                {
                    currentHealth = (float)valueProperty.GetValue(healthComponent);
                }

                // Try to get max health - try multiple property names
                float maxHealth = 100f;
                var maxHealthProperty = healthType.GetProperty("MaxHealth");
                if (maxHealthProperty == null)
                    maxHealthProperty = healthType.GetProperty("MaxHealthValue");
                if (maxHealthProperty == null)
                    maxHealthProperty = healthType.GetProperty("Max");

                if (maxHealthProperty != null)
                {
                    maxHealth = (float)maxHealthProperty.GetValue(healthComponent);
                }

                // Return percentage
                return Mathf.Clamp01(currentHealth / maxHealth);
            }
            catch
            {
                // Fallback - assume healthy
                return 1f;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!debugMorale) return;

            // Draw morale bar above AI
            Vector3 barPosition = transform.position + Vector3.up * 2.5f;
            float barWidth = 1f;
            float barHeight = 0.1f;

            // Background (red)
            Gizmos.color = Color.red;
            Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.01f));

            // Foreground (green/yellow based on morale)
            float moralePercent = currentMorale / 100f;
            Gizmos.color = moralePercent > 0.5f ? Color.green : Color.yellow;
            Vector3 moraleBarPosition = barPosition - Vector3.right * (barWidth / 2f * (1f - moralePercent));
            Gizmos.DrawCube(moraleBarPosition, new Vector3(barWidth * moralePercent, barHeight, 0.02f));

            // Draw ally detection range (green)
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, allyDetectionRange);

            // Draw enemy detection range (red)
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, enemyDetectionRange);

            // Draw lines to nearby allies (cyan)
            Gizmos.color = Color.cyan;
            foreach (var ally in nearbyAllies)
            {
                if (ally != null)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up, ally.transform.position + Vector3.up);
                }
            }

            // Draw lines to nearby enemies (red)
            Gizmos.color = Color.red;
            foreach (var enemy in nearbyEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, enemy.transform.position + Vector3.up * 1.5f);
                }
            }
        }
    }
}
