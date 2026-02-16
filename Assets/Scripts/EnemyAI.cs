using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Available high-level states for the enemy AI state machine.
public enum EnemyStateType
{
    Patrolling,
    Suspicious,
    Alerted,
    Searching
}

// Core enemy controller: manages the state machine, movement, combat,
// obstacle avoidance, collision teleportation, and item-effect reactions.
// Implements IEnemyDetection so external systems can toggle awareness.
public class EnemyAI : MonoBehaviour, IEnemyDetection
{
    // ========================================================================
    // Serialized Fields
    // ========================================================================

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer = 1;
    public float avoidanceDistance = 1f;
    public float avoidanceForce = 2f;
    public float sideRayOffset = 0.3f;
    public float rayDistance = 1f;

    [Header("Combat")]
    public int damage = 10;
    public float attackCooldown = 1f;
    public float attackRange = 1.5f;

    [Header("Collision Teleport")]
    [Tooltip("When stuck on terrain, the enemy disappears and reappears at its last patrol point")]
    public bool enableCollisionTeleport = true;
    public float teleportCooldown = 5f;
    public float disappearDuration = 1f;
    public LayerMask terrainLayer = 1;

    [Header("Smoke Effects")]
    public ParticleSystem smokeEffect;

    [Header("State Machine")]
    public EnemyStateType initialState = EnemyStateType.Patrolling;

    [Header("Patrol")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float patrolSpeed = 2f;
    public float waitTimeAtPoints = 2f;
    public float reachedDistance = 0.5f;

    [Header("References")]
    public FieldOfView fov;
    public Rigidbody2D rb;
    public Animator animator;

    // ========================================================================
    // Private State
    // ========================================================================

    private bool canDetectPlayer = true;

    // Combat
    private float lastAttackTime;
    private bool isAttacking;

    // Teleport
    private float lastTeleportTime;
    private bool isTeleporting;
    private int lastPatrolIndex;
    private Vector2 lastPatrolPosition;
    private Coroutine teleportCoroutine;

    // Distraction
    private bool isDistracted;

    // State machine
    private IEnemyState currentState;

    // Whether the enemy is currently moving (read by animation / external systems).
    public bool IsMoving { get; private set; }

    // Cached component lookups
    private Transform playerTransform;

    // ========================================================================
    // IEnemyDetection
    // ========================================================================

    public void EnableDetection() => canDetectPlayer = true;
    public void DisableDetection() => canDetectPlayer = false;
    public bool CanDetectPlayer() => canDetectPlayer;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        // Auto-resolve missing references
        if (fov == null) fov = GetComponent<FieldOfView>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        // Configure rigidbody for smooth isometric movement
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Find the player by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"{name}: No GameObject with 'Player' tag found!");
        }

        ChangeState(initialState);
    }

    private void Update()
    {
        if (!canDetectPlayer) return;

        CheckForAttack();
    }

    private void FixedUpdate()
    {
        if (!canDetectPlayer || isTeleporting) return;

        currentState?.Execute();
    }

    // ========================================================================
    // State Machine
    // ========================================================================

    // Transitions to a new state by enum type. Creates a fresh state instance.
    public void ChangeState(EnemyStateType newStateType)
    {
        currentState?.Exit();

        currentState = newStateType switch
        {
            EnemyStateType.Patrolling => new PatrolState(this),
            EnemyStateType.Suspicious => new SuspiciousState(this),
            EnemyStateType.Alerted => new AlertedState(this),
            EnemyStateType.Searching => new SearchState(this),
            _ => null
        };

        currentState?.Enter();
    }

    // Transitions to a pre-built state instance (e.g., FleeState with custom duration).
    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    // ========================================================================
    // Animation Helpers
    // ========================================================================

    // Sets blend-tree direction parameters and flips the sprite horizontally.
    public void SetMovementAnimation(Vector2 direction, bool isMoving)
    {
        IsMoving = isMoving;

        if (animator != null)
        {
            animator.SetFloat("MoveX", direction.x);
            animator.SetFloat("MoveY", direction.y);
            animator.SetBool("IsMoving", isMoving);
        }

        FlipSprite(direction.x);
    }

    // Toggles the alerted animation bool.
    public void SetAlertedAnimation(bool isAlerted)
    {
        if (animator != null)
        {
            animator.SetBool("IsAlerted", isAlerted);
        }
    }

    // Batch-updates all three state-related animator booleans.
    public void UpdateAnimatorStates(bool isMoving, bool isAlerted, bool isSuspicious)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsAlerted", isAlerted);
            animator.SetBool("IsSuspicious", isSuspicious);
        }
    }

    // Fires the "Attack" trigger if the parameter exists in the animator.
    public void TriggerAttackAnimation()
    {
        if (animator != null && HasAnimatorParameter("Attack"))
        {
            animator.SetTrigger("Attack");
        }
    }


    // Flips the sprite based on horizontal direction.
    public void FlipSprite(float horizontalDirection)
    {
        if (horizontalDirection > 0.1f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (horizontalDirection < -0.1f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // ========================================================================
    // Combat
    // ========================================================================

    // Called every Update while alerted — initiates an attack if the player is in range
    // and the cooldown has elapsed.
    private void CheckForAttack()
    {
        if (!(currentState is AlertedState) || isAttacking || fov.player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, fov.player.position);

        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackPlayer(fov.player.gameObject));
            lastAttackTime = Time.time;
        }
    }

    // Attack coroutine: wind-up -> deal damage -> recovery.
    private IEnumerator AttackPlayer(GameObject player)
    {
        isAttacking = true;
        SetMovementAnimation(Vector2.zero, false);
        UpdateAnimatorStates(false, true, false);
        TriggerAttackAnimation();

        // Wind-up
        yield return new WaitForSeconds(0.3f);

        // Apply damage
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Recovery
        yield return new WaitForSeconds(0.2f);

        UpdateAnimatorStates(false, true, false);
        isAttacking = false;
    }

    // ========================================================================
    // Obstacle Avoidance
    // ========================================================================

    // Casts five rays (center, ±45°, ±90°) and blends an avoidance vector
    // with the desired direction when obstacles are detected.
    public Vector2 GetEnhancedAvoidance(Vector2 desiredDirection)
    {
        if (desiredDirection == Vector2.zero) return desiredDirection;

        Vector2 avoidance = Vector2.zero;
        int hitCount = 0;

        Vector2[] rayDirections =
        {
            desiredDirection,
            Quaternion.Euler(0f, 0f,  45f) * desiredDirection,
            Quaternion.Euler(0f, 0f, -45f) * desiredDirection,
            Quaternion.Euler(0f, 0f,  90f) * desiredDirection,
            Quaternion.Euler(0f, 0f, -90f) * desiredDirection
        };

        foreach (Vector2 dir in rayDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, rayDistance, obstacleLayer);

            if (hit.collider != null)
            {
                avoidance += hit.normal;
                hitCount++;
                Debug.DrawRay(transform.position, dir * rayDistance, Color.red, 0.1f);
            }
            else
            {
                Debug.DrawRay(transform.position, dir * rayDistance, Color.green, 0.1f);
            }
        }

        if (hitCount > 0)
        {
            avoidance /= hitCount;

            // Pick the perpendicular direction that best preserves forward progress
            Vector2 avoidDir = Vector2.Perpendicular(avoidance).normalized;
            if (Vector2.Dot(avoidDir, desiredDirection) < 0f)
            {
                avoidDir = -avoidDir;
            }

            Vector2 finalDirection = (desiredDirection + avoidDir * avoidanceForce).normalized;

            Debug.DrawRay(transform.position, desiredDirection * rayDistance, Color.blue, 0.1f);
            Debug.DrawRay(transform.position, finalDirection * rayDistance, Color.yellow, 0.1f);

            return finalDirection;
        }

        return desiredDirection;
    }

    // Returns true if no obstacle blocks the given direction up to the specified distance.
    public bool HasClearPath(Vector2 direction, float distance)
    {
        return !Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
    }

    // ========================================================================
    // Collision Teleport
    // ========================================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableCollisionTeleport || isTeleporting) return;

        bool hitTerrain = ((1 << collision.gameObject.layer) & terrainLayer) != 0;
        bool cooldownReady = Time.time >= lastTeleportTime + teleportCooldown;

        if (hitTerrain && cooldownReady)
        {
            StartTeleport();
        }
    }

    // Initiates the smoke-disappear-reappear teleport sequence.
    public void StartTeleport()
    {
        if (isTeleporting) return;

        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
        }

        teleportCoroutine = StartCoroutine(TeleportRoutine());
    }

    // Public trigger to force a teleport (useful for testing or scripted events).
    public void ForceTeleportToPatrol() => StartTeleport();

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;
        lastTeleportTime = Time.time;

        // Remember which patrol point to return to
        if (currentState is PatrolState patrolState)
        {
            lastPatrolIndex = patrolState.currentPatrolIndex;
            if (patrolPoints.Count > 0 && lastPatrolIndex < patrolPoints.Count)
            {
                lastPatrolPosition = patrolPoints[lastPatrolIndex].position;
            }
        }

        // Smoke at current position, then vanish
        PlaySmokeEffect(transform.position);
        yield return new WaitForSeconds(0.2f);

        SetEnemyVisibility(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(disappearDuration);

        // Determine teleport destination
        Vector2 destination = lastPatrolPosition;
        if (destination == Vector2.zero && patrolPoints.Count > 0)
        {
            destination = patrolPoints[0].position;
        }

        PlaySmokeEffect(destination);
        transform.position = (Vector3)destination;

        // Restore collision and visibility
        if (col != null) col.enabled = true;
        SetEnemyVisibility(true);

        ChangeState(EnemyStateType.Patrolling);

        isTeleporting = false;
        teleportCoroutine = null;
    }

    // Shows or hides the enemy (sprites, animator, detection).
    private void SetEnemyVisibility(bool isVisible)
    {
        // Toggle all sprite renderers (self + children)
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in allRenderers)
        {
            sr.enabled = isVisible;
        }

        canDetectPlayer = isVisible;

        if (animator != null)
        {
            animator.enabled = isVisible;
            if (isVisible) SetMovementAnimation(Vector2.zero, false);
        }
    }

    // ========================================================================
    // Smoke VFX
    // ========================================================================

    private void PlaySmokeEffect(Vector2 position)
    {
        if (smokeEffect != null)
        {
            smokeEffect.transform.position = position;
            smokeEffect.Play();
        }
    }

    public void StopSmokeEffect()
    {
        if (smokeEffect != null)
        {
            smokeEffect.Stop();
        }
    }

    // ========================================================================
    // Item Effect Reactions
    // ========================================================================

    // Temporarily freezes the enemy in place (triggered by Distract item effect).
    public void Distract(float duration)
    {
        if (!isDistracted)
        {
            StartCoroutine(DistractRoutine(duration));
        }
    }

    private IEnumerator DistractRoutine(float duration)
    {
        isDistracted = true;
        yield return new WaitForSeconds(duration);
        isDistracted = false;
    }

    // Forces the enemy into FleeState for the given duration (triggered by Fear item effect).
    public void Flee(float duration)
    {
        ChangeState(new FleeState(this, duration));
    }

    // ========================================================================
    // Debug
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        // Patrol route
        if (patrolPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null) continue;

                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                int nextIndex = (i + 1) % patrolPoints.Count;
                if (patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                }
            }
        }

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

