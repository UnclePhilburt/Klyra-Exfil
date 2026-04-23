using UnityEngine;
using UnityEngine.AI;
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

    [Header("Patrol Settings")]
    [Tooltip("Patrol waypoints - will cycle through these")]
    public Transform[] patrolWaypoints;
    [Tooltip("Wait time at each waypoint")]
    public float waypointWaitTime = 3f;
    [Tooltip("Speed while patrolling")]
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

    [Header("References")]
    public Transform eyePosition; // For line of sight checks

    // Private state
    private NavMeshAgent navAgent;
    private Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion characterLocomotion;
    private Opsive.UltimateCharacterController.Character.Abilities.Items.Use useAbility;
    private Transform currentTarget;
    private int currentWaypointIndex = 0;
    private float waypointTimer = 0f;
    private float fireTimer = 0f;
    private bool isCompliant = false;
    private bool isFlashbanged = false;
    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPosition = false;

    public enum AIState
    {
        Patrol,
        Investigate,
        Combat,
        Compliant,
        Flashbanged
    }

    void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        characterLocomotion = GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

        if (characterLocomotion != null)
        {
            useAbility = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Items.Use>();
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

        // Start patrol
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            SetDestination(patrolWaypoints[0].position);
        }

        // Subscribe to death event
        Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnAIDeath);

        Debug.Log($"TacticalAI initialized on {gameObject.name}");
    }

    void OnDestroy()
    {
        // Unsubscribe from death event
        Opsive.Shared.Events.EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnAIDeath);
    }

    void OnAIDeath(Vector3 position, Vector3 force, GameObject attacker)
    {
        Debug.Log($"{gameObject.name} died");

        // Disable AI
        this.enabled = false;

        // Disable NavMesh agent
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return; // Only control on owner's client

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

        // Always check for threats (unless flashbanged or compliant)
        if (currentState != AIState.Flashbanged && currentState != AIState.Compliant)
        {
            CheckForThreats();
        }

        // Update alert level
        UpdateAlertLevel();
    }

    #region State Updates

    void UpdatePatrol()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        navAgent.speed = patrolSpeed;

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

    void UpdateInvestigate()
    {
        navAgent.speed = combatSpeed;

        // Go to last known position
        if (hasLastKnownPosition)
        {
            SetDestination(lastKnownPlayerPosition);

            // If reached investigation point and no target, return to patrol
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                if (currentTarget == null)
                {
                    Debug.Log($"{gameObject.name}: Investigation complete, returning to patrol");
                    TransitionToState(AIState.Patrol);
                    hasLastKnownPosition = false;
                }
            }
        }
        else
        {
            // No position to investigate, return to patrol
            TransitionToState(AIState.Patrol);
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

        navAgent.speed = combatSpeed;

        // Move to combat distance
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget > combatDistance + 2f)
        {
            // Too far, move closer
            SetDestination(currentTarget.position);
        }
        else if (distanceToTarget < combatDistance - 2f)
        {
            // Too close, back up
            Vector3 retreatPosition = transform.position + (transform.position - currentTarget.position).normalized * 5f;
            SetDestination(retreatPosition);
        }
        else
        {
            // Good distance, stop moving
            navAgent.ResetPath();
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
        // Stumble around blindly
        navAgent.ResetPath();
    }

    #endregion

    #region Detection

    void CheckForThreats()
    {
        // Find all players in range
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in players)
        {
            if (playerObj == gameObject) continue; // Skip self

            float distance = Vector3.Distance(transform.position, playerObj.transform.position);

            // Check sight
            if (distance <= sightRange)
            {
                if (CanSeeTarget(playerObj.transform))
                {
                    OnPlayerDetected(playerObj.transform);
                    lastKnownPlayerPosition = playerObj.transform.position;
                    hasLastKnownPosition = true;
                }
            }
        }
    }

    bool CanSeeTarget(Transform target)
    {
        Vector3 directionToTarget = (target.position - eyePosition.position).normalized;
        float angleToTarget = Vector3.Angle(eyePosition.forward, directionToTarget);

        if (angleToTarget <= fieldOfView / 2f)
        {
            float distanceToTarget = Vector3.Distance(eyePosition.position, target.position);

            // Line of sight check
            if (!Physics.Raycast(eyePosition.position, directionToTarget, distanceToTarget, obstacleMask))
            {
                return true;
            }
        }

        return false;
    }

    void OnPlayerDetected(Transform player)
    {
        currentTarget = player;

        if (currentState == AIState.Patrol || currentState == AIState.Investigate)
        {
            Debug.Log($"{gameObject.name}: Player detected! Engaging!");
            TransitionToState(AIState.Combat);
        }
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
        if (currentTarget == null) return;
        if (characterLocomotion == null) return;

        // Check if can see target
        if (!CanSeeTarget(currentTarget))
        {
            return;
        }

        // Apply accuracy
        if (Random.value > accuracy)
        {
            Debug.Log($"{gameObject.name}: Shot missed (accuracy)");
            return;
        }

        // Try to use weapon (fire) - Use the CharacterLocomotion to start the Use ability
        if (useAbility != null && !useAbility.IsActive)
        {
            characterLocomotion.TryStartAbility(useAbility);
        }

        Debug.Log($"{gameObject.name}: Firing at target!");
    }

    void LookAtTarget(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0; // Keep on horizontal plane

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
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

    IEnumerator ComplyWithCommand()
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
        AIState previousState = currentState;
        TransitionToState(AIState.Flashbanged);
        isFlashbanged = true;

        // Drop target
        currentTarget = null;

        yield return new WaitForSeconds(duration);

        isFlashbanged = false;
        TransitionToState(AIState.Patrol); // Reset to patrol after flashbang
        Debug.Log($"{gameObject.name}: Recovered from flashbang");
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
        }

        // Enter new state
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrol:
                alertLevel = 0f;
                if (patrolWaypoints != null && patrolWaypoints.Length > 0)
                {
                    SetDestination(patrolWaypoints[currentWaypointIndex].position);
                }
                break;

            case AIState.Combat:
                alertLevel = 1f;
                break;
        }
    }

    void SetDestination(Vector3 destination)
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destination);
        }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
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
        }

        // Current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        // Last known position
        if (hasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
        }
    }

    #endregion
}
