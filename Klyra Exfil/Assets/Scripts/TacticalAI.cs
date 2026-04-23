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
    [Tooltip("Mixamo / humanoid clip to play while stunned. Leave empty to use the wobble-only fallback.")]
    public AnimationClip flashbangStunClip;
    [Tooltip("How violently the AI wobbles their aim while stunned. Ignored when a stun clip is assigned.")]
    public float flashbangWobbleSpeed = 180f;
    [Tooltip("How far the AI's aim can swing off-center while stunned. Ignored when a stun clip is assigned.")]
    public float flashbangWobbleAmplitude = 90f;

    [Header("References")]
    public Transform eyePosition; // For line of sight checks

    [Header("Debug")]
    [Tooltip("Log a detection summary once per second.")]
    public bool debugDetection = false;
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
        if (stunGraph.IsValid()) stunGraph.Destroy();
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
            // Good distance, stop moving — setting destination to our current
            // position makes the pathfinding ability arrive immediately.
            SetDestination(transform.position);
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

    #region Detection

    void CheckForThreats()
    {
        var players = PlayerTarget.All;

        bool shouldLog = debugDetection && (Time.time - debugLogTimer) >= 1f;
        if (shouldLog)
        {
            debugLogTimer = Time.time;
            Debug.Log($"[AI:{name}] state={currentState} playersFound={players.Count} eyeFwd={eyePosition.forward} aiFwd={transform.forward}", this);
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || player.gameObject == gameObject) continue;

            Transform aim = player.AimPoint;
            float distance = Vector3.Distance(transform.position, aim.position);

            if (shouldLog)
            {
                Vector3 dir = (aim.position - eyePosition.position).normalized;
                float angle = Vector3.Angle(eyePosition.forward, dir);
                bool losBlocked = Physics.Raycast(eyePosition.position, dir, distance, obstacleMask);
                Debug.Log($"[AI:{name}]  -> player={player.name} dist={distance:F1}/{sightRange} angle={angle:F1}/{fieldOfView/2f} losBlocked={losBlocked} obstacleMask={obstacleMask.value}", this);
            }

            if (distance <= sightRange && CanSeeTarget(aim))
            {
                OnPlayerDetected(aim);
                lastKnownPlayerPosition = aim.position;
                hasLastKnownPosition = true;
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
        if (characterLocomotion == null || useAbility == null) return;

        // Check if can see target
        if (!CanSeeTarget(currentTarget)) return;

        // Don't try to fire while reloading.
        if (reloadAbility != null && reloadAbility.IsActive) return;

        // Keep the weapon aimed so firing lines up correctly.
        if (aimAbility != null && !aimAbility.IsActive)
        {
            characterLocomotion.TryStartAbility(aimAbility);
        }

        // Apply accuracy (skip *this* pull of the trigger, not the whole cycle).
        if (Random.value > accuracy) return;

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
        // Go through UCC's pathfinding ability so it can bridge the NavMeshAgent
        // path into the character's locomotion. Calling navAgent.SetDestination
        // directly bypasses that bridge and the character doesn't actually follow
        // the path — it ends up walking straight at the target through walls.
        if (pathfindingMovement != null)
        {
            pathfindingMovement.SetDestination(destination);
            return;
        }

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
