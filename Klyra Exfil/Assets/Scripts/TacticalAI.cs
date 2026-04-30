using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AI enemy for tactical shooter gameplay like Ready or Not.
/// Features patrol, investigation, combat, voice line responses, and flashbang reactions.
/// Works with Opsive Ultimate Character Controller and NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TacticalAI : MonoBehaviourPun
{
    [Header("AI State")]
    public AIState currentState = AIState.Patrol;
    public float alertLevel = 0f; // 0 = calm, 1 = fully alert

    [Header("Movement Settings")]
    [Tooltip("Movement mode: Patrol (waypoints), Roam (random), or Idle (stationary)")]
    public MovementMode movementMode = MovementMode.Idle;

    [Header("Patrol Settings (Only if Movement Mode = Patrol)")]
    [Tooltip("Patrol waypoints - will cycle through these")]
    public Transform[] patrolWaypoints;
    [Tooltip("Wait time at each waypoint")]
    public float waypointWaitTime = 3f;

    [Header("Roaming Settings (Only if Movement Mode = Roam)")]
    [Tooltip("How far from spawn point AI can roam")]
    public float roamRadius = 20f;
    [Tooltip("Time to wait before picking new roam point (higher = less movement, more Ready or Not style)")]
    public float roamWaitTime = 15f;
    [Tooltip("Minimum distance for new roam point")]
    public float minRoamDistance = 5f;
    [Tooltip("Max attempts to find valid roam point")]
    public int maxRoamAttempts = 10;
    [Tooltip("Chance to move to new spot (0-1). Lower = more stationary")]
    [Range(0f, 1f)]
    public float roamChance = 0.3f;

    [Header("Movement Speed")]
    [Tooltip("Speed while patrolling/roaming")]
    public float patrolSpeed = 1.5f;

    [Header("Detection Settings")]
    [Tooltip("How far AI can see")]
    public float sightRange = 20f;
    [Tooltip("Field of view angle")]
    public float fieldOfView = 90f;
    [Tooltip("How far AI can hear")]
    public float hearingRange = 15f;
    [Tooltip("Layer mask for line of sight checks")]
    public LayerMask obstacleMask;
    [Tooltip("How fast alert level increases when seeing player")]
    public float alertIncreaseRate = 2f;
    [Tooltip("How fast alert level decreases when not seeing player")]
    public float alertDecreaseRate = 0.5f;
    [Tooltip("Check for threats every X seconds (lower = more responsive)")]
    public float threatCheckInterval = 0.2f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Distance to check for obstacles in front")]
    public float obstacleDetectionDistance = 2f;
    [Tooltip("Number of raycasts to use for obstacle detection (more = better avoidance)")]
    public int obstacleRayCount = 5;
    [Tooltip("Width of obstacle detection spread")]
    public float obstacleDetectionWidth = 1f;
    [Tooltip("How often to check for obstacles (seconds)")]
    public float obstacleCheckInterval = 0.3f;
    [Tooltip("Layer mask for physical obstacles (furniture, walls, etc.) - leave as 'Everything' to detect all objects")]
    public LayerMask physicalObstacleMask = -1;

    [Header("Combat Settings")]
    [Tooltip("Combat movement speed")]
    public float combatSpeed = 3f;
    [Tooltip("Preferred combat distance")]
    public float combatDistance = 10f;
    [Tooltip("Time between shots")]
    public float fireRate = 0.5f;
    [Tooltip("Accuracy (0-1)")]
    [Range(0f, 1f)]
    public float accuracy = 0.7f;
    [Tooltip("Use cover when under fire")]
    public bool useCover = true;
    [Tooltip("How far to search for cover")]
    public float coverSearchRange = 15f;
    [Tooltip("Minimum cover height")]
    public float minCoverHeight = 0.8f;

    [Header("Territory Defense")]
    [Tooltip("Defend territory instead of aggressively pursuing players")]
    public bool defendTerritory = true;
    [Tooltip("Max distance from spawn to chase enemies (territory radius)")]
    public float territoryRadius = 25f;
    [Tooltip("How far outside territory before returning (gives some chase distance)")]
    public float maxChaseDistance = 35f;
    [Tooltip("When outside territory, prioritize returning to territory over chasing")]
    public bool returnWhenOutsideTerritory = true;

    [Header("Voice Line Response")]
    [Tooltip("Chance to comply with voice commands (0-1)")]
    [Range(0f, 1f)]
    public float complianceChance = 0.3f;
    [Tooltip("Time AI stays compliant")]
    public float complianceDuration = 5f;
    [Tooltip("Range to hear voice commands")]
    public float voiceCommandRange = 10f;

    [Header("Flashbang Response")]
    [Tooltip("Duration of flashbang stun")]
    public float flashbangStunDuration = 5f;
    [Tooltip("Mixamo / humanoid clip to play while stunned. Leave empty to use the wobble-only fallback.")]
    public AnimationClip flashbangStunClip;
    [Tooltip("How violently the AI wobbles their aim while stunned. Ignored when a stun clip is assigned.")]
    public float flashbangWobbleSpeed = 180f;
    [Tooltip("How far the AI's aim can swing off-center while stunned. Ignored when a stun clip is assigned.")]
    public float flashbangWobbleAmplitude = 90f;

    [Header("References")]
    public Transform eyePosition; // For line of sight checks

    [Header("Room Awareness")]
    [Tooltip("Enable room tracking (requires RoomVolume components)")]
    public bool enableRoomAwareness = true;

    [Header("Debug")]
    [Tooltip("Log a detection summary once per second.")]
    public bool debugDetection = false;
    [Tooltip("Show AI vision cone and detection rays in scene view")]
    public bool debugVision = true;
    [Tooltip("Debug room awareness system")]
    public bool debugRooms = false;
    private float debugLogTimer = 0f;

    // Private state
    private NavMeshAgent navAgent;
    private Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion characterLocomotion;
    private Opsive.UltimateCharacterController.Character.Abilities.Items.Use useAbility;
    private Opsive.UltimateCharacterController.Character.Abilities.Items.Aim aimAbility;
    private Opsive.UltimateCharacterController.Character.Abilities.Items.Reload reloadAbility;
    private Opsive.UltimateCharacterController.Character.Abilities.AI.PathfindingMovement pathfindingMovement;
    private int dryFireCount = 0;

    private Animator animator;
    private PlayableGraph stunGraph;
    private AnimationClipPlayable stunClipPlayable;
    private Transform currentTarget;
    private int currentWaypointIndex = 0;
    private float waypointTimer = 0f;
    private float fireTimer = 0f;
    private bool isCompliant = false;
    private bool isFlashbanged = false;
    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPosition = false;
    private CoverPoint currentCoverPoint = null;
    private bool hasValidCover = false;
    private float lastDamageTime = 0f;
    private Vector3 spawnPosition;
    private Vector3 currentRoamTarget;
    private bool hasRoamTarget = false;
    private float lastThreatCheckTime = 0f;
    private float lastObstacleCheckTime = 0f;
    private bool obstacleAhead = false;
    private int consecutiveObstacleHits = 0;
    private AdvancedAICombatTactics advancedTactics;

    // Room awareness
    private Klyra.AI.RoomVolume currentRoom;
    private Klyra.AI.RoomVolume lastKnownPlayerRoom;

    public enum AIState
    {
        Patrol,
        Investigate,
        Combat,
        Compliant,
        Flashbanged
    }

    public enum MovementMode
    {
        Patrol,
        Roam,
        Idle
    }

    void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        characterLocomotion = GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        animator = GetComponentInChildren<Animator>();

        if (characterLocomotion != null)
        {
            useAbility = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Items.Use>();
            aimAbility = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Items.Aim>();
            reloadAbility = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Items.Reload>();
            pathfindingMovement = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.AI.PathfindingMovement>();
        }

        // Setup eye position if not set
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 1.7f, 0);
            eyePosition = eyeObj.transform;
        }

        // Configure NavMesh agent
        if (navAgent != null)
        {
            navAgent.speed = patrolSpeed;
            navAgent.stoppingDistance = 1f;
        }

        // Save spawn position for roaming
        spawnPosition = transform.position;

        // Start movement based on mode
        if (movementMode == MovementMode.Patrol)
        {
            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                SetDestination(patrolWaypoints[0].position);
            }
        }
        else if (movementMode == MovementMode.Roam)
        {
            PickNewRoamTarget();
        }
        // Idle mode - don't move at start

        // Subscribe to death and damage events
        Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnAIDeath);
        Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(gameObject, "OnHealthDamage", OnTakeDamage);

        // Check for advanced tactics component
        advancedTactics = GetComponent<AdvancedAICombatTactics>();

        Debug.Log($"TacticalAI initialized on {gameObject.name}");
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        Opsive.Shared.Events.EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnAIDeath);
        Opsive.Shared.Events.EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, object, Collider>(gameObject, "OnHealthDamage", OnTakeDamage);
        if (stunGraph.IsValid()) stunGraph.Destroy();
    }

    void OnAIDeath(Vector3 position, Vector3 force, GameObject attacker)
    {
        Debug.Log($"{gameObject.name} died");

        // Release cover if using one
        ReleaseCover();

        // Disable AI
        this.enabled = false;

        // Disable NavMesh agent
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
    }

    /// <summary>
    /// Called when AI takes damage - makes them immediately engage
    /// </summary>
    void OnTakeDamage(float damage, Vector3 position, Vector3 force, GameObject attacker, object attackerObject, Collider hitCollider)
    {
        // If we're already dead, flashbanged, or compliant, ignore
        if (!this.enabled || currentState == AIState.Flashbanged || currentState == AIState.Compliant)
            return;

        Debug.Log($"{gameObject.name}: TOOK DAMAGE! {damage} from {(attacker != null ? attacker.name : "unknown")}");

        // Record damage time for cover seeking
        lastDamageTime = Time.time;
        hasValidCover = false; // Reset cover so we find new cover

        // Notify advanced tactics that we're taking fire
        if (advancedTactics != null)
        {
            advancedTactics.OnTakingFire();
        }

        // IMMEDIATELY enter combat mode when shot
        if (attacker != null)
        {
            // Find the attacker's aim point for targeting
            var playerTarget = attacker.GetComponent<PlayerTarget>();
            if (playerTarget != null && playerTarget.AimPoint != null)
            {
                currentTarget = playerTarget.AimPoint;
                lastKnownPlayerPosition = currentTarget.position;
                hasLastKnownPosition = true;
            }
            else
            {
                // Fallback - just target the attacker's transform
                currentTarget = attacker.transform;
                lastKnownPlayerPosition = attacker.transform.position;
                hasLastKnownPosition = true;
            }

            Debug.Log($"{gameObject.name}: Engaging attacker at {lastKnownPlayerPosition}!");

            // INSTANTLY turn to face the attacker
            Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
            directionToAttacker.y = 0; // Keep on horizontal plane
            if (directionToAttacker != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToAttacker);
                Debug.Log($"{gameObject.name}: INSTANTLY turned to face attacker!");
            }

            // ALERT TEAMMATES - we're under attack!
            AlertNearbyAllies(attacker.transform.position);

            TransitionToState(AIState.Combat);
        }
        else
        {
            // Took damage but don't know from where - investigate the damage position
            lastKnownPlayerPosition = position;
            hasLastKnownPosition = true;

            if (currentState != AIState.Combat)
            {
                Debug.Log($"{gameObject.name}: Investigating damage source at {position}");
                TransitionToState(AIState.Investigate);
            }
        }

        // Broadcast to nearby AI that we're under attack
        BroadcastAlert(position, attacker);
    }

    /// <summary>
    /// Alert nearby AI when this AI is attacked
    /// </summary>
    void BroadcastAlert(Vector3 threatPosition, GameObject attacker)
    {
        // Find all other AI within hearing range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, hearingRange);

        foreach (var col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue; // Skip self

            TacticalAI nearbyAI = col.GetComponent<TacticalAI>();
            if (nearbyAI != null && nearbyAI.enabled)
            {
                nearbyAI.OnAllyUnderAttack(threatPosition, attacker);
            }
        }
    }

    /// <summary>
    /// Called when a nearby ally is under attack
    /// </summary>
    public void OnAllyUnderAttack(Vector3 threatPosition, GameObject attacker)
    {
        // If already in combat, ignore
        if (currentState == AIState.Combat || currentState == AIState.Flashbanged || currentState == AIState.Compliant)
            return;

        Debug.Log($"{gameObject.name}: Ally under attack! Investigating {threatPosition}");

        lastKnownPlayerPosition = threatPosition;
        hasLastKnownPosition = true;

        // If we can see the attacker, engage immediately
        if (attacker != null)
        {
            var playerTarget = attacker.GetComponent<PlayerTarget>();
            Transform targetTransform = playerTarget != null && playerTarget.AimPoint != null
                ? playerTarget.AimPoint
                : attacker.transform;

            if (CanSeeTarget(targetTransform))
            {
                currentTarget = targetTransform;
                TransitionToState(AIState.Combat);
                Debug.Log($"{gameObject.name}: Can see attacker! Engaging!");
                return;
            }
        }

        // Can't see attacker, go investigate
        TransitionToState(AIState.Investigate);
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return; // Only control on owner's client

        // ALWAYS check for threats FIRST (unless flashbanged or compliant)
        // This ensures player detection happens before movement decisions
        if (currentState != AIState.Flashbanged && currentState != AIState.Compliant)
        {
            // Check threats at regular intervals for better performance
            if (Time.time - lastThreatCheckTime >= threatCheckInterval)
            {
                CheckForThreats();
                lastThreatCheckTime = Time.time;
            }
        }

        // Update based on state
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Investigate:
                UpdateInvestigate();
                break;
            case AIState.Combat:
                UpdateCombat();
                break;
            case AIState.Compliant:
                UpdateCompliant();
                break;
            case AIState.Flashbanged:
                UpdateFlashbanged();
                break;
        }

        // Update alert level
        UpdateAlertLevel();

        // Update eye position to follow character's actual facing direction
        UpdateEyeDirection();

        // Check for obstacles while moving (only during patrol/roam)
        if (currentState == AIState.Patrol && navAgent != null && navAgent.velocity.magnitude > 0.1f)
        {
            if (Time.time - lastObstacleCheckTime >= obstacleCheckInterval)
            {
                CheckForObstacles();
                lastObstacleCheckTime = Time.time;
            }
        }
    }

    void UpdateEyeDirection()
    {
        // Make sure eye position rotates with the character
        if (eyePosition != null && eyePosition.parent == transform)
        {
            // Eye should look in the same direction as the character
            eyePosition.rotation = Quaternion.LookRotation(transform.forward);
        }

        // Draw vision ray in game view for debugging
        if (debugVision && eyePosition != null)
        {
            Debug.DrawRay(eyePosition.position, eyePosition.forward * sightRange, Color.yellow, 0.1f);
        }
    }

    #region State Updates

    void UpdatePatrol()
    {
        navAgent.speed = patrolSpeed;

        // Use different behavior based on movement mode
        if (movementMode == MovementMode.Patrol)
        {
            UpdatePatrolWaypoints();
        }
        else if (movementMode == MovementMode.Roam)
        {
            UpdateRoaming();
        }
        else if (movementMode == MovementMode.Idle)
        {
            UpdateIdle();
        }
    }

    void UpdatePatrolWaypoints()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        // Check if reached waypoint
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            waypointTimer += Time.deltaTime;

            if (waypointTimer >= waypointWaitTime)
            {
                // Move to next waypoint
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                SetDestination(patrolWaypoints[currentWaypointIndex].position);
                waypointTimer = 0f;
            }
        }
    }

    void UpdateRoaming()
    {
        // If obstacle detected multiple times, pick new target
        if (obstacleAhead && consecutiveObstacleHits > 3)
        {
            Debug.LogWarning($"{gameObject.name}: Obstacle blocking path, picking new roam target");
            PickNewRoamTarget();
            consecutiveObstacleHits = 0;
            obstacleAhead = false;
            return;
        }

        // If NavMesh agent has no path or path is invalid, get a new target
        if (navAgent != null && (!navAgent.hasPath || navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid))
        {
            if (hasRoamTarget)
            {
                Debug.LogWarning($"{gameObject.name}: Invalid path to roam target, picking new one");
                hasRoamTarget = false;
            }
        }

        // Check if reached roam target
        if (hasRoamTarget && !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            waypointTimer += Time.deltaTime;

            if (waypointTimer >= roamWaitTime)
            {
                // Random chance to move (Ready or Not style - enemies often stay put)
                if (Random.value < roamChance)
                {
                    // Pick new random roam target
                    PickNewRoamTarget();
                    waypointTimer = 0f;
                    consecutiveObstacleHits = 0;
                }
                else
                {
                    // Stay at current position
                    waypointTimer = 0f;
                    Debug.Log($"{gameObject.name}: Chose to stay at current position");
                }
            }
        }
        else if (!hasRoamTarget)
        {
            // No target, pick one
            PickNewRoamTarget();
        }

        // Check if stuck (not moving for a while with a target)
        if (hasRoamTarget && navAgent != null && navAgent.velocity.magnitude < 0.1f && !navAgent.pathPending)
        {
            waypointTimer += Time.deltaTime;
            if (waypointTimer >= 2f) // Stuck for 2 seconds
            {
                Debug.LogWarning($"{gameObject.name}: Seems stuck, picking new roam target");
                PickNewRoamTarget();
                waypointTimer = 0f;
                consecutiveObstacleHits = 0;
            }
        }
        else
        {
            // Reset timer if moving
            if (navAgent != null && navAgent.velocity.magnitude > 0.1f)
            {
                waypointTimer = 0f;
            }
        }
    }

    void PickNewRoamTarget()
    {
        UnityEngine.AI.NavMeshHit hit;
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

        // Try multiple times to find a valid roam point
        for (int attempt = 0; attempt < maxRoamAttempts; attempt++)
        {
            // Pick random point within roam radius from spawn position
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 randomDirection = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Make sure it's on the NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Vector3 potentialTarget = hit.position;

                // Check distance requirements
                float distance = Vector3.Distance(transform.position, potentialTarget);
                if (distance < minRoamDistance)
                {
                    continue; // Too close, try again
                }

                // Check if there's a direct obstacle in the way
                Vector3 directionToTarget = (potentialTarget - transform.position).normalized;
                RaycastHit obstacleHit;
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, out obstacleHit, distance, obstacleMask))
                {
                    // There's an obstacle in the direct line - try to path around it via NavMesh
                    // This is fine as long as NavMesh can path around it
                }

                // IMPORTANT: Validate that we can actually path to this location
                if (navAgent != null && navAgent.isOnNavMesh)
                {
                    if (navAgent.CalculatePath(potentialTarget, path))
                    {
                        // Check if the path is complete (not partial)
                        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                        {
                            // Check path length isn't too crazy (no super long detours)
                            float pathLength = GetPathLength(path);
                            float straightLineDistance = distance;

                            // If path is more than 2x the straight line distance, it's too convoluted
                            if (pathLength < straightLineDistance * 2.5f)
                            {
                                // Valid path found!
                                currentRoamTarget = potentialTarget;
                                hasRoamTarget = true;
                                SetDestination(currentRoamTarget);

                                Debug.Log($"{gameObject.name}: Roaming to new position {distance:F1}m away, path length {pathLength:F1}m (attempt {attempt + 1})");
                                return;
                            }
                        }
                    }
                }
            }
        }

        // Failed to find valid roam point after all attempts
        Debug.LogWarning($"{gameObject.name}: Failed to find valid roam target after {maxRoamAttempts} attempts. Staying put.");
        hasRoamTarget = false;

        // Stay at current position
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
        }
    }

    float GetPathLength(UnityEngine.AI.NavMeshPath path)
    {
        float length = 0f;
        if (path.corners.Length < 2) return 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }

    void UpdateIdle()
    {
        // Ready or Not style - AI just stands still and watches
        // Stop any movement
        if (navAgent != null && navAgent.hasPath)
        {
            navAgent.ResetPath();
        }

        // Slowly look around (scanning for threats)
        waypointTimer += Time.deltaTime;
        if (waypointTimer >= Random.Range(4f, 8f)) // Random interval for realism
        {
            // Rotate slightly to look around
            float randomYaw = Random.Range(-45f, 45f);
            Vector3 newForward = Quaternion.Euler(0, randomYaw, 0) * transform.forward;

            if (characterLocomotion != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(newForward);
                characterLocomotion.SetRotation(targetRot, false);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(newForward);
            }

            waypointTimer = 0f;
        }
    }

    void UpdateInvestigate()
    {
        navAgent.speed = combatSpeed * 0.7f; // Move slower when investigating (more tactical)

        // If we have cover, move to it first, then investigate from there
        if (hasValidCover && currentCoverPoint != null)
        {
            Vector3 coverPos = currentCoverPoint.GetCoverPosition();
            float distanceToCover = Vector3.Distance(transform.position, coverPos);

            if (distanceToCover > 1f)
            {
                // Still moving to cover
                SetDestination(coverPos);
                return;
            }
            else
            {
                // At cover, now investigate from here
                SetDestination(transform.position); // Hold position

                // Look toward threat area
                if (hasLastKnownPosition)
                {
                    LookAtTarget(lastKnownPlayerPosition);
                }

                // After some time at cover, return to patrol
                waypointTimer += Time.deltaTime;
                if (waypointTimer >= 10f) // Wait 10 seconds in cover
                {
                    Debug.Log($"{gameObject.name}: Investigation complete, returning to patrol");
                    ReleaseCover();
                    TransitionToState(AIState.Patrol);
                    hasLastKnownPosition = false;
                    waypointTimer = 0f;
                }
            }
        }
        else
        {
            // No cover, move cautiously to last known position
            if (hasLastKnownPosition)
            {
                SetDestination(lastKnownPlayerPosition);

                // If reached investigation point and no target, search the area
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    if (currentTarget == null)
                    {
                        // SEARCH PATTERN: Look around and check nearby positions
                        waypointTimer += Time.deltaTime;

                        if (waypointTimer < 3f)
                        {
                            // First 3 seconds: Look around 360 degrees
                            float lookAngle = (waypointTimer / 3f) * 360f;
                            Vector3 lookDir = Quaternion.Euler(0, lookAngle, 0) * Vector3.forward;
                            LookAtTarget(transform.position + lookDir * 5f);
                        }
                        else if (waypointTimer < 8f)
                        {
                            // Next 5 seconds: Check random nearby positions (might be hiding)
                            if (waypointTimer % 2f < 0.1f) // Every 2 seconds
                            {
                                Vector3 searchOffset = new Vector3(
                                    Random.Range(-3f, 3f),
                                    0f,
                                    Random.Range(-3f, 3f)
                                );
                                Vector3 searchPosition = lastKnownPlayerPosition + searchOffset;
                                SetDestination(searchPosition);
                                Debug.Log($"{gameObject.name}: Searching nearby position at {searchPosition}");
                            }
                        }
                        else
                        {
                            // After 8 seconds of searching, give up
                            Debug.Log($"{gameObject.name}: Investigation complete, nothing found. Returning to patrol");
                            TransitionToState(AIState.Patrol);
                            hasLastKnownPosition = false;
                            waypointTimer = 0f;
                        }
                    }
                }
            }
            else
            {
                // No position to investigate, return to patrol
                TransitionToState(AIState.Patrol);
            }
        }
    }

    void UpdateCombat()
    {
        if (currentTarget == null)
        {
            // Lost target
            if (hasLastKnownPosition)
            {
                TransitionToState(AIState.Investigate);
            }
            else
            {
                TransitionToState(AIState.Patrol);
            }
            return;
        }

        // CHECK FOR TACTICAL RETREAT - wounded AI should fall back
        if (ShouldTacticalRetreat())
        {
            // Move to nearest cover or back toward spawn
            Vector3 retreatPosition = spawnPosition;

            // Try to find cover to retreat to
            if (useCover)
            {
                CoverPoint nearCover = FindNearestCover();
                if (nearCover != null)
                {
                    retreatPosition = nearCover.transform.position;
                    Debug.Log($"{gameObject.name}: Retreating to cover at {retreatPosition}!");
                }
            }

            // Move toward retreat position
            SetDestination(retreatPosition);
            navAgent.speed = combatSpeed * 1.2f; // Move faster when retreating wounded

            // Face the enemy and shoot while backing up (fighting retreat)
            LookAtTarget(currentTarget.position);

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate * 1.5f) // Slower fire while retreating
            {
                TryShootTarget();
                fireTimer = 0f;
            }

            return; // Skip normal combat behavior
        }

        // CHECK IF FALLBACK SYSTEM IS ACTIVE - if so, let it control movement
        var fallbackSystem = GetComponent<Klyra.AI.AIFallbackSystem>();
        if (fallbackSystem != null && fallbackSystem.IsFallingBack())
        {
            // Fallback system is controlling movement - just handle shooting
            LookAtTarget(currentTarget.position);

            // Shoot less frequently while falling back (suppressive fire)
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate * 2f) // Slower fire rate while retreating
            {
                TryShootTarget();
                fireTimer = 0f;
            }
            return; // Let fallback system handle movement
        }

        // TERRITORY DEFENSE CHECK
        float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
        bool outsideTerritory = distanceFromSpawn > territoryRadius;
        bool tooFarFromTerritory = distanceFromSpawn > maxChaseDistance;

        if (defendTerritory && tooFarFromTerritory && returnWhenOutsideTerritory)
        {
            // Too far from territory - return to defensive position
            if (debugDetection)
            {
                Debug.Log($"{gameObject.name}: Too far from territory ({distanceFromSpawn:F1}m), returning to defend spawn area");
            }

            // Move back towards spawn position
            Vector3 returnPosition = Vector3.MoveTowards(transform.position, spawnPosition, 10f);
            SetDestination(returnPosition);

            // Still shoot at target while backing up (defensive fire)
            LookAtTarget(currentTarget.position);
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate * 1.5f) // Slower fire while retreating
            {
                TryShootTarget();
                fireTimer = 0f;
            }

            // If we can't see target anymore, go back to patrol
            if (!CanSeeTarget(currentTarget))
            {
                currentTarget = null;
                TransitionToState(AIState.Patrol);
            }

            return;
        }

        navAgent.speed = combatSpeed;

        // Check if we should seek cover
        // - Recently took damage (within 3 seconds)
        // - OR advanced tactics wants us to seek cover
        bool shouldSeekCover = useCover && (Time.time - lastDamageTime < 3f);

        // If we have advanced tactics and no cover yet, let it decide when to seek
        if (useCover && !hasValidCover && advancedTactics == null)
        {
            // No advanced tactics - use simple cover seeking (always seek cover in combat)
            shouldSeekCover = true;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (shouldSeekCover && !hasValidCover)
        {
            // Find cover using manual cover points
            // If defending territory, prioritize cover within territory
            CoverPoint cover;
            if (defendTerritory && territoryRadius > 0f)
            {
                cover = CoverPoint.FindBestCover(transform.position, currentTarget.position, coverSearchRange, spawnPosition, territoryRadius);
            }
            else
            {
                cover = CoverPoint.FindBestCover(transform.position, currentTarget.position, coverSearchRange);
            }

            if (cover != null && cover.Reserve())
            {
                // Release previous cover if any
                if (currentCoverPoint != null)
                {
                    currentCoverPoint.Release();
                }

                currentCoverPoint = cover;
                hasValidCover = true;

                float coverDistFromSpawn = Vector3.Distance(cover.transform.position, spawnPosition);
                bool coverInTerritory = coverDistFromSpawn <= territoryRadius;

                if (debugDetection && defendTerritory)
                {
                    Debug.Log($"{gameObject.name}: Moving to cover point {cover.name} ({(coverInTerritory ? "INSIDE" : "outside")} territory)");
                }
                else
                {
                    Debug.Log($"{gameObject.name}: Moving to cover point {cover.name}");
                }
            }
        }

        // Move to cover or combat position
        if (hasValidCover && currentCoverPoint != null)
        {
            // Move to cover
            Vector3 coverPos = currentCoverPoint.GetCoverPosition();
            float distanceToCover = Vector3.Distance(transform.position, coverPos);

            if (distanceToCover > 1f)
            {
                // Still moving to cover - prioritize this over combat positioning
                SetDestination(coverPos);

                if (advancedTactics != null && advancedTactics.debugTactics && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"{gameObject.name}: Moving to cover {currentCoverPoint.name} - {distanceToCover:F1}m away");
                }
            }
            else
            {
                // At cover, hold position and face protected direction
                SetDestination(transform.position);

                if (advancedTactics != null && advancedTactics.debugTactics && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"{gameObject.name}: At cover {currentCoverPoint.name} - holding position");
                }

                // Clear cover after a while so we can reposition if needed
                // Only clear if NOT using advanced tactics (which handles relocation)
                if (advancedTactics == null && Time.time - lastDamageTime > 10f)
                {
                    if (currentCoverPoint != null)
                    {
                        currentCoverPoint.Release();
                        currentCoverPoint = null;
                    }
                    hasValidCover = false;
                }
            }
        }
        else
        {
            // Normal combat movement (no cover)
            if (defendTerritory && outsideTerritory)
            {
                // Outside territory but not too far - prefer backing towards spawn
                Vector3 defensivePosition = Vector3.Lerp(transform.position, spawnPosition, 0.3f);
                SetDestination(defensivePosition);

                if (debugDetection && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"{gameObject.name}: Outside territory, taking defensive position closer to spawn");
                }
            }
            else if (distanceToTarget > combatDistance + 2f)
            {
                // Too far - but if defending territory, don't chase aggressively
                if (defendTerritory)
                {
                    // Only move if target is within territory or close to it
                    float targetDistFromSpawn = Vector3.Distance(currentTarget.position, spawnPosition);
                    if (targetDistFromSpawn <= territoryRadius + 5f)
                    {
                        // Target near territory, move to engage
                        SetDestination(currentTarget.position);
                    }
                    else
                    {
                        // Target far from territory, hold position and shoot
                        SetDestination(transform.position);

                        if (debugDetection && Time.frameCount % 120 == 0)
                        {
                            Debug.Log($"{gameObject.name}: Target outside territory, holding defensive position");
                        }
                    }
                }
                else
                {
                    // Not defending territory, chase normally
                    SetDestination(currentTarget.position);
                }
            }
            else if (distanceToTarget < combatDistance - 2f)
            {
                // Too close, back up with random lateral movement
                Vector3 retreatDirection = (transform.position - currentTarget.position).normalized;

                // Add random strafe to make movement unpredictable
                Vector3 strafeDirection = Vector3.Cross(retreatDirection, Vector3.up) * Random.Range(-1f, 1f);
                Vector3 movement = (retreatDirection + strafeDirection * 0.3f).normalized;

                // If defending territory, back up towards spawn
                if (defendTerritory)
                {
                    Vector3 towardsSpawn = (spawnPosition - transform.position).normalized;
                    retreatDirection = Vector3.Lerp(retreatDirection, towardsSpawn, 0.4f).normalized;
                }

                Vector3 retreatPosition = transform.position + movement * 5f;
                SetDestination(retreatPosition);
            }
            else
            {
                // Good distance - occasionally strafe left/right for unpredictability
                if (Random.value < 0.3f) // 30% chance per frame to be strafing
                {
                    Vector3 toCombatTarget = (currentTarget.position - transform.position).normalized;
                    Vector3 strafeRight = Vector3.Cross(toCombatTarget, Vector3.up);

                    // Random strafe direction
                    float strafeDir = Random.value > 0.5f ? 1f : -1f;
                    Vector3 strafePosition = transform.position + strafeRight * strafeDir * 3f;

                    SetDestination(strafePosition);
                }
                else
                {
                    // Hold position
                    SetDestination(transform.position);
                }
            }
        }

        // Face target
        LookAtTarget(currentTarget.position);

        // Shoot
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            TryShootTarget();
            fireTimer = 0f;
        }
    }

    void UpdateCompliant()
    {
        // Stay still, hands up (would trigger animation here)
        navAgent.ResetPath();
    }

    void UpdateFlashbanged()
    {
        // Keep them rooted.
        SetDestination(transform.position);

        // Loop the stun clip manually if one is assigned.
        if (stunGraph.IsValid() && stunClipPlayable.IsValid() && flashbangStunClip != null)
        {
            double t = stunClipPlayable.GetTime();
            if (t >= flashbangStunClip.length)
            {
                stunClipPlayable.SetTime(t % flashbangStunClip.length);
            }
            return;
        }

        // Fallback: no clip assigned — wobble their aim so they look blinded.
        if (characterLocomotion != null)
        {
            float wobbleYaw = Mathf.Sin(Time.time * flashbangWobbleSpeed * Mathf.Deg2Rad) * flashbangWobbleAmplitude;
            Quaternion wobble = Quaternion.AngleAxis(wobbleYaw, Vector3.up) * transform.rotation;
            characterLocomotion.SetRotation(wobble, false);
        }
    }

    #endregion

    #region Obstacle Avoidance

    void CheckForObstacles()
    {
        if (navAgent == null || eyePosition == null) return;

        obstacleAhead = false;

        // Use the character's movement direction, not just forward
        Vector3 moveDirection = navAgent.velocity.normalized;
        if (moveDirection.magnitude < 0.1f)
        {
            moveDirection = transform.forward;
        }

        // Check at multiple heights: low (ankles), middle (waist), and high (chest)
        float[] heights = new float[] { 0.2f, 0.9f, 1.5f };

        foreach (float height in heights)
        {
            // Cast multiple rays in a spread pattern to detect obstacles
            for (int i = 0; i < obstacleRayCount; i++)
            {
                float angle = 0f;
                if (obstacleRayCount > 1)
                {
                    // Spread rays across the detection width
                    float t = i / (float)(obstacleRayCount - 1); // 0 to 1
                    angle = Mathf.Lerp(-30f, 30f, t); // -30 to +30 degrees
                }

                Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * moveDirection;
                Vector3 rayStart = transform.position + Vector3.up * height;

                // Cast ray forward (use physical obstacle mask, not vision obstacle mask)
                RaycastHit hit;
                if (Physics.Raycast(rayStart, rayDirection, out hit, obstacleDetectionDistance, physicalObstacleMask))
                {
                    // Hit something!
                    obstacleAhead = true;
                    consecutiveObstacleHits++;

                    if (debugDetection)
                    {
                        Debug.Log($"{gameObject.name}: Obstacle detected ahead at height {height}m: {hit.collider.name} at {hit.distance:F2}m");
                        Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.red, obstacleCheckInterval);
                    }
                    return; // Stop checking once we find an obstacle
                }
                else
                {
                    if (debugDetection)
                    {
                        Color debugColor = height < 0.5f ? Color.cyan : (height < 1.2f ? Color.green : Color.yellow);
                        Debug.DrawRay(rayStart, rayDirection * obstacleDetectionDistance, debugColor, obstacleCheckInterval);
                    }
                }
            }
        }

        if (!obstacleAhead)
        {
            consecutiveObstacleHits = 0;
        }
    }

    #endregion

    #region Detection

    /// <summary>
    /// Called when AI hears a gunshot
    /// </summary>
    public void OnGunshotHeard(Vector3 soundPosition, GameObject shooter)
    {
        // If already in combat, flashbanged, or compliant, ignore
        if (currentState == AIState.Combat || currentState == AIState.Flashbanged || currentState == AIState.Compliant)
            return;

        float distance = Vector3.Distance(transform.position, soundPosition);

        // Only react if within hearing range
        if (distance > hearingRange) return;

        Debug.Log($"{gameObject.name}: Heard gunshot {distance:F1}m away!");

        lastKnownPlayerPosition = soundPosition;
        hasLastKnownPosition = true;

        // IMMEDIATELY seek cover when hearing gunshots (Ready or Not style)
        if (useCover)
        {
            SeekNearestCover(soundPosition);
        }

        // If we can see the shooter, engage
        if (shooter != null)
        {
            var playerTarget = shooter.GetComponent<PlayerTarget>();
            Transform targetTransform = playerTarget != null && playerTarget.AimPoint != null
                ? playerTarget.AimPoint
                : shooter.transform;

            if (CanSeeTarget(targetTransform))
            {
                currentTarget = targetTransform;
                TransitionToState(AIState.Combat);
                Debug.Log($"{gameObject.name}: Saw the shooter! Engaging!");
                return;
            }
        }

        // Can't see shooter, go investigate the sound (from cover if possible)
        TransitionToState(AIState.Investigate);
    }

    void CheckForThreats()
    {
        var players = PlayerTarget.All;

        if (debugDetection)
        {
            Debug.Log($"[AI:{name}] CheckForThreats: Found {players.Count} PlayerTarget(s) in scene");
        }

        if (players.Count == 0)
        {
            if (debugDetection) Debug.LogWarning($"[AI:{name}] NO PLAYERS FOUND! Make sure player has PlayerTarget component!");
            return;
        }

        bool shouldLog = debugDetection && (Time.time - debugLogTimer) >= 1f;
        if (shouldLog)
        {
            debugLogTimer = Time.time;
            Debug.Log($"[AI:{name}] state={currentState} playersFound={players.Count} eyePos={eyePosition.position} eyeFwd={eyePosition.forward} aiFwd={transform.forward}", this);
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null)
            {
                if (debugDetection) Debug.LogWarning($"[AI:{name}] Player {i} is NULL!");
                continue;
            }

            if (player.gameObject == gameObject)
            {
                if (debugDetection) Debug.Log($"[AI:{name}] Skipping self");
                continue;
            }

            Transform aim = player.AimPoint;
            if (aim == null)
            {
                if (debugDetection) Debug.LogWarning($"[AI:{name}] Player {player.name} has NULL AimPoint!");
                continue;
            }

            float distance = Vector3.Distance(transform.position, aim.position);

            if (shouldLog)
            {
                Vector3 dir = (aim.position - eyePosition.position).normalized;
                float angle = Vector3.Angle(eyePosition.forward, dir);
                bool losBlocked = Physics.Raycast(eyePosition.position, dir, distance, obstacleMask);
                Debug.Log($"[AI:{name}]  -> player={player.name} dist={distance:F1}/{sightRange} angle={angle:F1}/{fieldOfView/2f} losBlocked={losBlocked} obstacleMask={obstacleMask.value}", this);

                // Draw a debug ray to the player
                Debug.DrawLine(eyePosition.position, aim.position, losBlocked ? Color.red : Color.green, 1f);
            }

            // CLOSE RANGE DETECTION: If player is very close (3m), detect them regardless of FOV
            // This simulates peripheral vision and noticing movement right next to you
            if (distance <= 3f)
            {
                if (debugDetection)
                {
                    Debug.Log($"[AI:{name}] CLOSE RANGE DETECTION! Player {player.name} is only {distance:F1}m away - detecting regardless of FOV!");
                }
                OnPlayerDetected(aim);
                lastKnownPlayerPosition = aim.position;
                hasLastKnownPosition = true;
                UpdatePlayerRoom(aim);
            }
            // NORMAL DETECTION: Check FOV and line of sight
            else if (distance <= sightRange && CanSeeTarget(aim))
            {
                OnPlayerDetected(aim);
                lastKnownPlayerPosition = aim.position;
                hasLastKnownPosition = true;

                // Update player's room
                UpdatePlayerRoom(aim);
            }
        }
    }

    bool CanSeeTarget(Transform target)
    {
        if (eyePosition == null || target == null) return false;

        Vector3 directionToTarget = (target.position - eyePosition.position).normalized;
        float angleToTarget = Vector3.Angle(eyePosition.forward, directionToTarget);
        float distanceToTarget = Vector3.Distance(eyePosition.position, target.position);

        // TEMP DEBUG: Always log when checking vision
        if (debugDetection)
        {
            Debug.Log($"[{name}] CanSeeTarget check: angle={angleToTarget:F1}° (max={fieldOfView/2f}°) dist={distanceToTarget:F1}m (max={sightRange}m)");
        }

        // Check angle first
        if (angleToTarget > fieldOfView / 2f)
        {
            if (debugDetection) Debug.Log($"[{name}] FAILED: Target outside FOV ({angleToTarget:F1}° > {fieldOfView/2f}°)");
            return false;
        }

        // Check distance
        if (distanceToTarget > sightRange)
        {
            if (debugDetection) Debug.Log($"[{name}] FAILED: Target too far ({distanceToTarget:F1}m > {sightRange}m)");
            return false;
        }

        // Line of sight check
        RaycastHit hit;
        if (Physics.Raycast(eyePosition.position, directionToTarget, out hit, distanceToTarget, obstacleMask))
        {
            if (debugDetection)
            {
                Debug.Log($"[{name}] FAILED: Line of sight blocked by {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            return false;
        }

        // Can see!
        if (debugDetection) Debug.Log($"[{name}] SUCCESS: Can see target!");
        return true;
    }

    void OnPlayerDetected(Transform player)
    {
        currentTarget = player;

        if (currentState == AIState.Patrol || currentState == AIState.Investigate)
        {
            Debug.Log($"{gameObject.name}: Player detected! Engaging!");
            TransitionToState(AIState.Combat);

            // ALERT NEARBY TEAMMATES IMMEDIATELY
            AlertNearbyAllies(player.position);
        }
    }

    /// <summary>
    /// Alert all nearby AI teammates about player position
    /// This makes AI coordinate and respond as a team
    /// </summary>
    void AlertNearbyAllies(Vector3 playerPosition)
    {
        // Find all nearby AI within communication range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 50f); // 50m communication range

        int alliesAlerted = 0;
        foreach (var col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;

            // Check if it's another AI
            var allyAI = col.GetComponent<TacticalAI>();
            if (allyAI == null)
                allyAI = col.GetComponentInParent<TacticalAI>();

            if (allyAI != null && allyAI.enabled)
            {
                // Don't alert if they're already in combat
                if (allyAI.currentState == AIState.Combat) continue;

                // Alert them to the player's position
                allyAI.OnAllySpottedEnemy(playerPosition);
                alliesAlerted++;
            }
        }

        if (alliesAlerted > 0)
        {
            Debug.Log($"{gameObject.name}: Alerted {alliesAlerted} teammates about enemy at {playerPosition}!");
        }
    }

    /// <summary>
    /// Called when a teammate spots an enemy and alerts this AI
    /// </summary>
    public void OnAllySpottedEnemy(Vector3 enemyPosition)
    {
        Debug.Log($"{gameObject.name}: Teammate called out enemy at {enemyPosition}! Investigating!");

        // Store the last known position
        lastKnownPlayerPosition = enemyPosition;
        hasLastKnownPosition = true;

        // If we're just patrolling, go investigate
        if (currentState == AIState.Patrol)
        {
            TransitionToState(AIState.Investigate);
        }

        // Increase alert level
        alertLevel = Mathf.Max(alertLevel, 0.5f);
    }

    /// <summary>
    /// Called when a door in this AI's room opens
    /// </summary>
    public void OnDoorOpened(Vector3 doorPosition, GameObject door)
    {
        // Ignore if dead, flashbanged, or compliant
        if (!this.enabled || currentState == AIState.Flashbanged || currentState == AIState.Compliant)
            return;

        Debug.Log($"[TacticalAI] {gameObject.name}: Door {door.name} OPENED at {doorPosition}!");

        // If we're in combat, don't get distracted by doors
        if (currentState == AIState.Combat && currentTarget != null)
        {
            Debug.Log($"[TacticalAI] {gameObject.name}: In combat - ignoring door");
            return;
        }

        // TURN TO LOOK AT THE DOOR
        Vector3 directionToDoor = (doorPosition - transform.position).normalized;
        directionToDoor.y = 0; // Keep on horizontal plane

        if (directionToDoor != Vector3.zero)
        {
            // Smoothly turn toward door over 0.5 seconds
            StartCoroutine(SmoothTurnToward(directionToDoor, 0.5f));
            Debug.Log($"[TacticalAI] {gameObject.name}: Turning to face door at {doorPosition}");
        }

        // Raise alert level - someone might be coming through
        alertLevel = Mathf.Max(alertLevel, 0.3f);

        // If we're on patrol and door opens, go on alert
        if (currentState == AIState.Patrol)
        {
            Debug.Log($"[TacticalAI] {gameObject.name}: Door opened while on patrol - going to Investigate state");
            lastKnownPlayerPosition = doorPosition;
            hasLastKnownPosition = true;
            TransitionToState(AIState.Investigate);
        }
    }

    /// <summary>
    /// Called when a door in this AI's room closes
    /// </summary>
    public void OnDoorClosed(Vector3 doorPosition, GameObject door)
    {
        // Ignore if dead, flashbanged, or compliant
        if (!this.enabled || currentState == AIState.Flashbanged || currentState == AIState.Compliant)
            return;

        Debug.Log($"[TacticalAI] {gameObject.name}: Door {door.name} CLOSED at {doorPosition}");

        // Less important than opening, but still worth noting
        // Someone might have just passed through
    }

    /// <summary>
    /// Smoothly turn toward a direction over time
    /// </summary>
    System.Collections.IEnumerator SmoothTurnToward(Vector3 direction, float duration)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    float GetHealthPercentage()
    {
        var health = GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
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

    /// <summary>
    /// Check if AI should tactically retreat due to being wounded
    /// </summary>
    bool ShouldTacticalRetreat()
    {
        float healthPercent = GetHealthPercentage();

        // If critically wounded (below 30% health), consider retreating
        if (healthPercent < 0.3f)
        {
            // 70% chance to retreat when critically wounded
            if (Random.value < 0.7f)
            {
                Debug.Log($"{gameObject.name}: CRITICALLY WOUNDED ({healthPercent * 100f:F0}% HP) - tactical retreat!");
                return true;
            }
        }
        // If wounded (below 50% health), might retreat
        else if (healthPercent < 0.5f)
        {
            // 30% chance to retreat when wounded
            if (Random.value < 0.3f)
            {
                Debug.Log($"{gameObject.name}: Wounded ({healthPercent * 100f:F0}% HP) - tactical retreat!");
                return true;
            }
        }

        return false;
    }

    void UpdateAlertLevel()
    {
        if (currentTarget != null && CanSeeTarget(currentTarget))
        {
            alertLevel += alertIncreaseRate * Time.deltaTime;
        }
        else
        {
            alertLevel -= alertDecreaseRate * Time.deltaTime;
        }

        alertLevel = Mathf.Clamp01(alertLevel);
    }

    #endregion

    #region Combat

    void TryShootTarget()
    {
        if (currentTarget == null && !hasLastKnownPosition) return;
        if (characterLocomotion == null || useAbility == null) return;

        // Check if advanced tactics allow shooting (for peek/hide behavior)
        if (advancedTactics != null && !advancedTactics.ShouldAllowShooting())
        {
            // AI is in cover, not peeking - don't shoot
            return;
        }

        // SUPPRESSIVE FIRE: If we can't see target but know where they were, shoot there anyway
        bool canSeeTarget = currentTarget != null && CanSeeTarget(currentTarget);
        bool usingSuppressiveFire = false;

        if (!canSeeTarget && hasLastKnownPosition)
        {
            // 40% chance to lay down suppressive fire at last known position
            if (Random.value < 0.4f)
            {
                usingSuppressiveFire = true;
                Debug.Log($"{gameObject.name}: SUPPRESSIVE FIRE at last known position!");
            }
            else
            {
                return; // Don't shoot if can't see and not doing suppressive fire
            }
        }
        else if (!canSeeTarget)
        {
            return; // Can't see and no last known position
        }

        // Don't try to fire while reloading.
        if (reloadAbility != null && reloadAbility.IsActive) return;

        // Keep the weapon aimed so firing lines up correctly.
        if (aimAbility != null && !aimAbility.IsActive)
        {
            characterLocomotion.TryStartAbility(aimAbility);
        }

        // Aim at the right target
        Vector3 aimTarget = usingSuppressiveFire ? lastKnownPlayerPosition : currentTarget.position;

        // Add random spread for suppressive fire (less accurate)
        if (usingSuppressiveFire)
        {
            aimTarget += new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-1f, 1f),
                Random.Range(-2f, 2f)
            );
        }

        LookAtTarget(aimTarget);

        // Apply accuracy (skip *this* pull of the trigger, not the whole cycle).
        // Suppressive fire is less accurate
        float effectiveAccuracy = usingSuppressiveFire ? accuracy * 0.3f : accuracy;
        if (Random.value > effectiveAccuracy) return;

        // Use is a press/release ability. Release any prior press so the next
        // Start is actually allowed to begin — otherwise IsActive stays true
        // forever after the first shot and the weapon silently stops firing.
        if (useAbility.IsActive)
        {
            characterLocomotion.TryStopAbility(useAbility);
        }

        bool started = characterLocomotion.TryStartAbility(useAbility);
        if (started)
        {
            dryFireCount = 0;
            Debug.Log($"{gameObject.name}: Firing at target!");

            // Notify room awareness system of gunshot
            var roomAwareness = GetComponent<Klyra.AI.AIRoomAwareness>();
            if (roomAwareness != null)
            {
                roomAwareness.OnWeaponFired();
            }

            // Notify advanced tactics that we fired a shot
            if (advancedTactics != null)
            {
                advancedTactics.OnShotFired();
            }
        }
        else
        {
            // Start refused — most commonly an empty mag. After a couple failed
            // attempts, trigger a reload.
            dryFireCount++;
            if (dryFireCount >= 2 && reloadAbility != null && !reloadAbility.IsActive)
            {
                if (characterLocomotion.TryStartAbility(reloadAbility))
                {
                    Debug.Log($"{gameObject.name}: Reloading");
                    dryFireCount = 0;
                }
            }
        }
    }

    void LookAtTarget(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction.normalized);

        // UCC's locomotion overwrites transform.rotation each frame, so we have
        // to go through SetRotation. Lerp toward the target for a smooth turn.
        if (characterLocomotion != null)
        {
            Quaternion smoothed = Quaternion.Slerp(characterLocomotion.Rotation, target, Time.deltaTime * 5f);
            characterLocomotion.SetRotation(smoothed, false);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 5f);
        }
    }

    #endregion

    #region Voice Line Response

    public void OnVoiceCommandHeard(Vector3 sourcePosition, string command)
    {
        float distance = Vector3.Distance(transform.position, sourcePosition);

        if (distance > voiceCommandRange) return;

        if (currentState == AIState.Flashbanged || currentState == AIState.Compliant) return;

        Debug.Log($"{gameObject.name}: Heard voice command: {command} from {distance:F1}m away");

        // Chance to comply
        if (Random.value < complianceChance)
        {
            Debug.Log($"{gameObject.name}: Complying with command!");
            StartCoroutine(ComplyWithCommand());
        }
        else
        {
            Debug.Log($"{gameObject.name}: Refusing to comply - engaging!");
            if (currentState != AIState.Combat)
            {
                // Investigate the voice source
                lastKnownPlayerPosition = sourcePosition;
                hasLastKnownPosition = true;
                TransitionToState(AIState.Investigate);
            }
        }
    }

    public IEnumerator ComplyWithCommand()
    {
        AIState previousState = currentState;
        TransitionToState(AIState.Compliant);
        isCompliant = true;

        yield return new WaitForSeconds(complianceDuration);

        isCompliant = false;
        TransitionToState(previousState);
        Debug.Log($"{gameObject.name}: No longer compliant");
    }

    #endregion

    #region Flashbang Response

    public void OnFlashbanged(float duration)
    {
        if (isFlashbanged) return;

        Debug.Log($"{gameObject.name}: FLASHBANGED! Stunned for {duration}s");
        StartCoroutine(FlashbangStun(duration));
    }

    IEnumerator FlashbangStun(float duration)
    {
        TransitionToState(AIState.Flashbanged);
        isFlashbanged = true;

        // Drop target and stop the character dead in its tracks so they're not
        // still firing or walking toward us while blinded.
        currentTarget = null;
        if (characterLocomotion != null)
        {
            if (useAbility != null && useAbility.IsActive) characterLocomotion.TryStopAbility(useAbility);
            if (aimAbility != null && aimAbility.IsActive) characterLocomotion.TryStopAbility(aimAbility);
        }
        SetDestination(transform.position);

        StartStunAnimation();

        yield return new WaitForSeconds(duration);

        StopStunAnimation();

        // Clear any stuck ability state from before/during the stun.
        if (characterLocomotion != null && reloadAbility != null && reloadAbility.IsActive)
        {
            characterLocomotion.TryStopAbility(reloadAbility, true);
        }
        dryFireCount = 0;
        fireTimer = 0f;

        isFlashbanged = false;
        TransitionToState(AIState.Patrol); // Reset to patrol after flashbang
        Debug.Log($"{gameObject.name}: Recovered from flashbang");
    }

    void StartStunAnimation()
    {
        if (flashbangStunClip == null || animator == null) return;
        if (stunGraph.IsValid()) return;

        stunGraph = PlayableGraph.Create($"FlashbangStun_{name}");
        stunGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        stunClipPlayable = AnimationClipPlayable.Create(stunGraph, flashbangStunClip);
        stunClipPlayable.SetApplyFootIK(true);

        var output = AnimationPlayableOutput.Create(stunGraph, "Animation", animator);
        output.SetSourcePlayable(stunClipPlayable);

        stunGraph.Play();
    }

    void StopStunAnimation()
    {
        if (!stunGraph.IsValid()) return;
        stunGraph.Destroy();
        // Do NOT call animator.Rebind() — that zeros out UCC's animator
        // parameters (Slot0ItemStateIndex etc.) which makes UCC believe the
        // item was unequipped, and Use/Aim silently refuse to fire afterwards.
        // Destroying the graph alone hands output back to the controller.
    }

    #endregion

    #region Cover System

    /// <summary>
    /// Immediately find and move to nearest cover
    /// </summary>
    void SeekNearestCover(Vector3 threatPosition)
    {
        if (!useCover) return;

        // Release old cover
        if (currentCoverPoint != null)
        {
            currentCoverPoint.Release();
            currentCoverPoint = null;
            hasValidCover = false;
        }

        // Find nearest cover
        CoverPoint cover = CoverPoint.FindBestCover(transform.position, threatPosition, coverSearchRange);
        if (cover != null && cover.Reserve())
        {
            currentCoverPoint = cover;
            hasValidCover = true;

            // Move to cover immediately
            Vector3 coverPos = currentCoverPoint.GetCoverPosition();
            SetDestination(coverPos);

            Debug.Log($"{gameObject.name}: Moving to cover at {cover.name} due to gunshot!");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No cover found nearby!");
        }
    }

    /// <summary>
    /// Release current cover when leaving combat or dying
    /// </summary>
    /// <summary>
    /// Find nearest cover point for retreating
    /// </summary>
    CoverPoint FindNearestCover()
    {
        Vector3 threatPos = currentTarget != null ? currentTarget.position : lastKnownPlayerPosition;
        return CoverPoint.FindBestCover(transform.position, threatPos, coverSearchRange);
    }

    void ReleaseCover()
    {
        if (currentCoverPoint != null)
        {
            currentCoverPoint.Release();
            currentCoverPoint = null;
            hasValidCover = false;
        }
    }

    #endregion

    #region Public Methods for Advanced Tactics

    /// <summary>
    /// Force AI to find and move to cover (called by AdvancedAICombatTactics)
    /// </summary>
    public void ForceSeekCover()
    {
        if (!useCover || currentState != AIState.Combat) return;

        // Find cover
        CoverPoint cover = CoverPoint.FindBestCover(transform.position,
            currentTarget != null ? currentTarget.position : transform.position,
            coverSearchRange);

        if (cover != null && cover.Reserve())
        {
            // Release previous cover if any
            if (currentCoverPoint != null)
            {
                currentCoverPoint.Release();
            }

            currentCoverPoint = cover;
            hasValidCover = true;
            Debug.Log($"{gameObject.name}: Force seeking cover at {cover.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name}: No cover available to seek");
        }
    }

    #endregion

    #region State Management

    void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"{gameObject.name}: State change: {currentState} → {newState}");

        // Exit old state
        switch (currentState)
        {
            case AIState.Patrol:
                waypointTimer = 0f;
                break;
            case AIState.Combat:
                // Lower the weapon when leaving combat.
                if (characterLocomotion != null && aimAbility != null && aimAbility.IsActive)
                {
                    characterLocomotion.TryStopAbility(aimAbility);
                }
                if (characterLocomotion != null && useAbility != null && useAbility.IsActive)
                {
                    characterLocomotion.TryStopAbility(useAbility);
                }
                dryFireCount = 0;
                // Release cover when leaving combat
                ReleaseCover();
                break;
        }

        // Enter new state
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrol:
                alertLevel = 0f;
                if (movementMode == MovementMode.Patrol && patrolWaypoints != null && patrolWaypoints.Length > 0)
                {
                    SetDestination(patrolWaypoints[currentWaypointIndex].position);
                }
                else if (movementMode == MovementMode.Roam)
                {
                    PickNewRoamTarget();
                }
                // Idle mode - just stay put
                break;

            case AIState.Combat:
                alertLevel = 1f;
                break;
        }
    }

    void SetDestination(Vector3 destination)
    {
        // Go through UCC's pathfinding ability so it can bridge the NavMeshAgent
        // path into the character's locomotion. Calling navAgent.SetDestination
        // directly bypasses that bridge and the character doesn't actually follow
        // the path — it ends up walking straight at the target through walls.
        if (pathfindingMovement != null)
        {
            pathfindingMovement.SetDestination(destination);

            if (advancedTactics != null && advancedTactics.debugTactics && hasValidCover && Time.frameCount % 60 == 0)
            {
                Debug.Log($"{gameObject.name}: SetDestination via PathfindingMovement to {destination} (distance: {Vector3.Distance(transform.position, destination):F1}m)");
            }
            return;
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destination);

            if (advancedTactics != null && advancedTactics.debugTactics && hasValidCover && Time.frameCount % 60 == 0)
            {
                Debug.Log($"{gameObject.name}: SetDestination via NavAgent to {destination} (distance: {Vector3.Distance(transform.position, destination):F1}m)");
            }
        }
        else if (advancedTactics != null && advancedTactics.debugTactics)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot set destination - NavAgent null or not on NavMesh!");
        }
    }

    #endregion

    #region Room Awareness

    /// <summary>
    /// Called by RoomVolume when AI enters a room
    /// </summary>
    public void OnEnteredRoom(Klyra.AI.RoomVolume room)
    {
        if (!enableRoomAwareness) return;

        currentRoom = room;

        if (debugRooms)
        {
            Debug.Log($"[{gameObject.name}] Entered {room.roomName} (Floor {room.floorNumber})");
        }
    }

    /// <summary>
    /// Called by RoomVolume when AI exits a room
    /// </summary>
    public void OnExitedRoom(Klyra.AI.RoomVolume room)
    {
        if (!enableRoomAwareness) return;

        if (currentRoom == room)
        {
            currentRoom = null;
        }

        if (debugRooms)
        {
            Debug.Log($"[{gameObject.name}] Exited {room.roomName}");
        }
    }

    /// <summary>
    /// Get the room this AI is currently in
    /// </summary>
    public Klyra.AI.RoomVolume GetCurrentRoom()
    {
        return currentRoom;
    }

    /// <summary>
    /// Get the room the player was last seen in
    /// </summary>
    public Klyra.AI.RoomVolume GetLastKnownPlayerRoom()
    {
        return lastKnownPlayerRoom;
    }

    /// <summary>
    /// Update player's last known room (called when detecting player)
    /// </summary>
    void UpdatePlayerRoom(Transform player)
    {
        if (!enableRoomAwareness || player == null) return;

        var playerRoom = Klyra.AI.RoomManager.GetRoomAtPosition(player.position);
        if (playerRoom != null && playerRoom != lastKnownPlayerRoom)
        {
            lastKnownPlayerRoom = playerRoom;

            if (debugRooms)
            {
                Debug.Log($"[{gameObject.name}] Player spotted in {playerRoom.roomName}");
            }
        }
    }

    /// <summary>
    /// Check if AI is in the same room as target
    /// </summary>
    public bool IsInSameRoomAs(Transform target)
    {
        if (!enableRoomAwareness || currentRoom == null) return false;

        var targetRoom = Klyra.AI.RoomManager.GetRoomAtPosition(target.position);
        return targetRoom == currentRoom;
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        if (!debugVision) return;

        // Sight range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Field of view
        if (eyePosition != null)
        {
            Vector3 forward = eyePosition.forward;
            Vector3 rightBound = Quaternion.Euler(0, fieldOfView / 2f, 0) * forward;
            Vector3 leftBound = Quaternion.Euler(0, -fieldOfView / 2f, 0) * forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(eyePosition.position, rightBound * sightRange);
            Gizmos.DrawRay(eyePosition.position, leftBound * sightRange);

            // Draw center vision ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(eyePosition.position, forward * sightRange);
        }

        // Current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }

        // Last known position
        if (hasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
        }

        // Roaming area (if in roam mode)
        if (movementMode == MovementMode.Roam)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(spawnPosition, roamRadius);

            // Current roam target
            if (hasRoamTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentRoamTarget, 0.5f);
                Gizmos.DrawLine(transform.position, currentRoamTarget);
            }
        }

        // Draw NavMesh path
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.white;
            var path = navAgent.path;
            Vector3 prevCorner = transform.position;
            foreach (var corner in path.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawWireSphere(corner, 0.3f);
                prevCorner = corner;
            }
        }
    }

    #endregion
}
