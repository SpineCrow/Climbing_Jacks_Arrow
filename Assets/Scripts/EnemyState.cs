using UnityEngine;
using System.Collections;


// Interface defining the lifecycle of an enemy state.
public interface IEnemyState
{
    void Enter();
    void Execute();
    void Exit();
}


// Base class for all enemy states. Caches common references.
public abstract class EnemyState : IEnemyState
{
    protected readonly EnemyAI enemy;
    protected readonly FieldOfView fov;

    protected EnemyState(EnemyAI enemy)
    {
        this.enemy = enemy;
        this.fov = enemy.fov;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();

    
    // Shared cleanup: stops movement and coroutines.
    // Called by most Exit() implementations.
    protected void StopAllMovement()
    {
        enemy.rb.linearVelocity = Vector2.zero;
        enemy.SetMovementAnimation(Vector2.zero, false);
        enemy.StopAllCoroutines();
    }
}


// ============================================================================
// Patrol State
// ============================================================================


// Enemy follows a looping patrol route, pausing at each waypoint to look around.
// Transitions to SuspiciousState or AlertedState based on detection level.
public class PatrolState : EnemyState
{
    internal int currentPatrolIndex;
    private bool isWaiting;
    private Vector2 currentTargetPosition;

    public PatrolState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SetMovementAnimation(Vector2.zero, false);
        enemy.UpdateAnimatorStates(false, false, false);

        if (enemy.patrolPoints.Count > 0)
        {
            currentTargetPosition = enemy.patrolPoints[currentPatrolIndex].position;
        }
        else
        {
            Debug.LogWarning($"{enemy.name}: No patrol points assigned.");
        }
    }

    public override void Execute()
    {
        if (enemy.patrolPoints.Count == 0 || isWaiting) return;

        float distanceToTarget = Vector2.Distance(enemy.transform.position, currentTargetPosition);

        // Reached waypoint — pause and look around
        if (distanceToTarget <= enemy.reachedDistance)
        {
            enemy.StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        // Move toward current patrol waypoint with obstacle avoidance
        Vector2 desiredDirection = (currentTargetPosition - (Vector2)enemy.transform.position).normalized;
        Vector2 finalDirection = enemy.GetEnhancedAvoidance(desiredDirection);

        Vector2 newPosition = Vector2.MoveTowards(
            enemy.rb.position,
            enemy.rb.position + finalDirection,
            enemy.patrolSpeed * Time.deltaTime
        );

        enemy.rb.MovePosition(newPosition);
        enemy.SetMovementAnimation(finalDirection, true);

        // Check detection thresholds for state transitions
        CheckDetectionTransitions();
    }

    public override void Exit()
    {
        StopAllMovement();
    }

    
    // Pauses at the current waypoint, cycling through look directions, then advances.
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        enemy.SetMovementAnimation(Vector2.zero, false);

        Vector2[] lookDirections = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        float lookDuration = enemy.waitTimeAtPoints / lookDirections.Length;

        foreach (Vector2 direction in lookDirections)
        {
            enemy.SetMovementAnimation(direction, false);
            yield return new WaitForSeconds(lookDuration);
        }

        // Advance to next waypoint (loop)
        currentPatrolIndex = (currentPatrolIndex + 1) % enemy.patrolPoints.Count;
        currentTargetPosition = enemy.patrolPoints[currentPatrolIndex].position;
        isWaiting = false;
    }

    private void CheckDetectionTransitions()
    {
        if (fov.player == null) return;

        if (fov.detectionLevel >= fov.settings.alertThreshold)
        {
            enemy.ChangeState(new AlertedState(enemy));
        }
        else if (fov.detectionLevel >= fov.settings.suspicionThreshold)
        {
            enemy.ChangeState(new SuspiciousState(enemy));
        }
    }
}


// ============================================================================
// Suspicious State
// ============================================================================


// Enemy has partial awareness of the player. Slowly approaches the last known
// position while facing it. Escalates to AlertedState or returns to PatrolState.
public class SuspiciousState : EnemyState
{
    public SuspiciousState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.UpdateAnimatorStates(false, false, true);
    }

    public override void Execute()
    {
        if (fov.lastKnownPosition != Vector2.zero)
        {
            Vector2 desiredDirection = (fov.lastKnownPosition - (Vector2)enemy.transform.position).normalized;
            Vector2 finalDirection = enemy.GetEnhancedAvoidance(desiredDirection);

            // Flip sprite to face the investigation direction
            enemy.FlipSprite(desiredDirection.x);

            // Move at half patrol speed toward last known position
            Vector2 newPosition = Vector2.MoveTowards(
                enemy.rb.position,
                enemy.rb.position + finalDirection,
                enemy.patrolSpeed * 0.5f * Time.deltaTime
            );

            enemy.rb.MovePosition(newPosition);
            enemy.UpdateAnimatorStates(true, false, true);
        }
        else
        {
            enemy.rb.linearVelocity = Vector2.zero;
            enemy.UpdateAnimatorStates(false, false, true);
        }

        // Escalate or de-escalate
        if (fov.detectionLevel >= 1f)
        {
            enemy.ChangeState(new AlertedState(enemy));
        }
        else if (fov.detectionLevel <= 0.1f)
        {
            enemy.ChangeState(new PatrolState(enemy));
        }
    }

    public override void Exit()
    {
        StopAllMovement();
    }
}


// ============================================================================
// Alerted State
// ============================================================================


// Enemy is fully aware and actively chasing the player.
// Stops to attack when within range. Transitions to SearchState on losing sight.
public class AlertedState : EnemyState
{
    private const float CHASE_SPEED_MULTIPLIER = 1.5f;

    public AlertedState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.SetAlertedAnimation(true);
        enemy.UpdateAnimatorStates(false, true, false);
    }

    public override void Execute()
    {
        if (fov.canSeePlayer && fov.player != null)
        {
            Vector2 desiredDirection = (fov.player.position - enemy.transform.position).normalized;
            Vector2 finalDirection = enemy.GetEnhancedAvoidance(desiredDirection);

            enemy.FlipSprite(desiredDirection.x);

            float distanceToPlayer = Vector2.Distance(enemy.transform.position, fov.player.position);

            if (distanceToPlayer <= enemy.attackRange)
            {
                // In attack range — stop and let attack logic handle it
                enemy.rb.linearVelocity = Vector2.zero;
                enemy.UpdateAnimatorStates(false, true, false);
            }
            else
            {
                // Chase the player at increased speed
                Vector2 newPosition = Vector2.MoveTowards(
                    enemy.rb.position,
                    enemy.rb.position + finalDirection,
                    enemy.patrolSpeed * CHASE_SPEED_MULTIPLIER * Time.deltaTime
                );

                enemy.rb.MovePosition(newPosition);
                enemy.UpdateAnimatorStates(true, true, false);
            }
        }
        else
        {
            // Lost line of sight — search the area
            enemy.ChangeState(new SearchState(enemy));
        }
    }

    public override void Exit()
    {
        StopAllMovement();
    }
}


// ============================================================================
// Search State
// ============================================================================


// Enemy moves to the last known player position and looks around.
// Re-alerts if detection spikes, or returns to patrol after the search timer expires.
public class SearchState : EnemyState
{
    private float searchTimer;
    private const float MAX_SEARCH_TIME = 5f;

    public SearchState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        searchTimer = 0f;
    }

    public override void Execute()
    {
        searchTimer += Time.deltaTime;

        // Navigate to last known position with obstacle avoidance
        Vector2 desiredDirection = (fov.lastKnownPosition - (Vector2)enemy.transform.position).normalized;
        Vector2 finalDirection = enemy.GetEnhancedAvoidance(desiredDirection);

        float distanceToTarget = Vector2.Distance(enemy.transform.position, fov.lastKnownPosition);

        if (distanceToTarget > enemy.reachedDistance)
        {
            Vector2 newPosition = Vector2.MoveTowards(
                enemy.rb.position,
                enemy.rb.position + finalDirection,
                enemy.patrolSpeed * Time.deltaTime
            );

            enemy.rb.MovePosition(newPosition);
            enemy.SetMovementAnimation(finalDirection, true);
        }
        else
        {
            enemy.SetMovementAnimation(Vector2.zero, false);
        }

        // Re-alert or give up
        if (fov.detectionLevel >= 1f)
        {
            enemy.ChangeState(new AlertedState(enemy));
        }
        else if (searchTimer >= MAX_SEARCH_TIME || fov.detectionLevel <= 0.1f)
        {
            enemy.ChangeState(new PatrolState(enemy));
        }
    }

    public override void Exit()
    {
        StopAllMovement();
    }
}


// ============================================================================
// Flee State
// ============================================================================


// Enemy runs away from the player for a set duration (triggered by Fear effect).
// Returns to PatrolState when the timer expires.
public class FleeState : EnemyState
{
    private readonly float fleeDuration;
    private float fleeTimer;
    private Vector2 fleeDirection;

    private const float FLEE_SPEED_MULTIPLIER = 2f;

    public FleeState(EnemyAI enemy, float duration) : base(enemy)
    {
        fleeDuration = duration;
        fleeTimer = duration;
    }

    public override void Enter()
    {
        // Determine flee direction (away from player)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            fleeDirection = ((Vector2)enemy.transform.position - (Vector2)player.transform.position).normalized;
        }
        else
        {
            // Fallback: random direction if player reference is unavailable
            fleeDirection = Random.insideUnitCircle.normalized;
        }

        enemy.UpdateAnimatorStates(true, false, false);
    }

    public override void Execute()
    {
        fleeTimer -= Time.deltaTime;

        Vector2 finalDirection = enemy.GetEnhancedAvoidance(fleeDirection);

        Vector2 newPosition = Vector2.MoveTowards(
            enemy.rb.position,
            enemy.rb.position + fleeDirection,
            enemy.patrolSpeed * FLEE_SPEED_MULTIPLIER * Time.deltaTime
        );

        enemy.rb.MovePosition(newPosition);
        enemy.SetMovementAnimation(finalDirection, true);

        if (fleeTimer <= 0f)
        {
            enemy.ChangeState(new PatrolState(enemy));
        }
    }

    public override void Exit()
    {
        enemy.rb.linearVelocity = Vector2.zero;
        enemy.SetMovementAnimation(Vector2.zero, false);
    }
}


