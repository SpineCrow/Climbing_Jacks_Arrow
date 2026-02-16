using UnityEngine;

// ScriptableObject holding all detection-related configuration for enemies.
// Create via Assets > Create > Game > Detection Settings.
[CreateAssetMenu(menuName = "Game/Detection Settings")]
public class DetectionSettings : ScriptableObject
{
    [Header("Vision Settings")]
    [Tooltip("Maximum distance the enemy can see")]
    public float viewRadius = 5f;

    [Range(0f, 360f)]
    [Tooltip("Total angular width of the vision cone")]
    public float viewAngle = 90f;

    [Tooltip("Layers that block line of sight")]
    public LayerMask obstacleMask;

    [Header("Suspicion System")]
    [Tooltip("Detection level at which the enemy becomes suspicious")]
    [Range(0f, 1f)]
    public float suspicionThreshold = 0.3f;

    [Tooltip("Detection level at which the enemy becomes fully alerted")]
    [Range(0f, 1f)]
    public float alertThreshold = 0.8f;

    [Tooltip("Seconds to build from zero to full suspicion")]
    [Min(0.1f)]
    public float suspicionBuildTime = 3f;

    [Tooltip("Seconds the enemy remembers the player's last known position")]
    [Min(0f)]
    public float memoryDuration = 5f;

    [Header("Detection Rates")]
    [Tooltip("Seconds to fully detect a visible player")]
    [Min(0.1f)]
    public float detectionTime = 2f;
}
