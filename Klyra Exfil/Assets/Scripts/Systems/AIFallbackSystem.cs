using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Klyra.AI
{
    /// <summary>
    /// Tactical fallback/retreat system for AI.
    /// When overwhelmed but not ready to surrender, AI will:
    /// - Fall back to rally points
    /// - Regroup with allies
    /// - Provide suppressive fire while retreating
    /// - Call for backup
    /// - Counter-attack once regrouped
    /// </summary>
    [RequireComponent(typeof(TacticalAI))]
    [RequireComponent(typeof(AIMoraleSystem))]
    public class AIFallbackSystem : MonoBehaviour
    {
        [Header("Fallback Triggers")]
        [Tooltip("Morale threshold to start fallback (above surrender, below confident)")]
        [Range(20f, 60f)]
        public float fallbackMoraleThreshold = 50f;

        [Tooltip("Morale threshold to stop fallback and re-engage")]
        [Range(40f, 80f)]
        public float reengageMoraleThreshold = 65f;

        [Tooltip("Fall back if taking this many hits in X seconds")]
        public int hitsBeforeFallback = 3;

        [Tooltip("Time window for hit counting")]
        public float hitCountWindow = 5f;

        [Header("Fallback Behavior")]
        [Tooltip("How far to fall back (minimum distance)")]
        public float fallbackDistance = 15f;

        [Tooltip("Maximum distance to search for rally points")]
        public float maxRallyPointSearchDistance = 30f;

        [Tooltip("Prefer rally points with this many allies nearby")]
        public int preferredAllyCount = 2;

        [Tooltip("Provide suppressive fire while retreating?")]
        public bool suppressiveFireWhileFalling = true;

        [Tooltip("Time between suppressive shots while retreating")]
        public float suppressiveFireInterval = 1.5f;

        [Header("Regrouping")]
        [Tooltip("Distance to allies to be considered 'regrouped'")]
        public float regroupDistance = 8f;

        [Tooltip("Morale bonus when successfully regrouped")]
        [Range(0f, 30f)]
        public float regroupMoraleBonus = 20f;

        [Tooltip("Cooldown before allowing another fallback")]
        public float fallbackCooldown = 15f;

        [Header("Voice Lines")]
        [Tooltip("Voice clips to play when falling back")]
        public AudioClip[] fallbackVoiceClips;

        [Tooltip("Voice clips to play when regrouped")]
        public AudioClip[] regroupedVoiceClips;

        [Header("Debug")]
        public bool debugFallback = true;

        // State
        private TacticalAI tacticalAI;
        private AIMoraleSystem moraleSystem;
        private NavMeshAgent navAgent;
        private AudioSource audioSource;

        private bool isFallingBack = false;
        private Vector3 rallyPoint;
        private float lastFallbackTime = -999f;
        private List<float> recentHitTimes = new List<float>();
        private float nextSuppressiveFireTime = 0f;
        private bool hasRegrouped = false;
        private Coroutine fallbackCoroutine;

        // Rally point candidates
        private List<AIMoraleSystem> nearbyAllies = new List<AIMoraleSystem>();
        private static List<AIFallbackSystem> allAIFallback = new List<AIFallbackSystem>();

        void Awake()
        {
            allAIFallback.Add(this);
        }

        void OnDestroy()
        {
            allAIFallback.Remove(this);
        }

        void Start()
        {
            tacticalAI = GetComponent<TacticalAI>();
            moraleSystem = GetComponent<AIMoraleSystem>();
            navAgent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();

            if (tacticalAI == null)
            {
                Debug.LogError($"[AIFallbackSystem] No TacticalAI on {gameObject.name}!");
                enabled = false;
                return;
            }

            if (moraleSystem == null)
            {
                Debug.LogError($"[AIFallbackSystem] No AIMoraleSystem on {gameObject.name}!");
                enabled = false;
                return;
            }

            if (navAgent == null)
            {
                Debug.LogWarning($"[AIFallbackSystem] No NavMeshAgent on {gameObject.name} - fallback may not work properly!");
            }

            // Setup audio source
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 25f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            // Subscribe to damage events
            Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(
                gameObject, "OnHealthDamage", OnTakeDamage);
        }

        void Update()
        {
            if (tacticalAI == null || moraleSystem == null) return;

            // Don't fallback if already surrendered or compliant
            if (tacticalAI.currentState == TacticalAI.AIState.Compliant) return;
            if (tacticalAI.currentState == TacticalAI.AIState.Flashbanged) return;

            // Check if should initiate fallback
            if (!isFallingBack && ShouldFallback())
            {
                InitiateFallback();
            }

            // Check if regrouped and should re-engage
            if (isFallingBack && hasRegrouped && moraleSystem.currentMorale >= reengageMoraleThreshold)
            {
                CompleteRegroup();
            }

            // Suppressive fire while falling back
            if (isFallingBack && suppressiveFireWhileFalling && Time.time >= nextSuppressiveFireTime)
            {
                FireSuppressiveShot();
                nextSuppressiveFireTime = Time.time + suppressiveFireInterval;
            }

            // Clean up old hit times
            recentHitTimes.RemoveAll(t => Time.time - t > hitCountWindow);
        }

        void OnTakeDamage(float damage, Vector3 position, Vector3 force, GameObject attacker, object attackerObject, Collider hitCollider)
        {
            // Track hits for fallback trigger
            recentHitTimes.Add(Time.time);

            if (debugFallback && recentHitTimes.Count >= hitsBeforeFallback)
            {
                Debug.Log($"[{gameObject.name}] Taking heavy fire! {recentHitTimes.Count} hits in {hitCountWindow}s");
            }
        }

        bool ShouldFallback()
        {
            // Cooldown check
            if (Time.time - lastFallbackTime < fallbackCooldown) return false;

            // Don't fallback if morale too low (should surrender instead)
            if (moraleSystem.currentMorale < moraleSystem.surrenderMoraleThreshold) return false;

            // Fallback if morale in "worried" range
            if (moraleSystem.currentMorale <= fallbackMoraleThreshold)
            {
                return true;
            }

            // Fallback if taking heavy fire
            if (recentHitTimes.Count >= hitsBeforeFallback)
            {
                return true;
            }

            return false;
        }

        void InitiateFallback()
        {
            if (debugFallback)
            {
                Debug.Log($"[{gameObject.name}] INITIATING FALLBACK! Morale: {moraleSystem.currentMorale:F1}, Hits: {recentHitTimes.Count}");
            }

            isFallingBack = true;
            lastFallbackTime = Time.time;
            hasRegrouped = false;

            // Play fallback voice line
            PlayFallbackVoiceLine();

            // Find rally point
            rallyPoint = FindBestRallyPoint();

            if (rallyPoint != Vector3.zero)
            {
                // Start fallback behavior
                if (fallbackCoroutine != null)
                {
                    StopCoroutine(fallbackCoroutine);
                }
                fallbackCoroutine = StartCoroutine(FallbackRoutine());
            }
            else
            {
                if (debugFallback)
                {
                    Debug.LogWarning($"[{gameObject.name}] No rally point found! Falling back to basic retreat.");
                }
                // Fallback: just move away from threat
                Vector3 retreatDirection = -transform.forward; // Move backwards
                rallyPoint = transform.position + retreatDirection * fallbackDistance;
                fallbackCoroutine = StartCoroutine(FallbackRoutine());
            }
        }

        IEnumerator FallbackRoutine()
        {
            if (debugFallback)
            {
                Debug.Log($"[{gameObject.name}] Falling back to rally point: {rallyPoint}");
            }

            // Force TacticalAI to move to rally point
            // We'll use reflection to access TacticalAI's movement
            while (isFallingBack && Vector3.Distance(transform.position, rallyPoint) > 2f)
            {
                // Move towards rally point
                if (navAgent != null && navAgent.isActiveAndEnabled)
                {
                    navAgent.SetDestination(rallyPoint);
                }

                // Check if reached or close to allies
                UpdateNearbyAllies();
                if (nearbyAllies.Count >= preferredAllyCount)
                {
                    if (debugFallback)
                    {
                        Debug.Log($"[{gameObject.name}] Regrouped with {nearbyAllies.Count} allies!");
                    }
                    hasRegrouped = true;
                    moraleSystem.currentMorale += regroupMoraleBonus;
                    PlayRegroupedVoiceLine();
                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // Reached rally point
            if (!hasRegrouped)
            {
                UpdateNearbyAllies();
                if (nearbyAllies.Count > 0)
                {
                    if (debugFallback)
                    {
                        Debug.Log($"[{gameObject.name}] Reached rally point with {nearbyAllies.Count} allies nearby!");
                    }
                    hasRegrouped = true;
                    moraleSystem.currentMorale += regroupMoraleBonus;
                    PlayRegroupedVoiceLine();
                }
            }

            // If still no allies, just end fallback after short wait
            if (!hasRegrouped)
            {
                if (debugFallback)
                {
                    Debug.Log($"[{gameObject.name}] Reached rally point but no allies found. Holding position.");
                }
                yield return new WaitForSeconds(3f);
                isFallingBack = false;
            }
        }

        void CompleteRegroup()
        {
            if (debugFallback)
            {
                Debug.Log($"[{gameObject.name}] RE-ENGAGING! Morale restored to {moraleSystem.currentMorale:F1}");
            }

            isFallingBack = false;
            hasRegrouped = false;
            recentHitTimes.Clear();

            // Let TacticalAI resume normal combat behavior
            // It will automatically re-engage based on current state
        }

        Vector3 FindBestRallyPoint()
        {
            // Strategy:
            // 1. Find cover points near allies
            // 2. Prefer positions behind current position (away from threat)
            // 3. Prefer positions with good defensive value

            UpdateNearbyAllies();

            if (nearbyAllies.Count == 0)
            {
                // No allies - just find cover behind us
                return FindCoverBehind();
            }

            // Find ally cluster center
            Vector3 allyCenter = Vector3.zero;
            foreach (var ally in nearbyAllies)
            {
                allyCenter += ally.transform.position;
            }
            allyCenter /= nearbyAllies.Count;

            // Find cover near allies
            CoverPoint[] allCover = CoverPoint.FindAllCoverPoints();
            CoverPoint bestRallyPoint = null;
            float bestScore = float.MinValue;

            // Check if we should defend territory
            bool defendTerritory = tacticalAI != null && tacticalAI.defendTerritory;
            Vector3 territoryCenter = tacticalAI != null ? tacticalAI.transform.position : transform.position;
            float territoryRadius = tacticalAI != null ? tacticalAI.territoryRadius : 0f;

            // Use spawn position from TacticalAI if available
            if (tacticalAI != null)
            {
                var spawnPosField = typeof(TacticalAI).GetField("spawnPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnPosField != null)
                {
                    territoryCenter = (Vector3)spawnPosField.GetValue(tacticalAI);
                }
            }

            foreach (var cover in allCover)
            {
                float distance = Vector3.Distance(transform.position, cover.transform.position);

                // Too close or too far
                if (distance < fallbackDistance || distance > maxRallyPointSearchDistance) continue;

                // Score based on:
                // - Distance to ally cluster (closer = better)
                // - Distance from current position (farther = better, but not too far)
                // - Cover quality
                // - Territory defense (prefer rally points within territory)

                float distanceToAllies = Vector3.Distance(cover.transform.position, allyCenter);
                float score = 0f;

                // Prefer positions near allies
                score += (maxRallyPointSearchDistance - distanceToAllies) * 2f;

                // Prefer positions away from current location (but not too far)
                score += Mathf.Min(distance, fallbackDistance * 2f);

                // Cover quality
                score += cover.coverType == CoverPoint.CoverType.Crouch ? 20f : 10f;

                // Prefer positions behind current position (away from threat)
                Vector3 toCover = (cover.transform.position - transform.position).normalized;
                float dotToBack = Vector3.Dot(-transform.forward, toCover);
                if (dotToBack > 0) // Behind us
                {
                    score += dotToBack * 30f;
                }

                // TERRITORY DEFENSE: Huge bonus for rally points within territory
                if (defendTerritory && territoryRadius > 0f)
                {
                    float distFromTerritory = Vector3.Distance(cover.transform.position, territoryCenter);
                    if (distFromTerritory <= territoryRadius)
                    {
                        // Rally point inside territory - BIG bonus
                        score += 100f;

                        // Even bigger bonus for rally points closer to spawn (defensive positions)
                        float proximityToSpawn = 1f - (distFromTerritory / territoryRadius);
                        score += proximityToSpawn * 50f;
                    }
                    else
                    {
                        // Rally point outside territory - penalty
                        score -= 50f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestRallyPoint = cover;
                }
            }

            if (bestRallyPoint != null)
            {
                if (debugFallback)
                {
                    Debug.Log($"[{gameObject.name}] Found rally point at {bestRallyPoint.name}, score: {bestScore:F1}");
                }
                return bestRallyPoint.transform.position;
            }

            // Fallback: just move towards ally center
            if (debugFallback)
            {
                Debug.Log($"[{gameObject.name}] No cover rally point found, moving towards allies at {allyCenter}");
            }
            return allyCenter;
        }

        Vector3 FindCoverBehind()
        {
            // Find cover behind current position
            Vector3 retreatDirection = -transform.forward;
            Vector3 targetPosition = transform.position + retreatDirection * fallbackDistance;

            // Find nearest cover to that position
            CoverPoint[] allCover = CoverPoint.FindAllCoverPoints();
            CoverPoint nearestCover = null;
            float nearestDistance = float.MaxValue;

            foreach (var cover in allCover)
            {
                float distance = Vector3.Distance(targetPosition, cover.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCover = cover;
                }
            }

            if (nearestCover != null)
            {
                return nearestCover.transform.position;
            }

            // No cover found - just use the target position
            return targetPosition;
        }

        void UpdateNearbyAllies()
        {
            nearbyAllies.Clear();

            foreach (var otherAI in allAIFallback)
            {
                if (otherAI == null || otherAI == this) continue;
                if (otherAI.moraleSystem == null) continue;

                float distance = Vector3.Distance(transform.position, otherAI.transform.position);
                if (distance <= regroupDistance)
                {
                    nearbyAllies.Add(otherAI.moraleSystem);
                }
            }
        }

        void FireSuppressiveShot()
        {
            // Try to fire at current target if available
            // This is a simple implementation - you might want to integrate with your weapon system

            if (debugFallback && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{gameObject.name}] Providing suppressive fire while falling back...");
            }

            // You can integrate with UCC's item system here to actually fire
            // For now, this is a placeholder that you can expand
        }

        void PlayFallbackVoiceLine()
        {
            if (fallbackVoiceClips == null || fallbackVoiceClips.Length == 0) return;
            if (audioSource == null) return;

            AudioClip clip = fallbackVoiceClips[Random.Range(0, fallbackVoiceClips.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.8f);
                if (debugFallback)
                {
                    Debug.Log($"[{gameObject.name}] Playing fallback voice: {clip.name}");
                }
            }
        }

        void PlayRegroupedVoiceLine()
        {
            if (regroupedVoiceClips == null || regroupedVoiceClips.Length == 0) return;
            if (audioSource == null) return;

            AudioClip clip = regroupedVoiceClips[Random.Range(0, regroupedVoiceClips.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.8f);
                if (debugFallback)
                {
                    Debug.Log($"[{gameObject.name}] Playing regroup voice: {clip.name}");
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!debugFallback) return;

            // Draw rally point
            if (isFallingBack && rallyPoint != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(rallyPoint, 1f);
                Gizmos.DrawLine(transform.position, rallyPoint);
            }

            // Draw regroup distance
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, regroupDistance);

            // Draw fallback distance
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, fallbackDistance);
        }

        public bool IsFallingBack()
        {
            return isFallingBack;
        }
    }
}
