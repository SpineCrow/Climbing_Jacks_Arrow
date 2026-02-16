using UnityEngine;

// Debug-only component that logs FieldOfView detection state every frame.
// Automatically stripped from release builds via #if UNITY_EDITOR.
public class DebugEnemy : MonoBehaviour
{
#if UNITY_EDITOR
    private FieldOfView fov;

    private void Start()
    {
        fov = GetComponent<FieldOfView>();
    }

    private void Update()
    {
        if (fov == null) return;

        Debug.Log($"[{name}] CanSeePlayer: {fov.canSeePlayer} | Detection: {fov.detectionLevel:F2}");
    }
#endif
}
