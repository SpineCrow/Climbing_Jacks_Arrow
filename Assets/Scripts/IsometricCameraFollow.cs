using UnityEngine;

// Smoothly follows a target transform with an offset.
// Designed for isometric 2D cameras. Optionally snaps to pixel grid
// for pixel-art games (enable usePixelSnapping in Inspector).
public class IsometricCameraFollow : MonoBehaviour
{
    [Tooltip("The transform the camera follows (usually the player)")]
    public Transform target;

    [Tooltip("Offset from the target position")]
    public Vector3 offset;

    [Tooltip("Smoothing time for camera movement (lower = snappier)")]
    public float smoothTime = 0.05f;

    [Header("Pixel Snapping (Optional)")]
    [Tooltip("Enable to snap camera position to the pixel grid")]
    public bool usePixelSnapping;

    [Tooltip("Pixels per unit of your sprite assets")]
    public float pixelsPerUnit = 32f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        transform.position = usePixelSnapping ? RoundToPixel(smoothed) : smoothed;
    }

    // Rounds a position to the nearest pixel boundary to prevent sub-pixel jitter.
    private Vector3 RoundToPixel(Vector3 pos)
    {
        float unitsPerPixel = 1f / pixelsPerUnit;
        return new Vector3(
            Mathf.Round(pos.x / unitsPerPixel) * unitsPerPixel,
            Mathf.Round(pos.y / unitsPerPixel) * unitsPerPixel,
            pos.z
        );
    }
}


