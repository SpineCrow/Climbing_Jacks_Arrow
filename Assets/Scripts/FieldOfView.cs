using System.Collections;
using UnityEngine;


// Handles line-of-sight detection and suspicion accumulation for enemies.
// Runs periodic visibility checks via coroutine and exposes detection state
// for the state machine (EnemyState) and visualizer (DetectionVisualizer).
public class FieldOfView : MonoBehaviour
{
    [Tooltip("ScriptableObject containing vision and suspicion parameters")]
    public DetectionSettings settings;

    // --- Public read-only detection state (consumed by EnemyState / EnemyAI) ---
    [HideInInspector] public bool canSeePlayer;
    [HideInInspector] public float detectionLevel;
    [HideInInspector] public Vector2 lastKnownPosition;
    [HideInInspector] public Transform player;

    // --- Internal references ---
    [Tooltip("Assign if auto-find by tag is unreliable; otherwise found at Start")]
    public Transform playerTransform;

    private EnemyAI enemyAI;

    // --- Suspicion tracking ---
    private float suspicionAccumulation;
    private bool wasPlayerRecentlySeen;
    private float timeSinceLastSeen;

    // --- Constants ---
    private const float DETECTION_INTERVAL = 0.2f;

    #region Unity Lifecycle

    private void Start()
    {
        // Cache references
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        enemyAI = GetComponent<EnemyAI>();

        StartCoroutine(FindTargetsWithDelay());
    }

    #endregion

    #region Detection Loop

    
    // Periodically checks for player visibility to avoid per-frame raycast cost.
    
    private IEnumerator FindTargetsWithDelay()
    {
        WaitForSeconds wait = new WaitForSeconds(DETECTION_INTERVAL);

        while (true)
        {
            yield return wait;
            FindVisibleTargets();
        }
    }

    
    // Core visibility check: angle -> distance-> raycast obstruction.
    
    private void FindVisibleTargets()
    {
        if (playerTransform == null) return;

        canSeePlayer = false;

        Vector2 toPlayer = (playerTransform.position - transform.position);
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 directionToPlayer = toPlayer / distanceToPlayer; // Normalized without extra call

        Vector2 facingDirection = GetFacingDirection();

        // Check angle first (cheapest), then distance, then raycast (most expensive)
        bool withinAngle = Vector2.Angle(facingDirection, directionToPlayer) < settings.viewAngle * 0.5f;
        bool withinRange = distanceToPlayer <= settings.viewRadius;

        if (withinAngle && withinRange)
        {
            bool lineOfSightClear = !Physics2D.Raycast(
                transform.position,
                directionToPlayer,
                distanceToPlayer,
                settings.obstacleMask
            );

            if (lineOfSightClear)
            {
                canSeePlayer = true;
                player = playerTransform;
                lastKnownPosition = playerTransform.position;
                wasPlayerRecentlySeen = true;
                timeSinceLastSeen = 0f;
            }
        }

        // Gradually forget the player after memory duration expires
        if (!canSeePlayer)
        {
            timeSinceLastSeen += DETECTION_INTERVAL;

            if (wasPlayerRecentlySeen && timeSinceLastSeen > settings.memoryDuration)
            {
                player = null;
                wasPlayerRecentlySeen = false;
            }
        }

        UpdateDetectionLevel();
    }

    #endregion

    #region Suspicion System

    
    // Adjusts the suspicion accumulator based on current visibility.
    // Uses a power curve (^1.5) for natural-feeling detection ramp-up.
    private void UpdateDetectionLevel()
    {
        float deltaRate = DETECTION_INTERVAL / settings.suspicionBuildTime;

        if (canSeePlayer)
        {
            // Build suspicion while player is visible
            suspicionAccumulation = Mathf.Clamp01(suspicionAccumulation + deltaRate);
            detectionLevel = Mathf.Pow(suspicionAccumulation, 1.5f);
        }
        else if (wasPlayerRecentlySeen)
        {
            // Decay slowly (3x slower than build rate) when player was recently seen
            suspicionAccumulation = Mathf.Clamp01(suspicionAccumulation - deltaRate / 3f);
            detectionLevel = Mathf.Pow(suspicionAccumulation, 1.5f);
        }
        else
        {
            // Reset quickly when player hasn't been seen in a while
            suspicionAccumulation = Mathf.MoveTowards(suspicionAccumulation, 0f, deltaRate);
            detectionLevel = suspicionAccumulation;
        }
    }

    #endregion

    #region Utility

    
    // Returns the enemy's facing direction based on sprite scale (for isometric 2D).
    // Falls back to transform.right if no EnemyAI is present.
    private Vector2 GetFacingDirection()
    {
        if (enemyAI != null)
        {
            return transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }

        return transform.right;
    }

    
    // Converts an angle (degrees) to a 2D direction vector.
    // Used by DetectionVisualizer and gizmo drawing.
    
    // <param name="angleInDegrees">Angle in degrees.</param>
    // <param name="angleIsGlobal">If false, the angle is relative to the enemy's facing direction.</param>
    public Vector2 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            Vector2 facing = GetFacingDirection();
            float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            angleInDegrees += facingAngle;
        }

        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;

        // Vision radius
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, settings.viewRadius);

        // Vision cone edges
        Vector2 facingDirection = Application.isPlaying ? GetFacingDirection() : Vector2.right;
        Vector3 viewAngleA = DirFromAngle(-settings.viewAngle * 0.5f, false);
        Vector3 viewAngleB = DirFromAngle(settings.viewAngle * 0.5f, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * settings.viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * settings.viewRadius);

        // Detection line to player (color reflects suspicion level)
        if (canSeePlayer && playerTransform != null)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, detectionLevel);
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // Facing direction indicator
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)facingDirection * 2f);
    }

    #endregion
}


